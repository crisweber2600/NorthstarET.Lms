using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace NorthstarET.Lms.Infrastructure.Performance;

/// <summary>
/// Service for monitoring application performance metrics and SLA compliance
/// Implements performance requirements PR-001, PR-002, PR-003, PR-004
/// </summary>
public class PerformanceMonitor : BackgroundService, IPerformanceMonitor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly PerformanceOptions _options;
    private readonly ConcurrentDictionary<string, PerformanceMetrics> _operationMetrics = new();
    private readonly ConcurrentQueue<PerformanceDataPoint> _realtimeData = new();
    private readonly Timer _analysisTimer;
    private readonly Timer _alertTimer;

    // SLA Thresholds from requirements
    private readonly Dictionary<string, TimeSpan> _slaThresholds = new()
    {
        ["CRUD"] = TimeSpan.FromMilliseconds(200), // PR-001: CRUD operations <200ms at 95th percentile
        ["Bulk"] = TimeSpan.FromSeconds(120),      // PR-002: Bulk operations (10k rows) <120s
        ["Audit"] = TimeSpan.FromSeconds(2),      // PR-003: Audit queries <2s on 1M records
        ["System"] = TimeSpan.FromMilliseconds(500) // General system operations
    };

    public PerformanceMonitor(
        IServiceProvider serviceProvider,
        ILogger<PerformanceMonitor> logger,
        PerformanceOptions options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        
        // Set up periodic analysis and alerting
        _analysisTimer = new Timer(AnalyzePerformanceMetrics, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _alertTimer = new Timer(CheckSlaViolations, null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Records the start of a performance measurement
    /// </summary>
    public IPerformanceScope StartMeasurement(string operationType, string operationName, Dictionary<string, object>? context = null)
    {
        return new PerformanceScope(this, operationType, operationName, context ?? new Dictionary<string, object>());
    }

    /// <summary>
    /// Records performance data for an operation
    /// </summary>
    public async Task RecordPerformanceAsync(
        string operationType, 
        string operationName, 
        TimeSpan duration, 
        bool success = true,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        var dataPoint = new PerformanceDataPoint
        {
            OperationType = operationType,
            OperationName = operationName,
            Duration = duration,
            Success = success,
            Timestamp = DateTime.UtcNow,
            Context = context ?? new Dictionary<string, object>()
        };

        _realtimeData.Enqueue(dataPoint);

        // Update rolling metrics
        var metricsKey = $"{operationType}:{operationName}";
        var metrics = _operationMetrics.GetOrAdd(metricsKey, _ => new PerformanceMetrics(operationType, operationName));
        
        await metrics.RecordDataPointAsync(dataPoint);

        // Check for immediate SLA violations
        if (IsSlViolation(operationType, duration))
        {
            await HandleSlaViolationAsync(dataPoint, cancellationToken);
        }

        _logger.LogDebug("Performance recorded: {OperationType}.{OperationName} completed in {Duration}ms (Success: {Success})",
            operationType, operationName, duration.TotalMilliseconds, success);
    }

    /// <summary>
    /// Gets current performance metrics for an operation
    /// </summary>
    public async Task<PerformanceMetrics?> GetOperationMetricsAsync(
        string operationType, 
        string operationName,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make async for interface compatibility
        var metricsKey = $"{operationType}:{operationName}";
        return _operationMetrics.GetValueOrDefault(metricsKey);
    }

    /// <summary>
    /// Gets performance summary across all operations
    /// </summary>
    public async Task<PerformanceSummary> GetPerformanceSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = new PerformanceSummary
        {
            GeneratedAt = DateTime.UtcNow,
            TotalOperations = _operationMetrics.Values.Sum(m => m.TotalCount),
            AverageResponseTime = TimeSpan.FromMilliseconds(CalculateOverallAverageResponseTime()),
            SlaCompliance = CalculateSlaCompliance()
        };

        // Group metrics by operation type
        foreach (var typeGroup in _operationMetrics.Values.GroupBy(m => m.OperationType))
        {
            var typeMetrics = new OperationTypeMetrics
            {
                OperationType = typeGroup.Key,
                TotalOperations = typeGroup.Sum(m => m.TotalCount),
                AverageResponseTime = TimeSpan.FromMilliseconds(
                    typeGroup.Average(m => m.AverageResponseTime.TotalMilliseconds)),
                P95ResponseTime = CalculateP95ForType(typeGroup),
                SuccessRate = typeGroup.Average(m => m.SuccessRate),
                SlaCompliance = CalculateTypeSlaCompliance(typeGroup.Key)
            };

            summary.OperationTypes.Add(typeMetrics);
        }

        // Add SLA violations
        summary.RecentSlaViolations = await GetRecentSlaViolationsAsync(50, cancellationToken);

        _logger.LogDebug("Performance summary generated: {TotalOps} operations, {AvgTime}ms avg, {SlaCompliance}% SLA compliance",
            summary.TotalOperations, summary.AverageResponseTime.TotalMilliseconds, summary.SlaCompliance);

        return summary;
    }

    /// <summary>
    /// Gets performance health status
    /// </summary>
    public async Task<PerformanceHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var summary = await GetPerformanceSummaryAsync(cancellationToken);
        
        var status = new PerformanceHealthStatus
        {
            CheckedAt = DateTime.UtcNow,
            OverallHealth = DetermineOverallHealth(summary),
            SlaCompliance = summary.SlaCompliance,
            ActiveAlerts = await GetActivePerformanceAlertsAsync(cancellationToken),
            Recommendations = GeneratePerformanceRecommendations(summary)
        };

        return status;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performance monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPerformanceDataAsync(stoppingToken);
                await Task.Delay(_options.ProcessingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in performance monitoring service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Performance monitoring service stopped");
    }

    private async Task ProcessPerformanceDataAsync(CancellationToken cancellationToken)
    {
        var dataPointsProcessed = 0;
        
        while (_realtimeData.TryDequeue(out var dataPoint) && dataPointsProcessed < _options.MaxDataPointsPerBatch)
        {
            await StorePerformanceDataAsync(dataPoint, cancellationToken);
            dataPointsProcessed++;
        }

        if (dataPointsProcessed > 0)
        {
            _logger.LogDebug("Processed {DataPointCount} performance data points", dataPointsProcessed);
        }
    }

    private async Task StorePerformanceDataAsync(PerformanceDataPoint dataPoint, CancellationToken cancellationToken)
    {
        try
        {
            // Store in audit/metrics system for historical analysis
            using var scope = _serviceProvider.CreateScope();
            var auditService = scope.ServiceProvider.GetService<Application.Interfaces.IAuditService>();
            
            if (auditService != null)
            {
                // IAuditService doesn't have LogPerformanceMetricAsync, use regular LogAsync
                await auditService.LogAsync(
                    "PerformanceMetric",
                    "PerformanceDataPoint", 
                    Guid.NewGuid(),
                    "system",
                    new { 
                        OperationType = dataPoint.OperationType,
                        OperationName = dataPoint.OperationName,
                        Duration = dataPoint.Duration.TotalMilliseconds,
                        Success = dataPoint.Success,
                        Context = dataPoint.Context 
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store performance data for {OperationType}.{OperationName}",
                dataPoint.OperationType, dataPoint.OperationName);
        }
    }

    private bool IsSlViolation(string operationType, TimeSpan duration)
    {
        if (!_slaThresholds.TryGetValue(operationType, out var threshold))
        {
            threshold = _slaThresholds["System"];
        }

        return duration > threshold;
    }

    private async Task HandleSlaViolationAsync(PerformanceDataPoint dataPoint, CancellationToken cancellationToken)
    {
        var violation = new SlaViolation
        {
            Id = Guid.NewGuid(),
            OperationType = dataPoint.OperationType,
            OperationName = dataPoint.OperationName,
            ActualDuration = dataPoint.Duration,
            ExpectedDuration = _slaThresholds.GetValueOrDefault(dataPoint.OperationType, _slaThresholds["System"]),
            Timestamp = dataPoint.Timestamp,
            Context = dataPoint.Context
        };

        _logger.LogWarning("SLA violation detected: {OperationType}.{OperationName} took {ActualMs}ms, expected <{ExpectedMs}ms",
            violation.OperationType, violation.OperationName, 
            violation.ActualDuration.TotalMilliseconds, violation.ExpectedDuration.TotalMilliseconds);

        // Store violation for reporting
        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetService<Application.Interfaces.IAuditService>();
        
        if (auditService != null)
        {
            // IAuditService doesn't have LogSecurityEventAsync, use regular LogAsync
            await auditService.LogAsync(
                "PerformanceSlaViolation",
                "SlaViolation",
                Guid.NewGuid(),
                "system",
                violation);
        }
    }

    private double CalculateOverallAverageResponseTime()
    {
        if (!_operationMetrics.Values.Any())
            return 0;

        return _operationMetrics.Values
            .Where(m => m.TotalCount > 0)
            .Average(m => m.AverageResponseTime.TotalMilliseconds);
    }

    private double CalculateSlaCompliance()
    {
        if (!_operationMetrics.Values.Any())
            return 100.0;

        var totalOperations = _operationMetrics.Values.Sum(m => m.TotalCount);
        var compliantOperations = _operationMetrics.Values.Sum(m => m.SlaCompliantCount);

        return totalOperations > 0 ? (double)compliantOperations / totalOperations * 100.0 : 100.0;
    }

    private TimeSpan CalculateP95ForType(IEnumerable<PerformanceMetrics> typeMetrics)
    {
        var p95Values = typeMetrics.Select(m => m.P95ResponseTime.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(p95Values.Any() ? p95Values.Max() : 0);
    }

    private double CalculateTypeSlaCompliance(string operationType)
    {
        var typeMetrics = _operationMetrics.Values.Where(m => m.OperationType == operationType);
        
        if (!typeMetrics.Any())
            return 100.0;

        var totalOperations = typeMetrics.Sum(m => m.TotalCount);
        var compliantOperations = typeMetrics.Sum(m => m.SlaCompliantCount);

        return totalOperations > 0 ? (double)compliantOperations / totalOperations * 100.0 : 100.0;
    }

    private async Task<List<SlaViolation>> GetRecentSlaViolationsAsync(int count, CancellationToken cancellationToken)
    {
        // This would typically query from persistent storage
        // For now, return empty list as violations are logged to audit system
        await Task.CompletedTask;
        return new List<SlaViolation>();
    }

    private PerformanceHealth DetermineOverallHealth(PerformanceSummary summary)
    {
        if (summary.SlaCompliance >= 95.0)
            return PerformanceHealth.Excellent;
        else if (summary.SlaCompliance >= 90.0)
            return PerformanceHealth.Good;
        else if (summary.SlaCompliance >= 80.0)
            return PerformanceHealth.Fair;
        else
            return PerformanceHealth.Poor;
    }

    private async Task<List<string>> GetActivePerformanceAlertsAsync(CancellationToken cancellationToken)
    {
        var alerts = new List<string>();

        // Check for active performance issues
        foreach (var metrics in _operationMetrics.Values)
        {
            if (metrics.SuccessRate < 95.0)
            {
                alerts.Add($"Low success rate for {metrics.OperationType}.{metrics.OperationName}: {metrics.SuccessRate:F1}%");
            }

            if (IsSlViolation(metrics.OperationType, metrics.P95ResponseTime))
            {
                alerts.Add($"P95 SLA violation for {metrics.OperationType}.{metrics.OperationName}: {metrics.P95ResponseTime.TotalMilliseconds:F0}ms");
            }
        }

        await Task.CompletedTask;
        return alerts;
    }

    private List<string> GeneratePerformanceRecommendations(PerformanceSummary summary)
    {
        var recommendations = new List<string>();

        if (summary.SlaCompliance < 90.0)
        {
            recommendations.Add("Consider scaling up resources or optimizing slow operations");
        }

        var slowOperations = summary.OperationTypes
            .Where(ot => ot.P95ResponseTime > _slaThresholds.GetValueOrDefault(ot.OperationType, _slaThresholds["System"]))
            .ToList();

        if (slowOperations.Any())
        {
            recommendations.Add($"Optimize slow operation types: {string.Join(", ", slowOperations.Select(o => o.OperationType))}");
        }

        if (summary.OperationTypes.Any(ot => ot.SuccessRate < 95.0))
        {
            recommendations.Add("Investigate and fix operations with low success rates");
        }

        return recommendations;
    }

    private void AnalyzePerformanceMetrics(object? state)
    {
        try
        {
            // Cleanup old metrics data
            var cutoffTime = DateTime.UtcNow.AddHours(-_options.MetricsRetentionHours);
            
            foreach (var metrics in _operationMetrics.Values)
            {
                metrics.CleanupOldData(cutoffTime);
            }

            var activeMetrics = _operationMetrics.Values.Count(m => m.TotalCount > 0);
            _logger.LogDebug("Performance metrics analysis completed. Active metrics: {MetricCount}", activeMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during performance metrics analysis");
        }
    }

    private void CheckSlaViolations(object? state)
    {
        try
        {
            var violationCount = 0;
            
            foreach (var metrics in _operationMetrics.Values)
            {
                if (metrics.SlaComplianceRate < _options.SlaAlertThreshold)
                {
                    violationCount++;
                    _logger.LogWarning("SLA compliance below threshold for {OperationType}.{OperationName}: {Compliance:F1}%",
                        metrics.OperationType, metrics.OperationName, metrics.SlaComplianceRate);
                }
            }

            if (violationCount > 0)
            {
                _logger.LogInformation("SLA check completed: {ViolationCount} operations below compliance threshold", violationCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SLA violation check");
        }
    }

    public override void Dispose()
    {
        _analysisTimer?.Dispose();
        _alertTimer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Performance measurement scope that automatically records duration
/// </summary>
public class PerformanceScope : IPerformanceScope
{
    private readonly PerformanceMonitor _monitor;
    private readonly string _operationType;
    private readonly string _operationName;
    private readonly Dictionary<string, object> _context;
    private readonly Stopwatch _stopwatch;
    private bool _disposed = false;

    public PerformanceScope(PerformanceMonitor monitor, string operationType, string operationName, Dictionary<string, object> context)
    {
        _monitor = monitor;
        _operationType = operationType;
        _operationName = operationName;
        _context = context;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Complete(bool success = true)
    {
        if (!_disposed)
        {
            _stopwatch.Stop();
            _ = Task.Run(() => _monitor.RecordPerformanceAsync(_operationType, _operationName, _stopwatch.Elapsed, success, _context));
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Complete(true);
    }
}

// Supporting classes and interfaces...

/// <summary>
/// Interface for performance monitoring service
/// </summary>
public interface IPerformanceMonitor
{
    IPerformanceScope StartMeasurement(string operationType, string operationName, Dictionary<string, object>? context = null);
    Task RecordPerformanceAsync(string operationType, string operationName, TimeSpan duration, bool success = true, Dictionary<string, object>? context = null, CancellationToken cancellationToken = default);
    Task<PerformanceMetrics?> GetOperationMetricsAsync(string operationType, string operationName, CancellationToken cancellationToken = default);
    Task<PerformanceSummary> GetPerformanceSummaryAsync(CancellationToken cancellationToken = default);
    Task<PerformanceHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Performance measurement scope interface
/// </summary>
public interface IPerformanceScope : IDisposable
{
    void Complete(bool success = true);
}

/// <summary>
/// Configuration options for performance monitoring
/// </summary>
public class PerformanceOptions
{
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxDataPointsPerBatch { get; set; } = 1000;
    public int MetricsRetentionHours { get; set; } = 24;
    public double SlaAlertThreshold { get; set; } = 90.0; // Percent
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// Performance data point
/// </summary>
public class PerformanceDataPoint
{
    public string OperationType { get; set; } = "";
    public string OperationName { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Aggregated performance metrics for an operation
/// </summary>
public class PerformanceMetrics
{
    private readonly List<PerformanceDataPoint> _recentDataPoints = new();
    private readonly object _lock = new();

    public string OperationType { get; }
    public string OperationName { get; }
    public long TotalCount { get; private set; }
    public long SlaCompliantCount { get; private set; }
    public TimeSpan AverageResponseTime { get; private set; }
    public TimeSpan P95ResponseTime { get; private set; }
    public double SuccessRate { get; private set; }
    public double SlaComplianceRate => TotalCount > 0 ? (double)SlaCompliantCount / TotalCount * 100.0 : 100.0;

    public PerformanceMetrics(string operationType, string operationName)
    {
        OperationType = operationType;
        OperationName = operationName;
    }

    public async Task RecordDataPointAsync(PerformanceDataPoint dataPoint)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                _recentDataPoints.Add(dataPoint);
                TotalCount++;

                if (dataPoint.Success)
                {
                    // Update success rate calculation
                }

                // Update SLA compliance
                var slaThresholds = new Dictionary<string, TimeSpan>
                {
                    ["CRUD"] = TimeSpan.FromMilliseconds(200),
                    ["Bulk"] = TimeSpan.FromSeconds(120),
                    ["Audit"] = TimeSpan.FromSeconds(2),
                    ["System"] = TimeSpan.FromMilliseconds(500)
                };

                var threshold = slaThresholds.GetValueOrDefault(OperationType, slaThresholds["System"]);
                if (dataPoint.Duration <= threshold)
                {
                    SlaCompliantCount++;
                }

                // Recalculate metrics
                RecalculateMetrics();
            }
        });
    }

    public void CleanupOldData(DateTime cutoffTime)
    {
        lock (_lock)
        {
            _recentDataPoints.RemoveAll(dp => dp.Timestamp < cutoffTime);
            RecalculateMetrics();
        }
    }

    private void RecalculateMetrics()
    {
        if (_recentDataPoints.Count == 0)
            return;

        // Calculate average response time
        AverageResponseTime = TimeSpan.FromMilliseconds(
            _recentDataPoints.Average(dp => dp.Duration.TotalMilliseconds));

        // Calculate P95 response time
        var sortedDurations = _recentDataPoints
            .Select(dp => dp.Duration.TotalMilliseconds)
            .OrderBy(d => d)
            .ToList();

        if (sortedDurations.Count > 0)
        {
            var p95Index = (int)Math.Ceiling(sortedDurations.Count * 0.95) - 1;
            p95Index = Math.Max(0, Math.Min(p95Index, sortedDurations.Count - 1));
            P95ResponseTime = TimeSpan.FromMilliseconds(sortedDurations[p95Index]);
        }

        // Calculate success rate
        var successfulOperations = _recentDataPoints.Count(dp => dp.Success);
        SuccessRate = _recentDataPoints.Count > 0 ? (double)successfulOperations / _recentDataPoints.Count * 100.0 : 100.0;
    }
}

/// <summary>
/// Overall performance summary
/// </summary>
public class PerformanceSummary
{
    public DateTime GeneratedAt { get; set; }
    public long TotalOperations { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public double SlaCompliance { get; set; }
    public List<OperationTypeMetrics> OperationTypes { get; set; } = new();
    public List<SlaViolation> RecentSlaViolations { get; set; } = new();
}

/// <summary>
/// Performance metrics for an operation type
/// </summary>
public class OperationTypeMetrics
{
    public string OperationType { get; set; } = "";
    public long TotalOperations { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public double SuccessRate { get; set; }
    public double SlaCompliance { get; set; }
}

/// <summary>
/// SLA violation record
/// </summary>
public class SlaViolation
{
    public Guid Id { get; set; }
    public string OperationType { get; set; } = "";
    public string OperationName { get; set; } = "";
    public TimeSpan ActualDuration { get; set; }
    public TimeSpan ExpectedDuration { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Performance health status
/// </summary>
public class PerformanceHealthStatus
{
    public DateTime CheckedAt { get; set; }
    public PerformanceHealth OverallHealth { get; set; }
    public double SlaCompliance { get; set; }
    public List<string> ActiveAlerts { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Performance health levels
/// </summary>
public enum PerformanceHealth
{
    Poor,
    Fair,
    Good,
    Excellent
}