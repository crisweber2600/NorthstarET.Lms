using MediatR;
using NorthstarET.Lms.Application.Abstractions;

namespace NorthstarET.Lms.Application.Behaviors;

/// <summary>
/// Pipeline behavior that ensures tenant context is set before command/query execution
/// </summary>
public class TenantScopingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantScopingBehavior(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Extract tenant slug from request if it exists
        var tenantSlugProperty = request?.GetType().GetProperty("TenantSlug");
        var tenantSlug = tenantSlugProperty?.GetValue(request)?.ToString();

        if (!string.IsNullOrEmpty(tenantSlug))
        {
            _tenantContextAccessor.SetTenant(tenantSlug);
        }

        return await next();
    }
}
