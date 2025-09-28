using Microsoft.AspNetCore.Http;
using NorthstarET.Lms.Application.Interfaces;

namespace NorthstarET.Lms.Infrastructure.Security;

/// <summary>
/// Represents tenant context information for multi-tenant data isolation
/// </summary>
public class TenantContext : ITenantContext
{
    public string TenantId { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Provides access to the current tenant context in multi-tenant scenarios
/// </summary>
public class TenantContextAccessor : ITenantContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    private static readonly AsyncLocal<ITenantContext?> _tenantContext = new();

    public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ITenantContext? GetTenant()
    {
        // Try to get from HTTP context first (for API requests)
        if (_httpContextAccessor.HttpContext != null)
        {
            if (_httpContextAccessor.HttpContext.Items.TryGetValue("TenantContext", out var contextTenant))
            {
                return contextTenant as ITenantContext;
            }
        }

        // Fall back to AsyncLocal for background services or non-HTTP contexts
        return _tenantContext.Value;
    }

    public void SetTenant(ITenantContext? tenant)
    {
        // Set in HTTP context if available
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items["TenantContext"] = tenant;
        }

        // Always set in AsyncLocal for consistency
        _tenantContext.Value = tenant;
    }

    public string? GetCurrentTenantId()
    {
        return GetTenant()?.TenantId;
    }
}