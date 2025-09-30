namespace NorthstarET.Lms.Domain.Common;

/// <summary>
/// Base class for all tenant-scoped entities that enforces multi-tenant isolation
/// All entities in the domain extend this to ensure proper tenant scoping
/// </summary>
public abstract class TenantScopedEntity : Entity
{
    /// <summary>
    /// The tenant slug that owns this entity - immutable after creation
    /// This is managed by the infrastructure layer and never exposed in APIs
    /// </summary>
    public string TenantSlug { get; private set; } = string.Empty;

    /// <summary>
    /// Initialize the tenant scope - can only be called once
    /// </summary>
    /// <param name="tenantSlug">The tenant slug this entity belongs to</param>
    /// <exception cref="InvalidOperationException">Thrown if tenant is already set</exception>
    protected void InitializeTenant(string tenantSlug)
    {
        if (!string.IsNullOrEmpty(TenantSlug))
            throw new InvalidOperationException("Tenant scope cannot be changed after initialization");

        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug cannot be null or empty", nameof(tenantSlug));

        TenantSlug = tenantSlug;
    }

    /// <summary>
    /// Verify that this entity belongs to the specified tenant
    /// </summary>
    /// <param name="tenantSlug">The tenant slug to verify against</param>
    /// <returns>True if the entity belongs to the tenant</returns>
    public bool BelongsToTenant(string tenantSlug)
    {
        return string.Equals(TenantSlug, tenantSlug, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensure that this entity belongs to the specified tenant
    /// </summary>
    /// <param name="tenantSlug">The tenant slug to verify against</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if entity doesn't belong to tenant</exception>
    public void EnsureTenantAccess(string tenantSlug)
    {
        if (!BelongsToTenant(tenantSlug))
            throw new UnauthorizedAccessException($"Entity does not belong to tenant '{tenantSlug}'");
    }
}