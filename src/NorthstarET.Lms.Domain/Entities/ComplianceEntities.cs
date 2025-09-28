using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public class RoleDefinition : TenantScopedEntity
{
    private readonly List<string> _permissions = new();

    // Private constructor for EF Core
    private RoleDefinition() { }

    public RoleDefinition(
        string name,
        string description,
        RoleScope scope,
        bool isSystemRole = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required", nameof(name));
        
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Role description is required", nameof(description));

        Name = name;
        Description = description;
        Scope = scope;
        IsSystemRole = isSystemRole;
        AllowsDelegation = false; // Default to false for security
        
        AddDomainEvent(new RoleDefinitionCreatedEvent(Id, name, scope, isSystemRole));
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public RoleScope Scope { get; private set; }
    public bool IsSystemRole { get; private set; }
    public bool AllowsDelegation { get; private set; }
    
    public IReadOnlyCollection<string> Permissions => _permissions.AsReadOnly();

    // Navigation properties
    public ICollection<RoleAssignment> Assignments { get; private set; } = new List<RoleAssignment>();

    public void UpdateDescription(string newDescription, string updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new ArgumentException("Description is required", nameof(newDescription));
        
        if (newDescription == Description)
            return; // No change

        var oldDescription = Description;
        Description = newDescription;
        MarkAsModified();
        
        AddDomainEvent(new RoleDefinitionUpdatedEvent(Id, Name, oldDescription, newDescription, updatedByUserId));
    }

    public void AddPermission(string permission, string addedByUserId)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission is required", nameof(permission));
        
        if (_permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Permission '{permission}' already exists in role");

        _permissions.Add(permission);
        MarkAsModified();
        
        AddDomainEvent(new RolePermissionAddedEvent(Id, Name, permission, addedByUserId));
    }

    public void RemovePermission(string permission, string removedByUserId)
    {
        var existingPermission = _permissions.FirstOrDefault(p => 
            string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
        
        if (existingPermission == null)
            throw new ArgumentException("Permission not found in role", nameof(permission));

        _permissions.Remove(existingPermission);
        MarkAsModified();
        
        AddDomainEvent(new RolePermissionRemovedEvent(Id, Name, existingPermission, removedByUserId));
    }

    public void EnableDelegation(string enabledByUserId)
    {
        if (AllowsDelegation)
            return; // Already enabled

        AllowsDelegation = true;
        MarkAsModified();
        
        AddDomainEvent(new RoleDelegationEnabledEvent(Id, Name, enabledByUserId));
    }

    public void DisableDelegation(string disabledByUserId)
    {
        if (!AllowsDelegation)
            return; // Already disabled

        AllowsDelegation = false;
        MarkAsModified();
        
        AddDomainEvent(new RoleDelegationDisabledEvent(Id, Name, disabledByUserId));
    }

    public bool HasPermission(string permission)
    {
        return _permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> GetPermissions()
    {
        return _permissions.AsReadOnly();
    }

    public bool CanBeDeleted()
    {
        // System roles cannot be deleted
        return !IsSystemRole;
    }

    public void ValidateForDeletion()
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System roles cannot be deleted");
        
        if (Assignments.Any(a => a.IsEffective))
            throw new InvalidOperationException("Cannot delete role with active assignments");
    }
}

public class RetentionPolicy : TenantScopedEntity
{
    // Private constructor for EF Core
    private RetentionPolicy() { }

    public RetentionPolicy(
        string entityType,
        int retentionYears,
        bool isDefault = false,
        DateTime? effectiveDate = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required", nameof(entityType));
        
        if (retentionYears < 1)
            throw new ArgumentException("Retention years must be at least 1", nameof(retentionYears));

        EntityType = entityType;
        RetentionYears = retentionYears;
        IsDefault = isDefault;
        EffectiveDate = effectiveDate ?? DateTime.UtcNow.Date;
        
        AddDomainEvent(new RetentionPolicyCreatedEvent(Id, entityType, retentionYears, isDefault));
    }

    public string EntityType { get; private set; } = string.Empty;
    public int RetentionYears { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? SupersededDate { get; private set; }

    public bool IsActive => SupersededDate == null && EffectiveDate <= DateTime.UtcNow.Date;

    public void UpdateRetentionPeriod(int newRetentionYears, string updatedByUserId)
    {
        if (newRetentionYears < 1)
            throw new ArgumentException("Retention years must be at least 1", nameof(newRetentionYears));
        
        if (newRetentionYears == RetentionYears)
            return; // No change

        var oldRetentionYears = RetentionYears;
        RetentionYears = newRetentionYears;
        MarkAsModified();
        
        AddDomainEvent(new RetentionPolicyUpdatedEvent(Id, EntityType, oldRetentionYears, newRetentionYears, updatedByUserId));
    }

    public void Supersede(DateTime supersededDate, string supersededByUserId)
    {
        if (SupersededDate.HasValue)
            throw new InvalidOperationException("Policy is already superseded");
        
        if (supersededDate < EffectiveDate)
            throw new ArgumentException("Superseded date cannot be before effective date", nameof(supersededDate));

        SupersededDate = supersededDate;
        MarkAsModified();
        
        AddDomainEvent(new RetentionPolicySupersededEvent(Id, EntityType, supersededDate, supersededByUserId));
    }

    public DateTime CalculatePurgeDate(DateTime recordCreationDate)
    {
        return recordCreationDate.AddYears(RetentionYears);
    }

    public bool IsEligibleForPurge(DateTime recordCreationDate, DateTime currentDate)
    {
        var purgeDate = CalculatePurgeDate(recordCreationDate);
        return currentDate >= purgeDate;
    }
}

public class LegalHold : TenantScopedEntity
{
    // Private constructor for EF Core
    private LegalHold() { }

    public LegalHold(
        string entityType,
        Guid entityId,
        string reason,
        string authorizingUser,
        DateTime? holdDate = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required", nameof(entityType));
        
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity ID cannot be empty", nameof(entityId));
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Legal hold reason is required", nameof(reason));
        
        if (string.IsNullOrWhiteSpace(authorizingUser))
            throw new ArgumentException("Authorizing user is required", nameof(authorizingUser));

        EntityType = entityType;
        EntityId = entityId;
        Reason = reason;
        AuthorizingUser = authorizingUser;
        HoldDate = holdDate ?? DateTime.UtcNow;
        IsActive = true;
        
        AddDomainEvent(new LegalHoldCreatedEvent(Id, entityType, entityId, reason, authorizingUser));
    }

    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime HoldDate { get; private set; }
    public DateTime? ReleaseDate { get; private set; }
    public string AuthorizingUser { get; private set; } = string.Empty;
    public string? ReleasedByUser { get; private set; }
    public bool IsActive { get; private set; }

    public void Release(string releasedByUser, DateTime? releaseDate = null)
    {
        if (!IsActive)
            throw new InvalidOperationException("Legal hold is already released");
        
        if (string.IsNullOrWhiteSpace(releasedByUser))
            throw new ArgumentException("Released by user is required", nameof(releasedByUser));

        var actualReleaseDate = releaseDate ?? DateTime.UtcNow;
        
        if (actualReleaseDate < HoldDate)
            throw new ArgumentException("Release date cannot be before hold date", nameof(releaseDate));

        IsActive = false;
        ReleaseDate = actualReleaseDate;
        ReleasedByUser = releasedByUser;
        MarkAsModified();
        
        AddDomainEvent(new LegalHoldReleasedEvent(Id, EntityType, EntityId, actualReleaseDate, releasedByUser));
    }

    public void UpdateReason(string newReason, string updatedByUser)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive legal hold");
        
        if (string.IsNullOrWhiteSpace(newReason))
            throw new ArgumentException("Legal hold reason is required", nameof(newReason));
        
        if (newReason == Reason)
            return; // No change

        var oldReason = Reason;
        Reason = newReason;
        MarkAsModified();
        
        AddDomainEvent(new LegalHoldUpdatedEvent(Id, EntityType, EntityId, oldReason, newReason, updatedByUser));
    }

    public bool PreventsDataPurge()
    {
        return IsActive;
    }

    public TimeSpan HoldDuration()
    {
        var endDate = ReleaseDate ?? DateTime.UtcNow;
        return endDate - HoldDate;
    }
}

/// <summary>
/// Platform-level audit record for cross-tenant operations (not tenant-scoped)
/// </summary>
public class PlatformAuditRecord
{
    public Guid Id { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string UserId { get; private set; }
    public string Details { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private PlatformAuditRecord() // EF Constructor
    {
        Action = string.Empty;
        EntityType = string.Empty;
        UserId = string.Empty;
        Details = string.Empty;
        IpAddress = string.Empty;
    }

    public PlatformAuditRecord(
        string action,
        string entityType,
        Guid entityId,
        string userId,
        string details,
        string ipAddress,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty", nameof(action));
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type cannot be empty", nameof(entityType));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("Details cannot be empty", nameof(details));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));

        Id = Guid.NewGuid();
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        UserId = userId;
        Details = details;
        Timestamp = DateTime.UtcNow;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}