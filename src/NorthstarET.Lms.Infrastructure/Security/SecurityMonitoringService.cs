using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace NorthstarET.Lms.Infrastructure.Security;

/// <summary>
/// Service for monitoring security events and detecting anomalies
/// Implements FR-047 (access anomaly detection) and FR-049 (authorization failure tracking)
/// </summary>
public class SecurityMonitoringService : BackgroundService, ISecurityMonitoringService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecurityMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, UserSecurityMetrics> _userMetrics = new();
    private readonly ConcurrentQueue<SecurityEvent> _eventQueue = new();
    private readonly SecurityMonitoringOptions _options;
    private readonly Timer _analysisTimer;

    public SecurityMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<SecurityMonitoringService> logger,
        SecurityMonitoringOptions options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        
        // Set up periodic analysis timer
        _analysisTimer = new Timer(AnalyzeSecurityMetrics, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Records a security event for analysis
    /// </summary>
    public async Task RecordSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        _eventQueue.Enqueue(securityEvent);
        
        // Update user metrics
        var userId = securityEvent.UserId ?? "anonymous";
        var userMetrics = _userMetrics.GetOrAdd(userId, _ => new UserSecurityMetrics(userId));
        
        await userMetrics.RecordEventAsync(securityEvent);

        // Check for immediate threats that require urgent response
        if (IsImmediateThreat(securityEvent, userMetrics))
        {
            await HandleImmediateThreatAsync(securityEvent, userMetrics, cancellationToken);
        }

        _logger.LogDebug("Recorded security event: {EventType} for user {UserId} from IP {IpAddress}",
            securityEvent.EventType, userId, securityEvent.IpAddress);
    }

    /// <summary>
    /// Records an authentication failure
    /// </summary>
    public async Task RecordAuthenticationFailureAsync(
        string? userId, 
        string ipAddress, 
        string reason,
        CancellationToken cancellationToken = default)
    {
        await RecordSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.AuthenticationFailure,
            UserId = userId,
            IpAddress = ipAddress,
            Details = reason,
            Timestamp = DateTime.UtcNow,
            Severity = SecurityEventSeverity.Medium
        }, cancellationToken);
    }

    /// <summary>
    /// Records an authorization failure
    /// </summary>
    public async Task RecordAuthorizationFailureAsync(
        ClaimsPrincipal user,
        string resource,
        string permission,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        await RecordSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.AuthorizationFailure,
            UserId = userId,
            IpAddress = ipAddress,
            Details = $"Failed to access {resource} with permission {permission}",
            Resource = resource,
            Timestamp = DateTime.UtcNow,
            Severity = SecurityEventSeverity.Medium
        }, cancellationToken);
    }

    /// <summary>
    /// Records suspicious access pattern
    /// </summary>
    public async Task RecordSuspiciousAccessAsync(
        string? userId,
        string ipAddress,
        string pattern,
        SecurityEventSeverity severity = SecurityEventSeverity.High,
        CancellationToken cancellationToken = default)
    {
        await RecordSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.SuspiciousAccess,
            UserId = userId,
            IpAddress = ipAddress,
            Details = pattern,
            Timestamp = DateTime.UtcNow,
            Severity = severity
        }, cancellationToken);
    }

    /// <summary>
    /// Gets security metrics for a user
    /// </summary>
    public async Task<UserSecurityMetrics?> GetUserSecurityMetricsAsync(
        string userId, 
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make async for interface compatibility
        return _userMetrics.GetValueOrDefault(userId);
    }

    /// <summary>
    /// Gets recent security alerts
    /// </summary>
    public async Task<IEnumerable<SecurityAlert>> GetRecentAlertsAsync(
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
        
        try
        {
            // Query audit records for security-related events
            var securityAuditRecords = await auditRepository.QueryAsync(
                entityType: null,
                entityId: null,
                userId: null,
                eventType: "SecurityAlert",
                startDate: null,
                endDate: null,
                page: 1,
                pageSize: count);

            return securityAuditRecords.Items.Select(record => new SecurityAlert
            {
                Id = record.Id,
                AlertType = ParseAlertType(record.AuditData),
                Message = ParseAlertMessage(record.Details),
                Severity = ParseSeverity(record.Details),
                Timestamp = record.Timestamp,
                UserId = record.UserId,
                IsResolved = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security alerts");
            return Enumerable.Empty<SecurityAlert>();
        }
    }

    /// <summary>
    /// Analyzes current security posture
    /// </summary>
    public async Task<SecurityPostureReport> AnalyzeSecurityPostureAsync(CancellationToken cancellationToken = default)
    {
        var report = new SecurityPostureReport
        {
            AnalysisTimestamp = DateTime.UtcNow,
            TotalUsers = _userMetrics.Count,
            ActiveThreats = 0,
            RiskScore = 0
        };

        // Analyze user metrics for threats
        foreach (var userMetric in _userMetrics.Values)
        {
            var riskLevel = CalculateUserRiskLevel(userMetric);
            
            if (riskLevel >= 0.7) // High risk threshold
            {
                report.ActiveThreats++;
                report.HighRiskUsers.Add(new UserRiskProfile
                {
                    UserId = userMetric.UserId,
                    RiskScore = riskLevel,
                    LastActivity = userMetric.LastActivity,
                    ThreatIndicators = GetThreatIndicators(userMetric)
                });
            }
            
            report.RiskScore += riskLevel;
        }

        if (report.TotalUsers > 0)
        {
            report.RiskScore /= report.TotalUsers; // Average risk score
        }

        // Add system-level threat analysis
        report.SystemThreats = await AnalyzeSystemThreatsAsync(cancellationToken);

        _logger.LogInformation("Security posture analysis completed. Risk Score: {RiskScore:F2}, Active Threats: {ActiveThreats}",
            report.RiskScore, report.ActiveThreats);

        return report;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Security monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSecurityEventsAsync(stoppingToken);
                await Task.Delay(_options.ProcessingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in security monitoring service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Security monitoring service stopped");
    }

    private async Task ProcessSecurityEventsAsync(CancellationToken cancellationToken)
    {
        var eventsProcessed = 0;
        
        while (_eventQueue.TryDequeue(out var securityEvent) && eventsProcessed < _options.MaxEventsPerBatch)
        {
            await ProcessSecurityEventAsync(securityEvent, cancellationToken);
            eventsProcessed++;
        }

        if (eventsProcessed > 0)
        {
            _logger.LogDebug("Processed {EventCount} security events", eventsProcessed);
        }
    }

    private async Task ProcessSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Store in audit log
            using var scope = _serviceProvider.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            
            await auditService.LogAsync(
                securityEvent.EventType.ToString(),
                "SecurityEvent",
                Guid.NewGuid(),
                securityEvent.UserId ?? "system",
                new { 
                    Details = securityEvent.Details,
                    IpAddress = securityEvent.IpAddress,
                    Severity = securityEvent.Severity.ToString(),
                    Resource = securityEvent.Resource
                });

            // Check for patterns that require alerting
            if (RequiresAlert(securityEvent))
            {
                await GenerateSecurityAlertAsync(securityEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing security event {EventType}", securityEvent.EventType);
        }
    }

    private bool IsImmediateThreat(SecurityEvent securityEvent, UserSecurityMetrics userMetrics)
    {
        return securityEvent.Severity == SecurityEventSeverity.Critical ||
               userMetrics.FailedLoginAttempts > _options.MaxFailedLogins ||
               IsFromSuspiciousLocation(securityEvent.IpAddress, userMetrics);
    }

    private async Task HandleImmediateThreatAsync(
        SecurityEvent securityEvent, 
        UserSecurityMetrics userMetrics, 
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Immediate threat detected for user {UserId}: {EventType} from {IpAddress}",
            securityEvent.UserId, securityEvent.EventType, securityEvent.IpAddress);

        // Generate critical alert
        await GenerateSecurityAlertAsync(securityEvent, cancellationToken, true);

        // Consider automatic response based on severity
        if (securityEvent.Severity == SecurityEventSeverity.Critical)
        {
            await ConsiderAutomaticResponseAsync(securityEvent, userMetrics, cancellationToken);
        }
    }

    private async Task ConsiderAutomaticResponseAsync(
        SecurityEvent securityEvent,
        UserSecurityMetrics userMetrics,
        CancellationToken cancellationToken)
    {
        // This could include:
        // - Temporary account suspension
        // - IP blocking
        // - Multi-factor authentication requirement
        // - Session termination

        _logger.LogWarning("Automatic security response triggered for user {UserId}", securityEvent.UserId);
        
        // For now, just log - actual response would depend on specific security policies
        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        
        await auditService.LogAsync(
            "AutomaticSecurityResponse",
            "SecurityEvent",
            Guid.NewGuid(),
            "system",
            new { 
                Details = $"Automatic response considered for user {securityEvent.UserId} due to {securityEvent.EventType}",
                Trigger = securityEvent, 
                UserMetrics = userMetrics 
            });
    }

    private bool IsFromSuspiciousLocation(string ipAddress, UserSecurityMetrics userMetrics)
    {
        // Check if IP is from a different geographic location than usual
        // This would require GeoIP lookup service
        return false; // Placeholder implementation
    }

    private bool RequiresAlert(SecurityEvent securityEvent)
    {
        return securityEvent.Severity >= SecurityEventSeverity.High ||
               securityEvent.EventType == SecurityEventType.SuspiciousAccess ||
               securityEvent.EventType == SecurityEventType.DataExfiltration;
    }

    private async Task GenerateSecurityAlertAsync(
        SecurityEvent securityEvent, 
        CancellationToken cancellationToken, 
        bool isCritical = false)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var alertData = new
        {
            AlertType = isCritical ? "CriticalSecurityAlert" : "SecurityAlert",
            Event = securityEvent,
            Timestamp = DateTime.UtcNow,
            RequiresImediateAttention = isCritical
        };

        await auditService.LogAsync(
            "SecurityAlert",
            "SecurityEvent",
            Guid.NewGuid(),
            "system",
            new {
                Details = $"{(isCritical ? "CRITICAL" : "")}: {securityEvent.EventType} detected",
                AlertData = alertData
            });
    }

    private double CalculateUserRiskLevel(UserSecurityMetrics userMetrics)
    {
        var riskFactors = 0.0;
        var maxRiskFactors = 10.0; // Total possible risk factors

        // Failed login attempts
        if (userMetrics.FailedLoginAttempts > 3) riskFactors += Math.Min(userMetrics.FailedLoginAttempts / 10.0, 1.0);

        // Authorization failures
        if (userMetrics.AuthorizationFailures > 5) riskFactors += Math.Min(userMetrics.AuthorizationFailures / 20.0, 1.0);

        // Unusual access times
        if (userMetrics.HasUnusualAccessTimes()) riskFactors += 0.3;

        // Multiple IP addresses
        if (userMetrics.UniqueIpAddresses.Count > 3) riskFactors += 0.2;

        // Recent activity level
        if (userMetrics.LastActivity < DateTime.UtcNow.AddDays(-30)) riskFactors += 0.1;

        return Math.Min(riskFactors / maxRiskFactors, 1.0);
    }

    private List<string> GetThreatIndicators(UserSecurityMetrics userMetrics)
    {
        var indicators = new List<string>();

        if (userMetrics.FailedLoginAttempts > 5)
            indicators.Add($"High failed login attempts: {userMetrics.FailedLoginAttempts}");

        if (userMetrics.AuthorizationFailures > 10)
            indicators.Add($"High authorization failures: {userMetrics.AuthorizationFailures}");

        if (userMetrics.UniqueIpAddresses.Count > 5)
            indicators.Add($"Multiple IP addresses: {userMetrics.UniqueIpAddresses.Count}");

        return indicators;
    }

    private Task<List<string>> AnalyzeSystemThreatsAsync(CancellationToken cancellationToken)
    {
        var threats = new List<string>();

        // Analyze overall system patterns
        var totalFailedLogins = _userMetrics.Values.Sum(m => m.FailedLoginAttempts);
        if (totalFailedLogins > 100)
        {
            threats.Add($"High system-wide failed login attempts: {totalFailedLogins}");
        }

        // Check for coordinated attacks
        var suspiciousIps = _userMetrics.Values
            .SelectMany(m => m.UniqueIpAddresses)
            .GroupBy(ip => ip)
            .Where(g => g.Count() > 10)
            .Select(g => g.Key);

        foreach (var ip in suspiciousIps)
        {
            threats.Add($"Potential coordinated attack from IP: {ip}");
        }

        return Task.FromResult(threats);
    }

    private void AnalyzeSecurityMetrics(object? state)
    {
        try
        {
            // Periodic cleanup of old metrics
            var cutoffTime = DateTime.UtcNow.AddDays(-_options.MetricsRetentionDays);
            var expiredUsers = _userMetrics.Values
                .Where(m => m.LastActivity < cutoffTime)
                .Select(m => m.UserId)
                .ToList();

            foreach (var userId in expiredUsers)
            {
                _userMetrics.TryRemove(userId, out _);
            }

            _logger.LogDebug("Security metrics analysis completed. Active users: {UserCount}", _userMetrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during security metrics analysis");
        }
    }

    private string ParseAlertType(string? auditData) => "SecurityAlert"; // Simplified
    private string ParseAlertMessage(string? auditData) => auditData ?? "Security alert";
    private SecurityEventSeverity ParseSeverity(string? auditData) => SecurityEventSeverity.Medium; // Simplified

    public override void Dispose()
    {
        _analysisTimer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Tracks security metrics for individual users
/// </summary>
public class UserSecurityMetrics
{
    public string UserId { get; }
    public int FailedLoginAttempts { get; private set; }
    public int AuthorizationFailures { get; private set; }
    public DateTime LastActivity { get; private set; }
    public HashSet<string> UniqueIpAddresses { get; } = new();
    public List<DateTime> AccessTimes { get; } = new();

    public UserSecurityMetrics(string userId)
    {
        UserId = userId;
        LastActivity = DateTime.UtcNow;
    }

    public async Task RecordEventAsync(SecurityEvent securityEvent)
    {
        LastActivity = DateTime.UtcNow;
        
        if (!string.IsNullOrEmpty(securityEvent.IpAddress))
        {
            UniqueIpAddresses.Add(securityEvent.IpAddress);
        }

        AccessTimes.Add(securityEvent.Timestamp);

        switch (securityEvent.EventType)
        {
            case SecurityEventType.AuthenticationFailure:
                FailedLoginAttempts++;
                break;
            case SecurityEventType.AuthorizationFailure:
                AuthorizationFailures++;
                break;
            case SecurityEventType.AuthenticationSuccess:
                // Reset failed attempts on successful login
                FailedLoginAttempts = 0;
                break;
        }

        // Keep only recent access times (last 24 hours)
        var cutoff = DateTime.UtcNow.AddDays(-1);
        AccessTimes.RemoveAll(t => t < cutoff);

        await Task.CompletedTask;
    }

    public bool HasUnusualAccessTimes()
    {
        // Simple heuristic: accessing system outside 6 AM - 10 PM
        return AccessTimes.Any(t => t.Hour < 6 || t.Hour > 22);
    }
}

/// <summary>
/// Configuration options for security monitoring
/// </summary>
public class SecurityMonitoringOptions
{
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxEventsPerBatch { get; set; } = 100;
    public int MaxFailedLogins { get; set; } = 5;
    public int MetricsRetentionDays { get; set; } = 30;
}

// ... (Additional supporting classes for security events, alerts, etc.)

/// <summary>
/// Represents a security event
/// </summary>
public class SecurityEvent
{
    public SecurityEventType EventType { get; set; }
    public string? UserId { get; set; }
    public string IpAddress { get; set; } = "";
    public string Details { get; set; } = "";
    public string? Resource { get; set; }
    public DateTime Timestamp { get; set; }
    public SecurityEventSeverity Severity { get; set; }
}

/// <summary>
/// Security event types
/// </summary>
public enum SecurityEventType
{
    AuthenticationSuccess,
    AuthenticationFailure,
    AuthorizationFailure,
    SuspiciousAccess,
    DataExfiltration,
    PolicyViolation,
    SystemAccess
}

/// <summary>
/// Security event severity levels
/// </summary>
public enum SecurityEventSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Security alert information
/// </summary>
public class SecurityAlert
{
    public Guid Id { get; set; }
    public string AlertType { get; set; } = "";
    public string Message { get; set; } = "";
    public SecurityEventSeverity Severity { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public bool IsResolved { get; set; }
}

/// <summary>
/// User risk profile
/// </summary>
public class UserRiskProfile
{
    public string UserId { get; set; } = "";
    public double RiskScore { get; set; }
    public DateTime LastActivity { get; set; }
    public List<string> ThreatIndicators { get; set; } = new();
}

/// <summary>
/// Security posture analysis report
/// </summary>
public class SecurityPostureReport
{
    public DateTime AnalysisTimestamp { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveThreats { get; set; }
    public double RiskScore { get; set; }
    public List<UserRiskProfile> HighRiskUsers { get; set; } = new();
    public List<string> SystemThreats { get; set; } = new();
}

/// <summary>
/// Interface for security monitoring service
/// </summary>
public interface ISecurityMonitoringService
{
    Task RecordSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task RecordAuthenticationFailureAsync(string? userId, string ipAddress, string reason, CancellationToken cancellationToken = default);
    Task RecordAuthorizationFailureAsync(ClaimsPrincipal user, string resource, string permission, string ipAddress, CancellationToken cancellationToken = default);
    Task RecordSuspiciousAccessAsync(string? userId, string ipAddress, string pattern, SecurityEventSeverity severity = SecurityEventSeverity.High, CancellationToken cancellationToken = default);
    Task<UserSecurityMetrics?> GetUserSecurityMetricsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityAlert>> GetRecentAlertsAsync(int count = 50, CancellationToken cancellationToken = default);
    Task<SecurityPostureReport> AnalyzeSecurityPostureAsync(CancellationToken cancellationToken = default);
}