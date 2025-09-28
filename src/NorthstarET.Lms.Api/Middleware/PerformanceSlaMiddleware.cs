using NorthstarET.Lms.Infrastructure.Performance;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace NorthstarET.Lms.Api.Middleware;

/// <summary>
/// Middleware that enforces performance SLA requirements and tracks response times
/// Implements PR-001 (CRUD less than 200ms p95), PR-002 (bulk less than 120s), PR-003 (audit less than 2s)
/// </summary>
public class PerformanceSlaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceSlaMiddleware> _logger;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly PerformanceSlaOptions _options;

    // SLA thresholds based on requirements
    private readonly Dictionary<string, TimeSpan> _slaThresholds = new()
    {
        // CRUD operations - PR-001: <200ms at 95th percentile
        ["GET"] = TimeSpan.FromMilliseconds(200),
        ["POST"] = TimeSpan.FromMilliseconds(200),
        ["PUT"] = TimeSpan.FromMilliseconds(200),
        ["DELETE"] = TimeSpan.FromMilliseconds(200),
        ["PATCH"] = TimeSpan.FromMilliseconds(200),
        
        // Bulk operations - PR-002: <120s for 10k records
        ["BULK"] = TimeSpan.FromSeconds(120),
        
        // Audit queries - PR-003: <2s for 1M records
        ["AUDIT"] = TimeSpan.FromSeconds(2),
        
        // Default for other operations
        ["DEFAULT"] = TimeSpan.FromMilliseconds(500)
    };

    public PerformanceSlaMiddleware(
        RequestDelegate next,
        ILogger<PerformanceSlaMiddleware> logger,
        IPerformanceMonitor performanceMonitor,
        IOptions<PerformanceSlaOptions> options)
    {
        _next = next;
        _logger = logger;
        _performanceMonitor = performanceMonitor;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationType = DetermineOperationType(context.Request);
        var operationName = GetOperationName(context.Request);
        var slaThreshold = GetSlaThreshold(context.Request, operationType);

        // Add performance context to request
        context.Items["Performance.OperationType"] = operationType;
        context.Items["Performance.OperationName"] = operationName;
        context.Items["Performance.SlaThreshold"] = slaThreshold;
        context.Items["Performance.StartTime"] = DateTime.UtcNow;

        var performanceScope = _performanceMonitor.StartMeasurement(operationType, operationName, new Dictionary<string, object>
        {
            ["HttpMethod"] = context.Request.Method,
            ["Path"] = context.Request.Path.Value ?? "",
            ["QueryString"] = context.Request.QueryString.Value ?? "",
            ["UserAgent"] = context.Request.Headers.UserAgent.FirstOrDefault() ?? "",
            ["RemoteIpAddress"] = context.Connection.RemoteIpAddress?.ToString() ?? ""
        });

        var success = true;
        Exception? thrownException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            success = false;
            thrownException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            // Complete performance measurement
            performanceScope.Complete(success && context.Response.StatusCode < 400);

            // Check SLA compliance
            await CheckSlaComplianceAsync(context, duration, slaThreshold, success, thrownException);

            // Add performance headers to response
            AddPerformanceHeaders(context, duration, slaThreshold);
        }
    }

    private string DetermineOperationType(HttpRequest request)
    {
        var path = request.Path.Value?.ToLowerInvariant() ?? "";
        var method = request.Method.ToUpperInvariant();

        // Check for bulk operations
        if (path.Contains("/bulk") || request.Query.ContainsKey("bulk"))
        {
            return "BULK";
        }

        // Check for audit operations
        if (path.Contains("/audit") || path.Contains("/logs"))
        {
            return "AUDIT";
        }

        // Check for specific operation types
        if (path.Contains("/students") || path.Contains("/staff") || path.Contains("/schools"))
        {
            return "CRUD";
        }

        // Default to HTTP method
        return method;
    }

    private string GetOperationName(HttpRequest request)
    {
        var path = request.Path.Value ?? "";
        var method = request.Method;

        // Extract controller and action from path
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (segments.Length >= 3 && segments[0] == "api" && segments[1] == "v1")
        {
            var controller = segments[2];
            var action = segments.Length > 3 ? segments[3] : method.ToLowerInvariant();
            return $"{controller}.{action}";
        }

        return $"{method}:{path}";
    }

    private TimeSpan GetSlaThreshold(HttpRequest request, string operationType)
    {
        // Check for custom SLA in headers or query parameters
        if (request.Headers.TryGetValue("X-SLA-Threshold", out var headerValue) &&
            int.TryParse(headerValue.FirstOrDefault(), out var customThresholdMs))
        {
            return TimeSpan.FromMilliseconds(customThresholdMs);
        }

        // Use operation type specific threshold
        if (_slaThresholds.TryGetValue(operationType, out var threshold))
        {
            return threshold;
        }

        return _slaThresholds["DEFAULT"];
    }

    private async Task CheckSlaComplianceAsync(
        HttpContext context,
        TimeSpan duration,
        TimeSpan slaThreshold,
        bool success,
        Exception? exception)
    {
        var operationType = (string)context.Items["Performance.OperationType"]!;
        var operationName = (string)context.Items["Performance.OperationName"]!;
        var isSlViolation = duration > slaThreshold;

        // Log SLA violations
        if (isSlViolation)
        {
            var violationSeverity = DetermineViolationSeverity(duration, slaThreshold);
            
            _logger.LogWarning("SLA violation ({Severity}): {OperationType}.{OperationName} took {Duration}ms, threshold: {Threshold}ms, Status: {StatusCode}",
                violationSeverity, operationType, operationName, duration.TotalMilliseconds, slaThreshold.TotalMilliseconds, context.Response.StatusCode);

            // Record detailed SLA violation
            await RecordSlaViolationAsync(context, duration, slaThreshold, violationSeverity, exception);
        }

        // Check for performance degradation patterns
        if (_options.EnablePerformanceTrends)
        {
            await AnalyzePerformanceTrendsAsync(context, duration, operationType, operationName);
        }

        // Apply SLA enforcement actions if enabled
        if (_options.EnableSlaEnforcement && isSlViolation)
        {
            await ApplySlaEnforcementAsync(context, duration, slaThreshold);
        }
    }

    private SlaViolationSeverity DetermineViolationSeverity(TimeSpan actual, TimeSpan threshold)
    {
        var violationRatio = actual.TotalMilliseconds / threshold.TotalMilliseconds;

        if (violationRatio > 5.0) return SlaViolationSeverity.Critical;
        if (violationRatio > 2.0) return SlaViolationSeverity.High;
        if (violationRatio > 1.5) return SlaViolationSeverity.Medium;
        return SlaViolationSeverity.Low;
    }

    private async Task RecordSlaViolationAsync(
        HttpContext context,
        TimeSpan duration,
        TimeSpan threshold,
        SlaViolationSeverity severity,
        Exception? exception)
    {
        var violation = new SlaViolationRecord
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            OperationType = (string)context.Items["Performance.OperationType"]!,
            OperationName = (string)context.Items["Performance.OperationName"]!,
            ActualDuration = duration,
            ThresholdDuration = threshold,
            Severity = severity,
            HttpMethod = context.Request.Method,
            RequestPath = context.Request.Path.Value ?? "",
            StatusCode = context.Response.StatusCode,
            Exception = exception?.Message,
            UserAgent = context.Request.Headers.UserAgent.FirstOrDefault(),
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            RequestSize = context.Request.ContentLength ?? 0,
            ResponseSize = context.Response.ContentLength ?? 0
        };

        // Store violation for analysis and reporting
        try
        {
            // This would typically be stored in a database or logging system
            // For now, we'll use the performance monitor's audit capabilities
            _logger.LogError("SLA Violation Recorded: {@Violation}", violation);
            
            // Could also send to external monitoring systems
            if (_options.EnableExternalAlerting && severity >= SlaViolationSeverity.High)
            {
                await SendExternalAlertAsync(violation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record SLA violation");
        }
    }

    private async Task AnalyzePerformanceTrendsAsync(
        HttpContext context,
        TimeSpan duration,
        string operationType,
        string operationName)
    {
        try
        {
            // Get recent performance metrics for this operation
            var metrics = await _performanceMonitor.GetOperationMetricsAsync(operationType, operationName);
            
            if (metrics != null)
            {
                // Check if current request is significantly slower than average
                var performanceDegradation = duration.TotalMilliseconds / metrics.AverageResponseTime.TotalMilliseconds;
                
                if (performanceDegradation > _options.PerformanceDegradationThreshold)
                {
                    _logger.LogWarning("Performance degradation detected: {OperationType}.{OperationName} is {Ratio:F2}x slower than average",
                        operationType, operationName, performanceDegradation);
                }

                // Check if success rate is dropping
                if (metrics.SuccessRate < _options.MinimumSuccessRate)
                {
                    _logger.LogWarning("Low success rate detected: {OperationType}.{OperationName} has {SuccessRate:P2} success rate",
                        operationType, operationName, metrics.SuccessRate / 100.0);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing performance trends");
        }
    }

    private async Task ApplySlaEnforcementAsync(HttpContext context, TimeSpan duration, TimeSpan threshold)
    {
        try
        {
            var violationSeverity = DetermineViolationSeverity(duration, threshold);

            switch (violationSeverity)
            {
                case SlaViolationSeverity.Critical:
                    // For critical violations, we might want to apply circuit breaker patterns
                    if (_options.EnableCircuitBreaker)
                    {
                        await TriggerCircuitBreakerAsync(context);
                    }
                    break;

                case SlaViolationSeverity.High:
                    // For high violations, add throttling headers
                    if (_options.EnableThrottling)
                    {
                        AddThrottlingHeaders(context);
                    }
                    break;

                case SlaViolationSeverity.Medium:
                case SlaViolationSeverity.Low:
                    // For medium/low violations, just add warning headers
                    AddWarningHeaders(context, duration, threshold);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying SLA enforcement");
        }
    }

    private async Task TriggerCircuitBreakerAsync(HttpContext context)
    {
        // Circuit breaker logic would be implemented here
        // This is a simplified example
        _logger.LogWarning("Circuit breaker triggered for operation: {OperationName}", 
            context.Items["Performance.OperationName"]);
        
        // Could set response headers indicating degraded service
        context.Response.Headers.Add("X-Service-Status", "Degraded");
        
        await Task.CompletedTask;
    }

    private void AddThrottlingHeaders(HttpContext context)
    {
        context.Response.Headers.Add("X-RateLimit-Warning", "Performance threshold exceeded");
        context.Response.Headers.Add("Retry-After", "60"); // Suggest retry after 60 seconds
    }

    private void AddWarningHeaders(HttpContext context, TimeSpan duration, TimeSpan threshold)
    {
        context.Response.Headers.Add("X-Performance-Warning", 
            $"Response time {duration.TotalMilliseconds:F0}ms exceeded threshold {threshold.TotalMilliseconds:F0}ms");
    }

    private void AddPerformanceHeaders(HttpContext context, TimeSpan duration, TimeSpan threshold)
    {
        if (_options.IncludePerformanceHeaders)
        {
            context.Response.Headers.Add("X-Response-Time", $"{duration.TotalMilliseconds:F2}");
            context.Response.Headers.Add("X-SLA-Threshold", $"{threshold.TotalMilliseconds:F0}");
            context.Response.Headers.Add("X-SLA-Compliant", (duration <= threshold).ToString().ToLowerInvariant());
            
            if (context.Items.ContainsKey("Performance.StartTime"))
            {
                var startTime = (DateTime)context.Items["Performance.StartTime"]!;
                context.Response.Headers.Add("X-Processing-Time", $"{(DateTime.UtcNow - startTime).TotalMilliseconds:F2}");
            }
        }
    }

    private async Task SendExternalAlertAsync(SlaViolationRecord violation)
    {
        try
        {
            // This would integrate with external monitoring/alerting systems
            // Examples: PagerDuty, Slack, Teams, email notifications, etc.
            _logger.LogInformation("External alert would be sent for SLA violation: {ViolationId}", violation.Id);
            
            // Example implementation might look like:
            // await _alertingService.SendAlertAsync(violation);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send external alert for SLA violation");
        }
    }
}

/// <summary>
/// Configuration options for Performance SLA middleware
/// </summary>
public class PerformanceSlaOptions
{
    public bool IncludePerformanceHeaders { get; set; } = true;
    public bool EnableSlaEnforcement { get; set; } = false;
    public bool EnablePerformanceTrends { get; set; } = true;
    public bool EnableExternalAlerting { get; set; } = false;
    public bool EnableCircuitBreaker { get; set; } = false;
    public bool EnableThrottling { get; set; } = false;
    public double PerformanceDegradationThreshold { get; set; } = 2.0; // 2x slower than average
    public double MinimumSuccessRate { get; set; } = 95.0; // 95%
    public Dictionary<string, int> CustomSlaThresholds { get; set; } = new();
}

/// <summary>
/// SLA violation record for tracking and analysis
/// </summary>
public class SlaViolationRecord
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string OperationType { get; set; } = "";
    public string OperationName { get; set; } = "";
    public TimeSpan ActualDuration { get; set; }
    public TimeSpan ThresholdDuration { get; set; }
    public SlaViolationSeverity Severity { get; set; }
    public string HttpMethod { get; set; } = "";
    public string RequestPath { get; set; } = "";
    public int StatusCode { get; set; }
    public string? Exception { get; set; }
    public string? UserAgent { get; set; }
    public string? RemoteIpAddress { get; set; }
    public long RequestSize { get; set; }
    public long ResponseSize { get; set; }
    
    public double ViolationRatio => ActualDuration.TotalMilliseconds / ThresholdDuration.TotalMilliseconds;
}

/// <summary>
/// SLA violation severity levels
/// </summary>
public enum SlaViolationSeverity
{
    Low,      // 1.5x - 2x threshold
    Medium,   // 2x - 5x threshold  
    High,     // 5x+ threshold
    Critical  // Extreme violations or system errors
}

/// <summary>
/// Extension methods for registering Performance SLA middleware
/// </summary>
public static class PerformanceSlaMiddlewareExtensions
{
    /// <summary>
    /// Registers the Performance SLA middleware with the application pipeline
    /// </summary>
    public static IApplicationBuilder UsePerformanceSla(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceSlaMiddleware>();
    }

    /// <summary>
    /// Registers Performance SLA services with the DI container
    /// </summary>
    public static IServiceCollection AddPerformanceSla(this IServiceCollection services, 
        Action<PerformanceSlaOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<PerformanceSlaOptions>(_ => { });
        }

        return services;
    }
}