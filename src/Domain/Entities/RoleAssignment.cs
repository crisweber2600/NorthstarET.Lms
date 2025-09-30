using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a role assignment to a user with hierarchical scope
/// </summary>
public class RoleAssignment : TenantScopedEntity
{
    /// <summary>
    /// Role definition being assigned
    /// </summary>
    public Guid RoleDefinitionId { get; private set; }

    /// <summary>
    /// User receiving the role assignment
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Scope context for the role assignment
    /// </summary>
    public RoleScope Scope { get; private set; }

    /// <summary>
    /// User who delegated this role (if applicable)
    /// </summary>
    public Guid? DelegatedBy { get; private set; }

    /// <summary>
    /// When the delegation expires (if applicable)
    /// </summary>
    public DateTime? DelegationExpiresAt { get; private set; }

    /// <summary>
    /// Current status of the role assignment
    /// </summary>
    public RoleAssignmentStatus Status { get; private set; }

    /// <summary>
    /// When the assignment was revoked (if applicable)
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Reason for revocation (if applicable)
    /// </summary>
    public string? RevocationReason { get; private set; }

    // EF Core constructor
    protected RoleAssignment()
    {
        Scope = RoleScope.District(Guid.Empty);
    }

    /// <summary>
    /// Create a new role assignment
    /// </summary>
    public RoleAssignment(
        string tenantSlug,
        Guid roleDefinitionId,
        Guid userId,
        RoleScope scope,
        string createdBy)
    {
        if (roleDefinitionId == Guid.Empty)
            throw new ArgumentException("Role definition ID cannot be empty", nameof(roleDefinitionId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        RoleDefinitionId = roleDefinitionId;
        UserId = userId;
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Status = RoleAssignmentStatus.Active;

        InitializeTenant(tenantSlug);
        SetAuditFields(createdBy);

        AddDomainEvent(new RoleAssignedEvent(Id, UserId, RoleDefinitionId, Scope)
        {
            TenantSlug = tenantSlug,
            TriggeredBy = createdBy
        });
    }

    /// <summary>
    /// Create a delegated role assignment
    /// </summary>
    public RoleAssignment(
        string tenantSlug,
        Guid roleDefinitionId,
        Guid userId,
        RoleScope scope,
        Guid delegatedBy,
        DateTime expiresAt,
        string createdBy) : this(tenantSlug, roleDefinitionId, userId, scope, createdBy)
    {
        if (delegatedBy == Guid.Empty)
            throw new ArgumentException("Delegated by user ID cannot be empty", nameof(delegatedBy));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        DelegatedBy = delegatedBy;
        DelegationExpiresAt = expiresAt;

        // Override the standard assignment event with delegation event
        ClearDomainEvents();
        AddDomainEvent(new RoleDelegatedEvent(Id, userId, delegatedBy, expiresAt)
        {
            TenantSlug = tenantSlug,
            TriggeredBy = createdBy
        });
    }

    /// <summary>
    /// Revoke the role assignment
    /// </summary>
    public void Revoke(string reason, string updatedBy)
    {
        if (Status != RoleAssignmentStatus.Active)
            throw new InvalidOperationException("Only active role assignments can be revoked");

        Status = RoleAssignmentStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason?.Trim();
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new RoleRevokedEvent(Id, UserId, reason ?? "No reason provided")
        {
            TenantSlug = TenantSlug,
            TriggeredBy = updatedBy
        });
    }

    /// <summary>
    /// Mark the role assignment as expired (called by background job)
    /// </summary>
    public void MarkAsExpired()
    {
        if (Status != RoleAssignmentStatus.Active)
            throw new InvalidOperationException("Only active role assignments can expire");

        if (!DelegationExpiresAt.HasValue || DelegationExpiresAt.Value > DateTime.UtcNow)
            throw new InvalidOperationException("Role assignment is not eligible for expiration");

        Status = RoleAssignmentStatus.Expired;
        UpdateAuditFields("System");

        AddDomainEvent(new RoleExpiredEvent(Id, UserId, DateTime.UtcNow)
        {
            TenantSlug = TenantSlug,
            TriggeredBy = "System"
        });
    }

    /// <summary>
    /// Check if the role assignment is currently effective
    /// </summary>
    public bool IsEffective()
    {
        if (Status != RoleAssignmentStatus.Active)
            return false;

        // Check if delegation has expired
        if (DelegationExpiresAt.HasValue && DelegationExpiresAt.Value <= DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Check if this is a delegated assignment
    /// </summary>
    public bool IsDelegated => DelegatedBy.HasValue;

    /// <summary>
    /// Check if the assignment is eligible for automatic expiration
    /// </summary>
    public bool IsEligibleForExpiration()
    {
        return Status == RoleAssignmentStatus.Active &&
               DelegationExpiresAt.HasValue &&
               DelegationExpiresAt.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// Check if this role assignment encompasses another scope (for permission inheritance)
    /// </summary>
    public bool EncompassesScope(RoleScope otherScope)
    {
        if (!IsEffective())
            return false;

        return Scope.Encompasses(otherScope);
    }

    /// <summary>
    /// Extend the delegation expiration date
    /// </summary>
    public void ExtendDelegation(DateTime newExpirationDate, string updatedBy)
    {
        if (!IsDelegated)
            throw new InvalidOperationException("Cannot extend expiration for non-delegated assignments");

        if (Status != RoleAssignmentStatus.Active)
            throw new InvalidOperationException("Cannot extend expired or revoked assignments");

        if (newExpirationDate <= DateTime.UtcNow)
            throw new ArgumentException("New expiration date must be in the future", nameof(newExpirationDate));

        DelegationExpiresAt = newExpirationDate;
        UpdateAuditFields(updatedBy);
    }
}

/// <summary>
/// Role assignment status enumeration
/// </summary>
public enum RoleAssignmentStatus
{
    Active,
    Revoked,
    Expired
}