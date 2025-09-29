using NorthstarET.Lms.Application.Services;
using System.Security.Claims;
using System.Text.Json;

namespace NorthstarET.Lms.Api.Middleware;

/// <summary>
/// Middleware to monitor and detect suspicious security activities,
/// including potential data breaches, unauthorized access attempts, and policy violations.
/// </summary>
public class SecurityMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMonitoringMiddleware> _logger;
    private readonly ISecurityMonitoringService _securityService;

    public SecurityMonitoringMiddleware(
        RequestDelegate next,
        ILogger<SecurityMonitoringMiddleware> logger,
        ISecurityMonitoringService securityService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTimeOffset.UtcNow;
        var securityContext = BuildSecurityContext(context);

        // Pre-request security checks
        var preRequestThreat = await AnalyzePreRequestSecurity(securityContext);
        if (preRequestThreat.ShouldBlock)
        {
            await HandleSecurityThreat(context, preRequestThreat);
            return;
        }

        Exception? exception = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            // Post-request security analysis
            var duration = DateTimeOffset.UtcNow - startTime;
            securityContext.ResponseStatusCode = context.Response.StatusCode;
            securityContext.Duration = duration;
            securityContext.Exception = exception;

            _ = Task.Run(async () => await AnalyzePostRequestSecurity(securityContext));
        }
    }

    private SecurityContext BuildSecurityContext(HttpContext context)
    {
        return new SecurityContext
        {
            RequestId = context.TraceIdentifier,
            Timestamp = DateTimeOffset.UtcNow,
            UserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            TenantId = context.User?.FindFirst("tenant_id")?.Value,
            UserRoles = context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>(),
            IpAddress = GetClientIpAddress(context),
            UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            QueryString = context.Request.QueryString.Value,
            Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.AsEnumerable()),
            IsAuthenticated = context.User?.Identity?.IsAuthenticated == true,
            AuthenticationType = context.User?.Identity?.AuthenticationType
        };
    }

    private async Task<SecurityThreatAssessment> AnalyzePreRequestSecurity(SecurityContext context)
    {
        var threats = new List<SecurityThreat>();

        // 1. Rate limiting detection
        var rateLimitThreat = await CheckRateLimiting(context);
        if (rateLimitThreat != null) threats.Add(rateLimitThreat);

        // 2. Suspicious IP patterns
        var ipThreat = await CheckSuspiciousIpActivity(context);
        if (ipThreat != null) threats.Add(ipThreat);

        // 3. Authentication anomalies
        var authThreat = await CheckAuthenticationAnomalies(context);
        if (authThreat != null) threats.Add(authThreat);

        // 4. Cross-tenant access attempts
        var tenantThreat = await CheckCrossTenantAccess(context);
        if (tenantThreat != null) threats.Add(tenantThreat);

        // 5. Privilege escalation attempts
        var privilegeThreat = await CheckPrivilegeEscalation(context);
        if (privilegeThreat != null) threats.Add(privilegeThreat);

        var highestSeverity = threats.Any() ? threats.Max(t => t.Severity) : SecurityThreatSeverity.None;
        var shouldBlock = highestSeverity >= SecurityThreatSeverity.Critical;

        return new SecurityThreatAssessment
        {
            Threats = threats,
            HighestSeverity = highestSeverity,
            ShouldBlock = shouldBlock,
            RecommendedAction = DetermineRecommendedAction(threats)
        };
    }

    private async Task AnalyzePostRequestSecurity(SecurityContext context)
    {
        var threats = new List<SecurityThreat>();

        // 1. Data exfiltration detection (large responses)
        if (IsLargeDataResponse(context))
        {
            threats.Add(new SecurityThreat
            {
                Type = SecurityThreatType.DataExfiltration,
                Severity = SecurityThreatSeverity.High,
                Description = $"Large data response detected: {context.ResponseSize} bytes",
                Context = context
            });
        }

        // 2. Error pattern analysis
        if (context.ResponseStatusCode >= 400)
        {
            var errorThreat = await AnalyzeErrorPatterns(context);
            if (errorThreat != null) threats.Add(errorThreat);
        }

        // 3. Performance anomaly detection
        if (context.Duration > TimeSpan.FromSeconds(30))
        {
            threats.Add(new SecurityThreat
            {
                Type = SecurityThreatType.PerformanceAnomaly,
                Severity = SecurityThreatSeverity.Medium,
                Description = $"Slow request detected: {context.Duration.TotalSeconds}s",
                Context = context
            });
        }

        // 4. Compliance violation detection
        var complianceThreat = await CheckComplianceViolations(context);
        if (complianceThreat != null) threats.Add(complianceThreat);

        // Log threats and trigger alerts
        if (threats.Any())
        {
            await _securityService.ProcessSecurityThreats(threats);
        }
    }

    private async Task<SecurityThreat?> CheckRateLimiting(SecurityContext context)
    {
        var rateLimitCheck = await _securityService.CheckRateLimit(
            context.IpAddress, 
            context.UserId, 
            context.Path);

        if (rateLimitCheck.IsExceeded)
        {
            return new SecurityThreat
            {
                Type = SecurityThreatType.RateLimitExceeded,
                Severity = SecurityThreatSeverity.High,
                Description = $"Rate limit exceeded: {rateLimitCheck.RequestCount} requests in {rateLimitCheck.TimeWindow}",
                Context = context
            };
        }

        return null;
    }

    private async Task<SecurityThreat?> CheckSuspiciousIpActivity(SecurityContext context)
    {
        var ipAnalysis = await _securityService.AnalyzeIpActivity(context.IpAddress);
        
        if (ipAnalysis.IsSuspicious)
        {
            return new SecurityThreat
            {
                Type = SecurityThreatType.SuspiciousIpActivity,
                Severity = ipAnalysis.Severity,
                Description = $"Suspicious IP activity detected: {string.Join(", ", ipAnalysis.SuspiciousIndicators)}",
                Context = context
            };
        }

        return null;
    }

    private async Task<SecurityThreat?> CheckAuthenticationAnomalies(SecurityContext context)
    {
        if (!context.IsAuthenticated)
            return null;

        var authAnalysis = await _securityService.AnalyzeAuthenticationAnomaly(
            context.UserId!, 
            context.IpAddress, 
            context.UserAgent);

        if (authAnalysis.IsAnomalous)
        {
            return new SecurityThreat
            {
                Type = SecurityThreatType.AuthenticationAnomaly,
                Severity = authAnalysis.Severity,
                Description = $"Authentication anomaly detected: {authAnalysis.AnomalyDescription}",
                Context = context
            };
        }

        return null;
    }

    private async Task<SecurityThreat?> CheckCrossTenantAccess(SecurityContext context)
    {
        if (string.IsNullOrEmpty(context.TenantId))
            return null;

        var crossTenantCheck = await _securityService.CheckCrossTenantAccess(
            context.UserId!,
            context.TenantId,
            context.Path);

        if (crossTenantCheck.IsViolation)
        {
            return new SecurityThreat
            {
                Type = SecurityThreatType.CrossTenantViolation,
                Severity = SecurityThreatSeverity.Critical,
                Description = $"Cross-tenant access violation: User accessing unauthorized tenant data",
                Context = context
            };
        }

        return null;
    }

    private async Task<SecurityThreat?> CheckPrivilegeEscalation(SecurityContext context)
    {
        var privilegeCheck = await _securityService.CheckPrivilegeEscalation(
            context.UserId!,
            context.UserRoles,
            context.Path,
            context.Method);

        if (privilegeCheck.IsViolation)
        {
            return new SecurityThreat
            {
                Type = SecurityThreatType.PrivilegeEscalation,
                Severity = SecurityThreatSeverity.Critical,
                Description = $"Privilege escalation attempt detected: {privilegeCheck.ViolationDescription}",
                Context = context
            };
        }

        return null;
    }

    private async Task<SecurityThreat?> AnalyzeErrorPatterns(SecurityContext context)
    {
        var errorPattern = await _securityService.AnalyzeErrorPattern(
            context.UserId,
            context.IpAddress,
            context.ResponseStatusCode,
            context.Path);

        if (errorPattern.IsSuspicious)
        {
            return new SecurityThreat
            {
                Type = SecurityThreatType.SuspiciousErrorPattern,
                Severity = errorPattern.Severity,
                Description = $"Suspicious error pattern detected: {errorPattern.PatternDescription}",
                Context = context
            };
        }

        return null;
    }

    private async Task<SecurityThreat?> CheckComplianceViolations(SecurityContext context)
    {
        var complianceCheck = await _securityService.CheckComplianceViolation(context);

        if (complianceCheck.HasViolation)
        {
            return new SecurityThreat
            {
                Type = SecurityThreatType.ComplianceViolation,
                Severity = SecurityThreatSeverity.High,
                Description = $"Compliance violation detected: {complianceCheck.ViolationType}",
                Context = context
            };
        }

        return null;
    }

    private static bool IsLargeDataResponse(SecurityContext context)
    {
        // Flag responses larger than 10MB as potentially suspicious
        return context.ResponseSize > 10 * 1024 * 1024;
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Try to get real IP from headers (reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string DetermineRecommendedAction(List<SecurityThreat> threats)
    {
        var criticalThreats = threats.Where(t => t.Severity == SecurityThreatSeverity.Critical).ToList();
        var highThreats = threats.Where(t => t.Severity == SecurityThreatSeverity.High).ToList();

        if (criticalThreats.Any())
        {
            return $"BLOCK REQUEST - Critical threats: {string.Join(", ", criticalThreats.Select(t => t.Type))}";
        }

        if (highThreats.Count >= 2)
        {
            return $"ALERT - Multiple high-severity threats: {string.Join(", ", highThreats.Select(t => t.Type))}";
        }

        if (highThreats.Any())
        {
            return $"MONITOR - High-severity threat: {highThreats.First().Type}";
        }

        return "ALLOW - No significant threats detected";
    }

    private async Task HandleSecurityThreat(HttpContext context, SecurityThreatAssessment assessment)
    {
        _logger.LogWarning("Security threat blocked: {Threats} for IP {IpAddress} User {UserId}",
            string.Join(", ", assessment.Threats.Select(t => $"{t.Type}:{t.Severity}")),
            GetClientIpAddress(context),
            context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous");

        // Log security incident
        await _securityService.LogSecurityIncident(assessment);

        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Security threat detected",
            message = "Request blocked due to security policy violation",
            correlationId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

// Supporting types for security monitoring
public class SecurityContext
{
    public string RequestId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public string[] UserRoles { get; set; } = Array.Empty<string>();
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? QueryString { get; set; }
    public Dictionary<string, IEnumerable<string>> Headers { get; set; } = new();
    public bool IsAuthenticated { get; set; }
    public string? AuthenticationType { get; set; }
    public int ResponseStatusCode { get; set; }
    public long ResponseSize { get; set; }
    public TimeSpan Duration { get; set; }
    public Exception? Exception { get; set; }
}

public class SecurityThreatAssessment
{
    public List<SecurityThreat> Threats { get; set; } = new();
    public SecurityThreatSeverity HighestSeverity { get; set; }
    public bool ShouldBlock { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
}

public class SecurityThreat
{
    public SecurityThreatType Type { get; set; }
    public SecurityThreatSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public SecurityContext Context { get; set; } = null!;
}

public enum SecurityThreatType
{
    RateLimitExceeded,
    SuspiciousIpActivity,
    AuthenticationAnomaly,
    CrossTenantViolation,
    PrivilegeEscalation,
    DataExfiltration,
    PerformanceAnomaly,
    SuspiciousErrorPattern,
    ComplianceViolation
}

public enum SecurityThreatSeverity
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Interface for security monitoring service
/// </summary>
public interface ISecurityMonitoringService
{
    Task<RateLimitCheck> CheckRateLimit(string ipAddress, string? userId, string? path);
    Task<IpActivityAnalysis> AnalyzeIpActivity(string ipAddress);
    Task<AuthenticationAnomalyAnalysis> AnalyzeAuthenticationAnomaly(string userId, string ipAddress, string? userAgent);
    Task<CrossTenantAccessCheck> CheckCrossTenantAccess(string userId, string tenantId, string? path);
    Task<PrivilegeEscalationCheck> CheckPrivilegeEscalation(string userId, string[] roles, string? path, string method);
    Task<ErrorPatternAnalysis> AnalyzeErrorPattern(string? userId, string ipAddress, int statusCode, string? path);
    Task<ComplianceCheck> CheckComplianceViolation(SecurityContext context);
    Task ProcessSecurityThreats(List<SecurityThreat> threats);
    Task LogSecurityIncident(SecurityThreatAssessment assessment);
}

// Security monitoring types
public class RateLimitCheck
{
    public bool IsLimited { get; set; }
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public TimeSpan Window { get; set; }
}

public class IpActivityAnalysis
{
    public string IpAddress { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int DistinctUsers { get; set; }
    public bool IsSuspicious { get; set; }
    public string? Reason { get; set; }
}

public class AuthenticationAnomalyAnalysis
{
    public string UserId { get; set; } = string.Empty;
    public bool IsAnomalous { get; set; }
    public string? Reason { get; set; }
}

public class CrossTenantAccessCheck
{
    public bool IsViolation { get; set; }
    public string? Reason { get; set; }
}

public class PrivilegeEscalationCheck
{
    public bool IsEscalation { get; set; }
    public string? Reason { get; set; }
}

public class ErrorPatternAnalysis
{
    public bool IsPattern { get; set; }
    public int ErrorCount { get; set; }
    public string? Reason { get; set; }
}

public class ComplianceCheck
{
    public bool IsViolation { get; set; }
    public string? Reason { get; set; }
    public string? ComplianceRule { get; set; }
}

/// <summary>
/// Extension methods for registering security monitoring middleware
/// </summary>
public static class SecurityMonitoringMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityMonitoringMiddleware>();
    }
}