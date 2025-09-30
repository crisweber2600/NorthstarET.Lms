using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a role definition with permissions and allowed scopes
/// </summary>
public class RoleDefinition : TenantScopedEntity
{
    /// <summary>
    /// Role name (e.g., "Teacher", "Principal", "DistrictAdmin")
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Role description
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Allowed scope types for this role
    /// </summary>
    private readonly List<RoleScopeType> _allowedScopes = new();
    public IReadOnlyList<RoleScopeType> AllowedScopes => _allowedScopes.AsReadOnly();

    /// <summary>
    /// Permissions granted by this role
    /// </summary>
    private readonly List<Permission> _permissions = new();
    public IReadOnlyList<Permission> Permissions => _permissions.AsReadOnly();

    /// <summary>
    /// Whether this role is active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Version of the permission set for auditing changes
    /// </summary>
    public int Version { get; private set; }

    // EF Core constructor
    protected RoleDefinition()
    {
        Name = string.Empty;
        Description = string.Empty;
        Version = 1;
    }

    /// <summary>
    /// Create a new role definition
    /// </summary>
    public RoleDefinition(
        string tenantSlug,
        string name,
        string description,
        IEnumerable<RoleScopeType> allowedScopes,
        IEnumerable<Permission> permissions,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or empty", nameof(name));

        var scopeList = allowedScopes?.ToList() ?? throw new ArgumentNullException(nameof(allowedScopes));
        var permissionList = permissions?.ToList() ?? throw new ArgumentNullException(nameof(permissions));

        if (scopeList.Count == 0)
            throw new ArgumentException("At least one allowed scope is required", nameof(allowedScopes));

        if (permissionList.Count == 0)
            throw new ArgumentException("At least one permission is required", nameof(permissions));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsActive = true;
        Version = 1;

        _allowedScopes.AddRange(scopeList);
        _permissions.AddRange(permissionList);

        InitializeTenant(tenantSlug);
        SetAuditFields(createdBy);
    }

    /// <summary>
    /// Update role permissions (creates new version for audit trail)
    /// </summary>
    public void UpdatePermissions(IEnumerable<Permission> permissions, string updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive role");

        var permissionList = permissions?.ToList() ?? throw new ArgumentNullException(nameof(permissions));

        if (permissionList.Count == 0)
            throw new ArgumentException("At least one permission is required", nameof(permissions));

        _permissions.Clear();
        _permissions.AddRange(permissionList);
        Version++;
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Update allowed scopes
    /// </summary>
    public void UpdateAllowedScopes(IEnumerable<RoleScopeType> allowedScopes, string updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive role");

        var scopeList = allowedScopes?.ToList() ?? throw new ArgumentNullException(nameof(allowedScopes));

        if (scopeList.Count == 0)
            throw new ArgumentException("At least one allowed scope is required", nameof(allowedScopes));

        _allowedScopes.Clear();
        _allowedScopes.AddRange(scopeList);
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Deactivate the role
    /// </summary>
    public void Deactivate(string updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Role is already inactive");

        IsActive = false;
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Reactivate the role
    /// </summary>
    public void Reactivate(string updatedBy)
    {
        if (IsActive)
            throw new InvalidOperationException("Role is already active");

        IsActive = true;
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Check if this role can be assigned to a specific scope
    /// </summary>
    public bool CanBeAssignedToScope(RoleScopeType scopeType)
    {
        return IsActive && _allowedScopes.Contains(scopeType);
    }

    /// <summary>
    /// Check if this role has a specific permission
    /// </summary>
    public bool HasPermission(string permissionCode)
    {
        return IsActive && _permissions.Any(p => p.Code == permissionCode.ToUpperInvariant());
    }

    /// <summary>
    /// Get permissions by category
    /// </summary>
    public IEnumerable<Permission> GetPermissionsByCategory(string category)
    {
        return _permissions.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
    }
}