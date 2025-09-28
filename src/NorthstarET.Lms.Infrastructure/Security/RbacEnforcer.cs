using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace NorthstarET.Lms.Infrastructure.Security;

/// <summary>
/// Service that enforces Role-Based Access Control (RBAC) with hierarchical permissions
/// Implements deny-by-default with explicit permission granting per FR-052, FR-053
/// </summary>
public class RbacEnforcer : IRbacEnforcer
{
    private readonly ITenantContextAccessor _tenantContext;
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IRoleDefinitionRepository _roleDefinitionRepository;
    private readonly ILogger<RbacEnforcer> _logger;

    // Hierarchical role levels (higher numbers include lower-level permissions)
    private readonly Dictionary<string, int> _roleHierarchy = new()
    {
        ["PlatformAdmin"] = 100,
        ["DistrictAdmin"] = 80,
        ["SchoolAdmin"] = 60,
        ["Teacher"] = 40,
        ["SchoolUser"] = 20,
        ["Student"] = 10,
        ["Guardian"] = 5
    };

    public RbacEnforcer(
        ITenantContextAccessor tenantContext,
        IRoleAssignmentRepository roleAssignmentRepository,
        IRoleDefinitionRepository roleDefinitionRepository,
        ILogger<RbacEnforcer> logger)
    {
        _tenantContext = tenantContext;
        _roleAssignmentRepository = roleAssignmentRepository;
        _roleDefinitionRepository = roleDefinitionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the current user has the specified permission
    /// </summary>
    public async Task<AuthorizationResult> HasPermissionAsync(
        ClaimsPrincipal user,
        string permission,
        string? resource = null,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return AuthorizationResult.Forbidden("User not authenticated");
        }

        var userId = GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return AuthorizationResult.Forbidden("User ID not found");
        }

        var tenantId = _tenantContext.GetCurrentTenantId();
        
        try
        {
            // Get user's role assignments
            var roleAssignments = await _roleAssignmentRepository.GetUserRoleAssignmentsAsync(
                userId, cancellationToken);

            if (!roleAssignments.Any())
            {
                _logger.LogWarning("No role assignments found for user {UserId}", userId);
                return AuthorizationResult.Forbidden("No role assignments found");
            }

            // Check each role assignment for the required permission
            foreach (var assignment in roleAssignments)
            {
                var hasPermission = await CheckRolePermissionAsync(
                    assignment, permission, resource, resourceId, tenantId, cancellationToken);

                if (hasPermission.IsAuthorized)
                {
                    _logger.LogDebug("Permission {Permission} granted to user {UserId} via role {RoleId}",
                        permission, userId, assignment.RoleDefinitionId);
                    return hasPermission;
                }
            }

            _logger.LogWarning("Permission {Permission} denied for user {UserId} on resource {Resource}",
                permission, userId, resource ?? "system");
                
            return AuthorizationResult.Forbidden($"Permission '{permission}' denied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
            return AuthorizationResult.Error("Permission check failed");
        }
    }

    /// <summary>
    /// Checks if the current user has any of the specified permissions (OR logic)
    /// </summary>
    public async Task<AuthorizationResult> HasAnyPermissionAsync(
        ClaimsPrincipal user,
        string[] permissions,
        string? resource = null,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var permission in permissions)
        {
            var result = await HasPermissionAsync(user, permission, resource, resourceId, cancellationToken);
            if (result.IsAuthorized)
            {
                return result;
            }
        }

        return AuthorizationResult.Forbidden($"None of the required permissions granted: {string.Join(", ", permissions)}");
    }

    /// <summary>
    /// Checks if the current user has all specified permissions (AND logic)
    /// </summary>
    public async Task<AuthorizationResult> HasAllPermissionsAsync(
        ClaimsPrincipal user,
        string[] permissions,
        string? resource = null,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        var deniedPermissions = new List<string>();

        foreach (var permission in permissions)
        {
            var result = await HasPermissionAsync(user, permission, resource, resourceId, cancellationToken);
            if (!result.IsAuthorized)
            {
                deniedPermissions.Add(permission);
            }
        }

        if (deniedPermissions.Any())
        {
            return AuthorizationResult.Forbidden($"Missing required permissions: {string.Join(", ", deniedPermissions)}");
        }

        return AuthorizationResult.Allowed();
    }

    /// <summary>
    /// Gets all effective permissions for the current user
    /// </summary>
    public async Task<IEnumerable<EffectivePermission>> GetUserPermissionsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Enumerable.Empty<EffectivePermission>();
        }

        var userId = GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return Enumerable.Empty<EffectivePermission>();
        }

        try
        {
            var roleAssignments = await _roleAssignmentRepository.GetUserRoleAssignmentsAsync(
                userId, cancellationToken);

            var effectivePermissions = new List<EffectivePermission>();

            foreach (var assignment in roleAssignments)
            {
                var roleDefinition = await _roleDefinitionRepository.GetByIdAsync(
                    assignment.RoleDefinitionId, cancellationToken);

                if (roleDefinition != null)
                {
                    var permissions = ParsePermissions(roleDefinition.Permissions);
                    
                    foreach (var permission in permissions)
                    {
                        effectivePermissions.Add(new EffectivePermission
                        {
                            Permission = permission,
                            RoleName = roleDefinition.Name,
                            Scope = DetermineScope(assignment),
                            GrantedAt = assignment.CreatedAt
                        });
                    }
                }
            }

            // Remove duplicates and apply hierarchy
            return effectivePermissions
                .GroupBy(p => p.Permission)
                .Select(g => g.OrderByDescending(p => GetRoleLevel(p.RoleName)).First())
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions for {UserId}", userId);
            return Enumerable.Empty<EffectivePermission>();
        }
    }

    /// <summary>
    /// Validates a role assignment against business rules
    /// </summary>
    public async Task<AuthorizationResult> ValidateRoleAssignmentAsync(
        RoleAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if role definition exists
            var roleDefinition = await _roleDefinitionRepository.GetByIdAsync(
                assignment.RoleDefinitionId, cancellationToken);

            if (roleDefinition == null)
            {
                return AuthorizationResult.Forbidden("Role definition not found");
            }

            // Validate scope restrictions
            var scopeValidation = ValidateRoleScope(roleDefinition, assignment);
            if (!scopeValidation.IsAuthorized)
            {
                return scopeValidation;
            }

            // Check for conflicting assignments
            var existingAssignments = await _roleAssignmentRepository.GetUserRoleAssignmentsAsync(
                assignment.UserId, cancellationToken);

            var conflicts = DetectRoleConflicts(roleDefinition, existingAssignments);
            if (conflicts.Any())
            {
                return AuthorizationResult.Forbidden($"Role conflicts detected: {string.Join(", ", conflicts)}");
            }

            return AuthorizationResult.Allowed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating role assignment for user {UserId}", assignment.UserId);
            return AuthorizationResult.Error("Role assignment validation failed");
        }
    }

    private async Task<AuthorizationResult> CheckRolePermissionAsync(
        RoleAssignment assignment,
        string permission,
        string? resource,
        Guid? resourceId,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        // Get role definition
        var roleDefinition = await _roleDefinitionRepository.GetByIdAsync(
            assignment.RoleDefinitionId, cancellationToken);

        if (roleDefinition == null)
        {
            return AuthorizationResult.Forbidden("Role definition not found");
        }

        // Check if role has the permission
        var rolePermissions = ParsePermissions(roleDefinition.Permissions);
        if (!rolePermissions.Contains(permission))
        {
            return AuthorizationResult.Forbidden($"Role {roleDefinition.Name} lacks permission {permission}");
        }

        // Check scope constraints
        if (!ValidateScope(assignment, resource, resourceId))
        {
            return AuthorizationResult.Forbidden("Resource outside role scope");
        }

        // Check temporal constraints (if role assignment has expiry)
        if (assignment.ExpiresAt.HasValue && assignment.ExpiresAt.Value < DateTime.UtcNow)
        {
            return AuthorizationResult.Forbidden("Role assignment expired");
        }

        // Check if assignment is active
        if (assignment.Status != RoleAssignmentStatus.Active)
        {
            return AuthorizationResult.Forbidden($"Role assignment is {assignment.Status}");
        }

        return AuthorizationResult.Allowed();
    }

    private bool ValidateScope(RoleAssignment assignment, string? resource, Guid? resourceId)
    {
        // If no resource specified, check general access
        if (string.IsNullOrEmpty(resource))
        {
            return true;
        }

        // Check School-scoped access
        if (assignment.SchoolId.HasValue && resource?.ToLower().Contains("school") == true)
        {
            return resourceId == null || resourceId == assignment.SchoolId;
        }

        // Check Class-scoped access
        if (assignment.ClassId.HasValue && resource?.ToLower().Contains("class") == true)
        {
            return resourceId == null || resourceId == assignment.ClassId;
        }

        // Check SchoolYear-scoped access
        if (assignment.SchoolYearId.HasValue && resource?.ToLower().Contains("schoolyear") == true)
        {
            return resourceId == null || resourceId == assignment.SchoolYearId;
        }

        return true; // Allow access if no specific scope restrictions
    }

    private AuthorizationResult ValidateRoleScope(RoleDefinition roleDefinition, RoleAssignment assignment)
    {
        // Validate that role assignment scope matches role definition constraints
        var requiredScope = roleDefinition.RequiredScope;

        if (requiredScope.HasFlag(RoleScope.School) && !assignment.SchoolId.HasValue)
        {
            return AuthorizationResult.Forbidden("Role requires school scope");
        }

        if (requiredScope.HasFlag(RoleScope.Class) && !assignment.ClassId.HasValue)
        {
            return AuthorizationResult.Forbidden("Role requires class scope");
        }

        if (requiredScope.HasFlag(RoleScope.SchoolYear) && !assignment.SchoolYearId.HasValue)
        {
            return AuthorizationResult.Forbidden("Role requires school year scope");
        }

        return AuthorizationResult.Allowed();
    }

    private List<string> DetectRoleConflicts(RoleDefinition newRole, IEnumerable<RoleAssignment> existingAssignments)
    {
        var conflicts = new List<string>();
        
        // Check for mutually exclusive roles
        var conflictingRoles = new Dictionary<string, string[]>
        {
            ["Student"] = new[] { "Teacher", "SchoolAdmin", "DistrictAdmin" },
            ["Guardian"] = new[] { "Teacher", "SchoolAdmin", "DistrictAdmin" }
        };

        if (conflictingRoles.ContainsKey(newRole.Name))
        {
            var incompatibleRoles = conflictingRoles[newRole.Name];
            // This would require additional logic to check existing role names
        }

        return conflicts;
    }

    private string[] ParsePermissions(string permissionsJson)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(permissionsJson) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private string GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               user.FindFirst("sub")?.Value ?? 
               user.FindFirst("user_id")?.Value ?? "";
    }

    private int GetRoleLevel(string roleName)
    {
        return _roleHierarchy.GetValueOrDefault(roleName, 0);
    }

    private string DetermineScope(RoleAssignment assignment)
    {
        if (assignment.ClassId.HasValue)
            return $"Class:{assignment.ClassId}";
        if (assignment.SchoolId.HasValue)
            return $"School:{assignment.SchoolId}";
        if (assignment.SchoolYearId.HasValue)
            return $"SchoolYear:{assignment.SchoolYearId}";
        
        return "District";
    }
}

/// <summary>
/// Interface for RBAC enforcement service
/// </summary>
public interface IRbacEnforcer
{
    Task<AuthorizationResult> HasPermissionAsync(ClaimsPrincipal user, string permission, string? resource = null, Guid? resourceId = null, CancellationToken cancellationToken = default);
    Task<AuthorizationResult> HasAnyPermissionAsync(ClaimsPrincipal user, string[] permissions, string? resource = null, Guid? resourceId = null, CancellationToken cancellationToken = default);
    Task<AuthorizationResult> HasAllPermissionsAsync(ClaimsPrincipal user, string[] permissions, string? resource = null, Guid? resourceId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<EffectivePermission>> GetUserPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<AuthorizationResult> ValidateRoleAssignmentAsync(RoleAssignment assignment, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of authorization check
/// </summary>
public class AuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public string? Reason { get; set; }
    public AuthorizationStatus Status { get; set; }

    public static AuthorizationResult Allowed() => new() { IsAuthorized = true, Status = AuthorizationStatus.Allowed };
    public static AuthorizationResult Forbidden(string reason) => new() { IsAuthorized = false, Reason = reason, Status = AuthorizationStatus.Forbidden };
    public static AuthorizationResult Error(string reason) => new() { IsAuthorized = false, Reason = reason, Status = AuthorizationStatus.Error };
}

/// <summary>
/// Effective permission with context
/// </summary>
public class EffectivePermission
{
    public string Permission { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string Scope { get; set; } = "";
    public DateTime GrantedAt { get; set; }
}

/// <summary>
/// Authorization status types
/// </summary>
public enum AuthorizationStatus
{
    Allowed,
    Forbidden,
    Error
}

/// <summary>
/// Role scope flags
/// </summary>
[Flags]
public enum RoleScope
{
    None = 0,
    District = 1,
    School = 2,
    Class = 4,
    SchoolYear = 8
}