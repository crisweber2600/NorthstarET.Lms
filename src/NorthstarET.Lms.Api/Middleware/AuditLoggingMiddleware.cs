using NorthstarET.Lms.Application.Services;
using NorthstarET.Lms.Domain.Events;
using System.Security.Claims;
using System.Text.Json;

namespace NorthstarET.Lms.Api.Middleware;

/// <summary>
/// Middleware to automatically capture and log all HTTP requests and responses 
/// for compliance and security monitoring purposes.
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly AuditService _auditService;

    public AuditLoggingMiddleware(
        RequestDelegate next, 
        ILogger<AuditLoggingMiddleware> logger,
        AuditService auditService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip audit logging for health checks and non-business endpoints
        if (ShouldSkipAudit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var startTime = DateTimeOffset.UtcNow;
        var correlationId = GenerateCorrelationId(context);
        
        // Capture request details
        var requestDetails = await CaptureRequestDetailsAsync(context.Request);
        
        // Enable response buffering to capture response body
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
            
            // Capture response details
            var responseDetails = await CaptureResponseDetailsAsync(context.Response, responseBodyStream);
            var duration = DateTimeOffset.UtcNow - startTime;

            // Log the audit event asynchronously
            _ = Task.Run(async () => await LogAuditEventAsync(
                context, 
                requestDetails, 
                responseDetails, 
                correlationId, 
                duration));

            // Copy response back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
        catch (Exception ex)
        {
            // Log the error and still capture audit information
            var errorDetails = new
            {
                Error = ex.Message,
                StackTrace = ex.StackTrace
            };

            var duration = DateTimeOffset.UtcNow - startTime;
            
            _ = Task.Run(async () => await LogAuditEventAsync(
                context, 
                requestDetails, 
                errorDetails, 
                correlationId, 
                duration,
                isError: true));

            context.Response.Body = originalResponseBodyStream;
            throw;
        }
        finally
        {
            context.Response.Body = originalResponseBodyStream;
        }
    }

    private static bool ShouldSkipAudit(PathString path)
    {
        var skipPaths = new[]
        {
            "/health",
            "/metrics",
            "/swagger",
            "/favicon.ico",
            "/_framework", // SignalR/Blazor
            "/api/v1/audit" // Avoid recursive audit logging
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
    }

    private static string GenerateCorrelationId(HttpContext context)
    {
        // Use existing correlation ID if present, otherwise generate new one
        return context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? 
               Guid.NewGuid().ToString();
    }

    private async Task<object> CaptureRequestDetailsAsync(HttpRequest request)
    {
        var requestBody = string.Empty;
        
        // Capture request body for POST/PUT/PATCH requests (with size limits)
        if (request.ContentLength.HasValue && 
            request.ContentLength > 0 && 
            request.ContentLength < 1024 * 1024 && // Max 1MB
            (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH"))
        {
            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);
            
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);
            
            // Sanitize sensitive data
            requestBody = SanitizeSensitiveData(requestBody);
        }

        return new
        {
            Method = request.Method,
            Path = request.Path.Value,
            QueryString = request.QueryString.Value,
            Headers = SanitizeHeaders(request.Headers),
            Body = requestBody,
            ContentType = request.ContentType,
            ContentLength = request.ContentLength,
            UserAgent = request.Headers["User-Agent"].FirstOrDefault(),
            RemoteIpAddress = request.HttpContext.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private async Task<object> CaptureResponseDetailsAsync(HttpResponse response, MemoryStream responseStream)
    {
        var responseBody = string.Empty;
        
        // Capture response body for non-file downloads (with size limits)
        if (responseStream.Length > 0 && 
            responseStream.Length < 1024 * 1024 && // Max 1MB
            !IsFileDownload(response))
        {
            responseStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseStream, leaveOpen: true);
            responseBody = await reader.ReadToEndAsync();
            
            // Sanitize sensitive data
            responseBody = SanitizeSensitiveData(responseBody);
        }

        return new
        {
            StatusCode = response.StatusCode,
            Headers = SanitizeHeaders(response.Headers.ToDictionary(h => h.Key, h => h.Value.AsEnumerable())),
            Body = responseBody,
            ContentType = response.ContentType,
            ContentLength = responseStream.Length,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private async Task LogAuditEventAsync(
        HttpContext context, 
        object requestDetails, 
        object responseDetails, 
        string correlationId, 
        TimeSpan duration,
        bool isError = false)
    {
        try
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var tenantId = context.User?.FindFirst("tenant_id")?.Value;
            var roles = context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

            var auditEvent = new ApiAccessEvent
            {
                UserId = userId,
                TenantId = tenantId,
                Roles = roles,
                RequestDetails = JsonSerializer.Serialize(requestDetails),
                ResponseDetails = JsonSerializer.Serialize(responseDetails),
                CorrelationId = correlationId,
                Duration = duration,
                IsError = isError,
                Timestamp = DateTimeOffset.UtcNow
            };

            await _auditService.LogApiAccessAsync(auditEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event for correlation ID: {CorrelationId}", correlationId);
            // Don't throw - audit logging failures shouldn't break the request
        }
    }

    private static string SanitizeSensitiveData(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Remove or mask sensitive fields
        var sensitivePatterns = new[]
        {
            (@"""password"":\s*""[^""]*""", @"""password"":""***REDACTED***"""),
            (@"""token"":\s*""[^""]*""", @"""token"":""***REDACTED***"""),
            (@"""secret"":\s*""[^""]*""", @"""secret"":""***REDACTED***"""),
            (@"""ssn"":\s*""[^""]*""", @"""ssn"":""***REDACTED***"""),
            (@"""creditCard"":\s*""[^""]*""", @"""creditCard"":""***REDACTED***"""),
            // Add more patterns as needed for FERPA compliance
        };

        foreach (var (pattern, replacement) in sensitivePatterns)
        {
            content = System.Text.RegularExpressions.Regex.Replace(
                content, pattern, replacement, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return content;
    }

    private static Dictionary<string, object> SanitizeHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        var sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Cookie",
            "X-API-Key",
            "X-Auth-Token"
        };

        return headers.ToDictionary(
            h => h.Key,
            h => sensitiveHeaders.Contains(h.Key) 
                ? (object)"***REDACTED***" 
                : string.Join(", ", h.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsFileDownload(HttpResponse response)
    {
        var contentType = response.ContentType?.ToLower();
        return contentType != null && (
            contentType.StartsWith("application/pdf") ||
            contentType.StartsWith("application/octet-stream") ||
            contentType.StartsWith("image/") ||
            response.Headers.ContainsKey("Content-Disposition"));
    }
}

/// <summary>
/// Represents an API access audit event
/// </summary>
public class ApiAccessEvent
{
    public string UserId { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string RequestDetails { get; set; } = string.Empty;
    public string ResponseDetails { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool IsError { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Extension methods for registering audit logging middleware
/// </summary>
public static class AuditLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
}