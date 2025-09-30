using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Raised when a role is assigned to a user
/// </summary>
public sealed class RoleAssignedEvent : DomainEvent
{
    public Guid RoleAssignmentId { get; }
    public Guid UserId { get; }
    public Guid RoleDefinitionId { get; }
    public RoleScope Scope { get; }

    public RoleAssignedEvent(Guid roleAssignmentId, Guid userId, Guid roleDefinitionId, RoleScope scope)
    {
        RoleAssignmentId = roleAssignmentId;
        UserId = userId;
        RoleDefinitionId = roleDefinitionId;
        Scope = scope;
    }
}

/// <summary>
/// Raised when a role assignment is revoked
/// </summary>
public sealed class RoleRevokedEvent : DomainEvent
{
    public Guid RoleAssignmentId { get; }
    public Guid UserId { get; }
    public string Reason { get; }

    public RoleRevokedEvent(Guid roleAssignmentId, Guid userId, string reason)
    {
        RoleAssignmentId = roleAssignmentId;
        UserId = userId;
        Reason = reason;
    }
}

/// <summary>
/// Raised when a role is delegated
/// </summary>
public sealed class RoleDelegatedEvent : DomainEvent
{
    public Guid RoleAssignmentId { get; }
    public Guid DelegatedToUserId { get; }
    public Guid DelegatedByUserId { get; }
    public DateTime ExpiresAt { get; }

    public RoleDelegatedEvent(Guid roleAssignmentId, Guid delegatedToUserId, Guid delegatedByUserId, DateTime expiresAt)
    {
        RoleAssignmentId = roleAssignmentId;
        DelegatedToUserId = delegatedToUserId;
        DelegatedByUserId = delegatedByUserId;
        ExpiresAt = expiresAt;
    }
}

/// <summary>
/// Raised when a role assignment expires
/// </summary>
public sealed class RoleExpiredEvent : DomainEvent
{
    public Guid RoleAssignmentId { get; }
    public Guid UserId { get; }
    public DateTime ExpiredAt { get; }

    public RoleExpiredEvent(Guid roleAssignmentId, Guid userId, DateTime expiredAt)
    {
        RoleAssignmentId = roleAssignmentId;
        UserId = userId;
        ExpiredAt = expiredAt;
    }
}