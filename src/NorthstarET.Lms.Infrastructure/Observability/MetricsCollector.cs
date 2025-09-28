using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace NorthstarET.Lms.Infrastructure.Observability;

/// <summary>
/// Service for collecting real-time performance metrics and system telemetry
/// Implements comprehensive observability for SLA monitoring and capacity planning
/// </summary>
public class MetricsCollector : BackgroundService, IMetricsCollector
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsCollector> _logger;
    private readonly MetricsCollectionOptions _options;
    private readonly ConcurrentDictionary<string, MetricSeries> _metricSeries = new();
    private readonly Timer _systemMetricsTimer;
    private readonly Timer _reportingTimer;

    // Performance counters for system metrics
    private readonly Process _currentProcess = Process.GetCurrentProcess();
    private long _previousCpuTime = 0;
    private DateTime _previousMeasurementTime = DateTime.UtcNow;

    public MetricsCollector(
        IServiceProvider serviceProvider,
        ILogger<MetricsCollector> logger,
        MetricsCollectionOptions options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        
        // Set up periodic system metrics collection
        _systemMetricsTimer = new Timer(CollectSystemMetrics, null,
            TimeSpan.Zero, _options.SystemMetricsInterval);
            
        // Set up periodic reporting
        _reportingTimer = new Timer(GenerateMetricsReport, null,
            _options.ReportingInterval, _options.ReportingInterval);
    }

    /// <summary>
    /// Records a custom metric value
    /// </summary>
    public async Task RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        var metric = new MetricDataPoint
        {
            Name = metricName,
            Value = value,
            Timestamp = DateTime.UtcNow,
            Tags = tags ?? new Dictionary<string, string>()
        };

        var seriesKey = BuildSeriesKey(metricName, tags);
        var series = _metricSeries.GetOrAdd(seriesKey, _ => new MetricSeries(metricName, tags));
        
        await series.AddDataPointAsync(metric);

        _logger.LogTrace("Metric recorded: {MetricName} = {Value}", metricName, value);
    }

    /// <summary>
    /// Records a counter increment
    /// </summary>
    public async Task IncrementCounterAsync(string counterName, long increment = 1, Dictionary<string, string>? tags = null)
    {
        await RecordMetricAsync($"{counterName}.count", increment, tags);
    }

    /// <summary>
    /// Records a gauge value (point-in-time measurement)
    /// </summary>
    public async Task RecordGaugeAsync(string gaugeName, double value, Dictionary<string, string>? tags = null)
    {
        await RecordMetricAsync($"{gaugeName}.gauge", value, tags);
    }

    /// <summary>
    /// Records a histogram value (for measuring distributions)
    /// </summary>
    public async Task RecordHistogramAsync(string histogramName, double value, Dictionary<string, string>? tags = null)
    {
        var enhancedTags = new Dictionary<string, string>(tags ?? new Dictionary<string, string>());
        
        // Add percentile buckets
        enhancedTags["bucket"] = GetHistogramBucket(value);
        
        await RecordMetricAsync($"{histogramName}.histogram", value, enhancedTags);
    }

    /// <summary>
    /// Records a timer measurement
    /// </summary>
    public async Task RecordTimerAsync(string timerName, TimeSpan duration, Dictionary<string, string>? tags = null)
    {
        await RecordHistogramAsync($"{timerName}.timer", duration.TotalMilliseconds, tags);
    }

    /// <summary>
    /// Gets current metrics for a specific series
    /// </summary>
    public async Task<MetricSeries?> GetMetricSeriesAsync(string metricName, Dictionary<string, string>? tags = null)
    {
        await Task.CompletedTask;
        var seriesKey = BuildSeriesKey(metricName, tags);
        return _metricSeries.GetValueOrDefault(seriesKey);
    }

    /// <summary>
    /// Gets a snapshot of all current metrics
    /// </summary>
    public async Task<MetricsSnapshot> GetMetricsSnapshotAsync()
    {
        var snapshot = new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SystemMetrics = await CollectCurrentSystemMetricsAsync(),
            ApplicationMetrics = _metricSeries.Values.ToList(),
            TotalSeries = _metricSeries.Count
        };

        return snapshot;
    }

    /// <summary>
    /// Gets performance health metrics
    /// </summary>
    public async Task<PerformanceHealthMetrics> GetPerformanceHealthAsync()
    {
        var systemMetrics = await CollectCurrentSystemMetricsAsync();
        
        var health = new PerformanceHealthMetrics
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = systemMetrics.CpuUsagePercent,
            MemoryUsageMB = systemMetrics.MemoryUsageMB,
            GcPressure = CalculateGcPressure(),
            ThreadCount = systemMetrics.ThreadCount,
            HealthScore = CalculateHealthScore(systemMetrics)
        };

        // Add application-specific health indicators
        health.RequestThroughput = await CalculateRequestThroughputAsync();
        health.AverageResponseTime = await CalculateAverageResponseTimeAsync();
        health.ErrorRate = await CalculateErrorRateAsync();

        return health;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics collector started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectApplicationMetricsAsync(stoppingToken);
                await Task.Delay(_options.CollectionInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in metrics collection service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Metrics collector stopped");
    }

    private async Task CollectApplicationMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Collect application-specific metrics
            using var scope = _serviceProvider.CreateScope();
            
            // Database connection pool metrics (if available)
            await CollectDatabaseMetricsAsync(scope, cancellationToken);
            
            // Cache hit ratios and performance
            await CollectCacheMetricsAsync(scope, cancellationToken);
            
            // HTTP request metrics
            await CollectHttpMetricsAsync(scope, cancellationToken);
            
            // Business logic metrics
            await CollectBusinessMetricsAsync(scope, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error collecting application metrics");
        }
    }

    private async Task CollectDatabaseMetricsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            // This would integrate with EF Core or database connection pool metrics
            // Example metrics: active connections, query duration, deadlocks, etc.
            
            await RecordGaugeAsync("database.active_connections", GetActiveConnectionCount());
            await RecordGaugeAsync("database.pool_size", GetConnectionPoolSize());
            await RecordCounterAsync("database.queries_executed", GetQueriesExecutedCount());
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error collecting database metrics");
        }
    }

    private async Task CollectCacheMetricsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var cachingService = scope.ServiceProvider.GetService<Application.Interfaces.ICachingService>();
            if (cachingService != null)
            {
                var cacheStats = await cachingService.GetCacheStatisticsAsync(cancellationToken);
                
                await RecordGaugeAsync("cache.hit_ratio", cacheStats.HitRatio);
                await RecordCounterAsync("cache.total_operations", cacheStats.TotalOperations);
                
                foreach (var layerStat in cacheStats.LayerStats)
                {
                    var layerTags = new Dictionary<string, string> { ["layer"] = layerStat.Key.ToString().ToLowerInvariant() };
                    await RecordCounterAsync("cache.layer_hits", layerStat.Value.HitCount, layerTags);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error collecting cache metrics");
        }
    }

    private async Task CollectHttpMetricsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            // HTTP request metrics would typically be collected by middleware
            // Here we might aggregate or calculate derived metrics
            
            var httpMetrics = GetHttpRequestMetrics();
            await RecordGaugeAsync("http.requests_per_second", httpMetrics.RequestsPerSecond);
            await RecordGaugeAsync("http.average_response_time", httpMetrics.AverageResponseTime);
            await RecordGaugeAsync("http.error_rate", httpMetrics.ErrorRate);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error collecting HTTP metrics");
        }
    }

    private async Task CollectBusinessMetricsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            // Business-specific metrics for the LMS
            await RecordGaugeAsync("lms.active_students", await GetActiveStudentCountAsync(scope));
            await RecordGaugeAsync("lms.active_districts", await GetActiveDistrictCountAsync(scope));
            await RecordGaugeAsync("lms.daily_enrollments", await GetDailyEnrollmentCountAsync(scope));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error collecting business metrics");
        }
    }

    private void CollectSystemMetrics(object? state)
    {
        try
        {
            var systemMetrics = CollectCurrentSystemMetricsAsync().GetAwaiter().GetResult();
            
            // Record system metrics
            _ = Task.Run(async () =>
            {
                await RecordGaugeAsync("system.cpu_usage", systemMetrics.CpuUsagePercent);
                await RecordGaugeAsync("system.memory_usage", systemMetrics.MemoryUsageMB);
                await RecordGaugeAsync("system.thread_count", systemMetrics.ThreadCount);
                await RecordGaugeAsync("system.handle_count", systemMetrics.HandleCount);
                await RecordGaugeAsync("system.gc_gen0_collections", GC.CollectionCount(0));
                await RecordGaugeAsync("system.gc_gen1_collections", GC.CollectionCount(1));
                await RecordGaugeAsync("system.gc_gen2_collections", GC.CollectionCount(2));
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error collecting system metrics");
        }
    }

    private async Task<SystemMetrics> CollectCurrentSystemMetricsAsync()
    {
        _currentProcess.Refresh();
        
        // Calculate CPU usage
        var currentCpuTime = _currentProcess.TotalProcessorTime.Ticks;
        var currentTime = DateTime.UtcNow;
        var cpuUsagePercent = 0.0;

        if (_previousCpuTime != 0)
        {
            var cpuTimeDiff = currentCpuTime - _previousCpuTime;
            var timeDiff = (currentTime - _previousMeasurementTime).Ticks;
            
            if (timeDiff > 0)
            {
                cpuUsagePercent = (double)cpuTimeDiff / timeDiff * 100.0;
            }
        }

        _previousCpuTime = currentCpuTime;
        _previousMeasurementTime = currentTime;

        var metrics = new SystemMetrics
        {
            CpuUsagePercent = Math.Min(cpuUsagePercent, 100.0),
            MemoryUsageMB = _currentProcess.WorkingSet64 / 1024.0 / 1024.0,
            ThreadCount = _currentProcess.Threads.Count,
            HandleCount = _currentProcess.HandleCount,
            Uptime = DateTime.UtcNow - _currentProcess.StartTime,
            GcTotalMemory = GC.GetTotalMemory(false) / 1024.0 / 1024.0
        };

        await Task.CompletedTask;
        return metrics;
    }

    private double CalculateGcPressure()
    {
        // Simple GC pressure calculation based on collection frequency
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        
        // Weight higher generation collections more heavily
        return (gen0 * 0.1) + (gen1 * 0.3) + (gen2 * 1.0);
    }

    private double CalculateHealthScore(SystemMetrics systemMetrics)
    {
        var score = 100.0;
        
        // Deduct points for high resource usage
        if (systemMetrics.CpuUsagePercent > 80) score -= 20;
        else if (systemMetrics.CpuUsagePercent > 60) score -= 10;
        
        if (systemMetrics.MemoryUsageMB > 1024) score -= 20; // >1GB
        else if (systemMetrics.MemoryUsageMB > 512) score -= 10; // >512MB
        
        if (systemMetrics.ThreadCount > 100) score -= 10;
        
        return Math.Max(0, score);
    }

    private async Task<double> CalculateRequestThroughputAsync()
    {
        var httpSeries = _metricSeries.Values
            .FirstOrDefault(s => s.MetricName.StartsWith("http.requests"));
            
        if (httpSeries?.DataPoints.Any() == true)
        {
            var recentPoints = httpSeries.DataPoints
                .Where(p => p.Timestamp > DateTime.UtcNow.AddMinutes(-1))
                .ToList();
                
            return recentPoints.Count; // Requests per minute
        }
        
        await Task.CompletedTask;
        return 0;
    }

    private async Task<double> CalculateAverageResponseTimeAsync()
    {
        var responseTimeSeries = _metricSeries.Values
            .FirstOrDefault(s => s.MetricName.Contains("response_time"));
            
        if (responseTimeSeries?.DataPoints.Any() == true)
        {
            var recentPoints = responseTimeSeries.DataPoints
                .Where(p => p.Timestamp > DateTime.UtcNow.AddMinutes(-5))
                .ToList();
                
            return recentPoints.Any() ? recentPoints.Average(p => p.Value) : 0;
        }
        
        await Task.CompletedTask;
        return 0;
    }

    private async Task<double> CalculateErrorRateAsync()
    {
        var errorSeries = _metricSeries.Values
            .FirstOrDefault(s => s.MetricName.Contains("error"));
            
        var totalSeries = _metricSeries.Values
            .FirstOrDefault(s => s.MetricName.Contains("total"));
            
        if (errorSeries?.DataPoints.Any() == true && totalSeries?.DataPoints.Any() == true)
        {
            var recentWindow = DateTime.UtcNow.AddMinutes(-5);
            var recentErrors = errorSeries.DataPoints.Where(p => p.Timestamp > recentWindow).Sum(p => p.Value);
            var recentTotal = totalSeries.DataPoints.Where(p => p.Timestamp > recentWindow).Sum(p => p.Value);
            
            return recentTotal > 0 ? (recentErrors / recentTotal) * 100.0 : 0;
        }
        
        await Task.CompletedTask;
        return 0;
    }

    private string BuildSeriesKey(string metricName, Dictionary<string, string>? tags)
    {
        var key = metricName;
        if (tags?.Any() == true)
        {
            var tagString = string.Join(",", tags.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"));
            key += $"|{tagString}";
        }
        return key;
    }

    private string GetHistogramBucket(double value)
    {
        // Define histogram buckets for response time measurements
        if (value <= 10) return "0-10ms";
        if (value <= 50) return "10-50ms";
        if (value <= 100) return "50-100ms";
        if (value <= 200) return "100-200ms";
        if (value <= 500) return "200-500ms";
        if (value <= 1000) return "500-1000ms";
        if (value <= 5000) return "1000-5000ms";
        return "5000ms+";
    }

    private void GenerateMetricsReport(object? state)
    {
        try
        {
            _ = Task.Run(async () =>
            {
                var snapshot = await GetMetricsSnapshotAsync();
                _logger.LogInformation("Metrics Report: {SeriesCount} series, CPU: {CpuUsage:F1}%, Memory: {MemoryUsage:F1}MB",
                    snapshot.TotalSeries, 
                    snapshot.SystemMetrics.CpuUsagePercent,
                    snapshot.SystemMetrics.MemoryUsageMB);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating metrics report");
        }
    }

    // Placeholder methods for database and HTTP metrics
    private int GetActiveConnectionCount() => 0;
    private int GetConnectionPoolSize() => 10;
    private long GetQueriesExecutedCount() => 0;
    private HttpMetrics GetHttpRequestMetrics() => new();
    private async Task<long> GetActiveStudentCountAsync(IServiceScope scope) { await Task.CompletedTask; return 0; }
    private async Task<long> GetActiveDistrictCountAsync(IServiceScope scope) { await Task.CompletedTask; return 0; }
    private async Task<long> GetDailyEnrollmentCountAsync(IServiceScope scope) { await Task.CompletedTask; return 0; }

    public override void Dispose()
    {
        _systemMetricsTimer?.Dispose();
        _reportingTimer?.Dispose();
        _currentProcess?.Dispose();
        base.Dispose();
    }
}

// Supporting classes and interfaces...

/// <summary>
/// Interface for metrics collection service
/// </summary>
public interface IMetricsCollector
{
    Task RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null);
    Task IncrementCounterAsync(string counterName, long increment = 1, Dictionary<string, string>? tags = null);
    Task RecordGaugeAsync(string gaugeName, double value, Dictionary<string, string>? tags = null);
    Task RecordHistogramAsync(string histogramName, double value, Dictionary<string, string>? tags = null);
    Task RecordTimerAsync(string timerName, TimeSpan duration, Dictionary<string, string>? tags = null);
    Task<MetricSeries?> GetMetricSeriesAsync(string metricName, Dictionary<string, string>? tags = null);
    Task<MetricsSnapshot> GetMetricsSnapshotAsync();
    Task<PerformanceHealthMetrics> GetPerformanceHealthAsync();
}

/// <summary>
/// Configuration options for metrics collection
/// </summary>
public class MetricsCollectionOptions
{
    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan SystemMetricsInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan ReportingInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxDataPointsPerSeries { get; set; } = 1000;
    public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromHours(24);
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// Individual metric data point
/// </summary>
public class MetricDataPoint
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Time series of metric data points
/// </summary>
public class MetricSeries
{
    private readonly object _lock = new();
    private readonly List<MetricDataPoint> _dataPoints = new();

    public string MetricName { get; }
    public Dictionary<string, string>? Tags { get; }
    public IReadOnlyList<MetricDataPoint> DataPoints 
    { 
        get 
        { 
            lock (_lock) 
            { 
                return _dataPoints.ToList().AsReadOnly(); 
            } 
        } 
    }

    public MetricSeries(string metricName, Dictionary<string, string>? tags = null)
    {
        MetricName = metricName;
        Tags = tags;
    }

    public async Task AddDataPointAsync(MetricDataPoint dataPoint)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                _dataPoints.Add(dataPoint);
                
                // Keep only recent data points to prevent memory issues
                if (_dataPoints.Count > 1000)
                {
                    _dataPoints.RemoveRange(0, 100); // Remove oldest 100 points
                }
            }
        });
    }

    public void CleanupOldData(DateTime cutoffTime)
    {
        lock (_lock)
        {
            _dataPoints.RemoveAll(dp => dp.Timestamp < cutoffTime);
        }
    }
}

/// <summary>
/// System performance metrics
/// </summary>
public class SystemMetrics
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsageMB { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public TimeSpan Uptime { get; set; }
    public double GcTotalMemory { get; set; }
}

/// <summary>
/// Complete metrics snapshot
/// </summary>
public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public SystemMetrics SystemMetrics { get; set; } = new();
    public List<MetricSeries> ApplicationMetrics { get; set; } = new();
    public int TotalSeries { get; set; }
}

/// <summary>
/// Performance health metrics
/// </summary>
public class PerformanceHealthMetrics
{
    public DateTime Timestamp { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryUsageMB { get; set; }
    public double GcPressure { get; set; }
    public int ThreadCount { get; set; }
    public double HealthScore { get; set; }
    public double RequestThroughput { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
}

/// <summary>
/// HTTP request metrics
/// </summary>
public class HttpMetrics
{
    public double RequestsPerSecond { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
}