using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Domain.Events;

// Role Definition Events
public record RoleDefinitionCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RoleDefinitionId { get; }
    public string RoleName { get; }
    public RoleScope Scope { get; }
    public bool IsSystemRole { get; }

    public RoleDefinitionCreatedEvent(Guid roleDefinitionId, string roleName, RoleScope scope, bool isSystemRole)
    {
        RoleDefinitionId = roleDefinitionId;
        RoleName = roleName;
        Scope = scope;
        IsSystemRole = isSystemRole;
    }
}

public record RoleDefinitionUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RoleDefinitionId { get; }
    public string RoleName { get; }
    public string OldDescription { get; }
    public string NewDescription { get; }
    public string UpdatedByUserId { get; }

    public RoleDefinitionUpdatedEvent(Guid roleDefinitionId, string roleName, string oldDescription, string newDescription, string updatedByUserId)
    {
        RoleDefinitionId = roleDefinitionId;
        RoleName = roleName;
        OldDescription = oldDescription;
        NewDescription = newDescription;
        UpdatedByUserId = updatedByUserId;
    }
}

public record RolePermissionAddedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RoleDefinitionId { get; }
    public string RoleName { get; }
    public string Permission { get; }
    public string AddedByUserId { get; }

    public RolePermissionAddedEvent(Guid roleDefinitionId, string roleName, string permission, string addedByUserId)
    {
        RoleDefinitionId = roleDefinitionId;
        RoleName = roleName;
        Permission = permission;
        AddedByUserId = addedByUserId;
    }
}

public record RolePermissionRemovedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RoleDefinitionId { get; }
    public string RoleName { get; }
    public string Permission { get; }
    public string RemovedByUserId { get; }

    public RolePermissionRemovedEvent(Guid roleDefinitionId, string roleName, string permission, string removedByUserId)
    {
        RoleDefinitionId = roleDefinitionId;
        RoleName = roleName;
        Permission = permission;
        RemovedByUserId = removedByUserId;
    }
}

public record RoleDelegationEnabledEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RoleDefinitionId { get; }
    public string RoleName { get; }
    public string EnabledByUserId { get; }

    public RoleDelegationEnabledEvent(Guid roleDefinitionId, string roleName, string enabledByUserId)
    {
        RoleDefinitionId = roleDefinitionId;
        RoleName = roleName;
        EnabledByUserId = enabledByUserId;
    }
}

public record RoleDelegationDisabledEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RoleDefinitionId { get; }
    public string RoleName { get; }
    public string DisabledByUserId { get; }

    public RoleDelegationDisabledEvent(Guid roleDefinitionId, string roleName, string disabledByUserId)
    {
        RoleDefinitionId = roleDefinitionId;
        RoleName = roleName;
        DisabledByUserId = disabledByUserId;
    }
}

// Retention Policy Events
public record RetentionPolicyCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RetentionPolicyId { get; }
    public string EntityType { get; }
    public int RetentionYears { get; }
    public bool IsDefault { get; }

    public RetentionPolicyCreatedEvent(Guid retentionPolicyId, string entityType, int retentionYears, bool isDefault)
    {
        RetentionPolicyId = retentionPolicyId;
        EntityType = entityType;
        RetentionYears = retentionYears;
        IsDefault = isDefault;
    }
}

public record RetentionPolicyUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RetentionPolicyId { get; }
    public string EntityType { get; }
    public int OldRetentionYears { get; }
    public int NewRetentionYears { get; }
    public string UpdatedByUserId { get; }

    public RetentionPolicyUpdatedEvent(Guid retentionPolicyId, string entityType, int oldRetentionYears, int newRetentionYears, string updatedByUserId)
    {
        RetentionPolicyId = retentionPolicyId;
        EntityType = entityType;
        OldRetentionYears = oldRetentionYears;
        NewRetentionYears = newRetentionYears;
        UpdatedByUserId = updatedByUserId;
    }
}

public record RetentionPolicySupersededEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RetentionPolicyId { get; }
    public string EntityType { get; }
    public DateTime SupersededDate { get; }
    public string SupersededByUserId { get; }

    public RetentionPolicySupersededEvent(Guid retentionPolicyId, string entityType, DateTime supersededDate, string supersededByUserId)
    {
        RetentionPolicyId = retentionPolicyId;
        EntityType = entityType;
        SupersededDate = supersededDate;
        SupersededByUserId = supersededByUserId;
    }
}

// Legal Hold Events
public record LegalHoldCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid LegalHoldId { get; }
    public string EntityType { get; }
    public Guid EntityId { get; }
    public string Reason { get; }
    public string AuthorizingUser { get; }

    public LegalHoldCreatedEvent(Guid legalHoldId, string entityType, Guid entityId, string reason, string authorizingUser)
    {
        LegalHoldId = legalHoldId;
        EntityType = entityType;
        EntityId = entityId;
        Reason = reason;
        AuthorizingUser = authorizingUser;
    }
}

public record LegalHoldReleasedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid LegalHoldId { get; }
    public string EntityType { get; }
    public Guid EntityId { get; }
    public DateTime ReleaseDate { get; }
    public string ReleasedByUser { get; }

    public LegalHoldReleasedEvent(Guid legalHoldId, string entityType, Guid entityId, DateTime releaseDate, string releasedByUser)
    {
        LegalHoldId = legalHoldId;
        EntityType = entityType;
        EntityId = entityId;
        ReleaseDate = releaseDate;
        ReleasedByUser = releasedByUser;
    }
}

public record LegalHoldUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid LegalHoldId { get; }
    public string EntityType { get; }
    public Guid EntityId { get; }
    public string OldReason { get; }
    public string NewReason { get; }
    public string UpdatedByUser { get; }

    public LegalHoldUpdatedEvent(Guid legalHoldId, string entityType, Guid entityId, string oldReason, string newReason, string updatedByUser)
    {
        LegalHoldId = legalHoldId;
        EntityType = entityType;
        EntityId = entityId;
        OldReason = oldReason;
        NewReason = newReason;
        UpdatedByUser = updatedByUser;
    }
}