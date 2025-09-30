using Microsoft.AspNetCore.Http;
using NorthstarET.Lms.Domain.Shared;

namespace NorthstarET.Lms.Infrastructure.Persistence;

public class TenantContextAccessor : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _tenantSlug;

    public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantSlug
    {
        get
        {
            if (!string.IsNullOrEmpty(_tenantSlug))
            {
                return _tenantSlug;
            }

            // Try to get tenant slug from HTTP context
            if (_httpContextAccessor.HttpContext != null)
            {
                // Check request headers first (X-Tenant-Slug)
                if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Tenant-Slug", out var headerValue))
                {
                    _tenantSlug = headerValue.ToString();
                    return _tenantSlug ?? string.Empty;
                }

                // Check route values would require additional dependencies
                // In production, use path parsing or dedicated routing middleware

                // Check claims (for authenticated users)
                var tenantClaim = _httpContextAccessor.HttpContext.User?.FindFirst("tenant_slug");
                if (tenantClaim != null)
                {
                    _tenantSlug = tenantClaim.Value;
                    return _tenantSlug;
                }
            }

            return string.Empty;
        }
    }

    public void SetTenantSlug(string tenantSlug)
    {
        _tenantSlug = tenantSlug;
    }
}
