namespace NorthstarET.Lms.Application.Interfaces;

/// <summary>
/// Represents tenant context for multi-tenant data isolation
/// </summary>
public interface ITenantContext
{
    string TenantId { get; }
    string SchemaName { get; }
    string ConnectionString { get; }
    string DisplayName { get; }
}

/// <summary>
/// Provides access to the current tenant context in multi-tenant scenarios
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>
    /// Gets the current tenant context, or null if no tenant is set
    /// </summary>
    ITenantContext? GetTenant();

    /// <summary>
    /// Sets the current tenant context
    /// </summary>
    void SetTenant(ITenantContext? tenant);
}