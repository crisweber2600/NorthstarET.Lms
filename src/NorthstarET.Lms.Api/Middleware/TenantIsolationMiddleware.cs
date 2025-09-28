using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Common;
using System.Security.Claims;

namespace NorthstarET.Lms.Api.Middleware;

/// <summary>
/// Middleware to ensure strict tenant data isolation by setting tenant context
/// and validating all database operations are scoped to the authenticated user's tenant.
/// </summary>
public class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIsolationMiddleware> _logger;

    public TenantIsolationMiddleware(RequestDelegate next, ILogger<TenantIsolationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, ITenantContextAccessor tenantContextAccessor)
    {
        try
        {
            // Skip tenant resolution for health checks and public endpoints
            if (IsPublicEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Extract tenant information from authenticated user claims
            var tenantContext = await ResolveTenantContextAsync(context);
            
            if (tenantContext == null)
            {
                _logger.LogWarning("No tenant context found for authenticated user on path: {Path}", 
                    context.Request.Path);
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Tenant access forbidden");
                return;
            }

            // Set tenant context for this request
            tenantContextAccessor.SetTenant(tenantContext);
            
            _logger.LogDebug("Tenant context set for request: {TenantId} on path: {Path}", 
                tenantContext.TenantId, context.Request.Path);

            // Validate tenant access permissions
            if (!await ValidateTenantAccessAsync(context, tenantContext))
            {
                _logger.LogWarning("Tenant access validation failed for user {UserId} accessing tenant {TenantId}", 
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, tenantContext.TenantId);
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Insufficient tenant permissions");
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant isolation middleware");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error");
        }
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/health",
            "/swagger",
            "/api/v1/auth",
            "/.well-known"
        };

        return publicPaths.Any(publicPath => path.StartsWithSegments(publicPath));
    }

    private async Task<TenantContext?> ResolveTenantContextAsync(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            return null;
        }

        // For PlatformAdmin accessing platform-level endpoints, no tenant context needed
        if (IsPlatformAdminRequest(context))
        {
            return null;
        }

        // Extract tenant from user claims (set during authentication)
        var tenantId = context.User.FindFirst("tenant_id")?.Value;
        var tenantSlug = context.User.FindFirst("tenant_slug")?.Value;

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(tenantSlug))
        {
            _logger.LogWarning("User {UserId} missing tenant claims", 
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return null;
        }

        // Build connection string for tenant schema
        var connectionString = BuildTenantConnectionString(tenantSlug);
        var schemaName = tenantSlug.Replace('-', '_'); // Convert kebab-case to snake_case

        return new TenantContext
        {
            TenantId = tenantId,
            SchemaName = schemaName,
            ConnectionString = connectionString
        };
    }

    private static bool IsPlatformAdminRequest(HttpContext context)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var isPlatformAdmin = roles.Contains("PlatformAdmin");
        var isPlatformEndpoint = context.Request.Path.StartsWithSegments("/api/v1/districts") ||
                                context.Request.Path.StartsWithSegments("/api/v1/platform");

        return isPlatformAdmin && isPlatformEndpoint;
    }

    private async Task<bool> ValidateTenantAccessAsync(HttpContext context, TenantContext tenantContext)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

        // PlatformAdmin has access to all tenants
        if (roles.Contains("PlatformAdmin"))
        {
            return true;
        }

        // Validate that user has appropriate roles for the tenant
        var tenantRoles = context.User.FindAll("tenant_role").Select(c => c.Value);
        var hasValidTenantRole = tenantRoles.Any(role => 
            role == "DistrictAdmin" || 
            role == "SchoolUser" || 
            role == "Staff");

        if (!hasValidTenantRole)
        {
            _logger.LogWarning("User {UserId} has no valid tenant roles for tenant {TenantId}", 
                userId, tenantContext.TenantId);
            return false;
        }

        // Additional validation could include:
        // - Check if tenant is active/suspended
        // - Verify user's role assignments are current
        // - Validate school/class scope permissions
        
        return true;
    }

    private string BuildTenantConnectionString(string tenantSlug)
    {
        // In production, this would come from configuration or key vault
        var baseConnectionString = "Server=localhost;Database=lms;Trusted_Connection=true;TrustServerCertificate=true;";
        
        // For schema-per-tenant, we modify the connection string to use the tenant schema
        return $"{baseConnectionString}SearchPath={tenantSlug.Replace('-', '_')};";
    }
}

/// <summary>
/// Represents the tenant context for the current request
/// </summary>
public class TenantContext
{
    public string TenantId { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// Extension methods for registering tenant isolation middleware
/// </summary>
public static class TenantIsolationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantIsolation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantIsolationMiddleware>();
    }
}