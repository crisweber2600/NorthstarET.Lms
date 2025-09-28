using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Domain.Events;

// Guardian Events
public record GuardianCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public string FullName { get; }
    public string Email { get; }

    public GuardianCreatedEvent(Guid userId, string fullName, string email)
    {
        UserId = userId;
        FullName = fullName;
        Email = email;
    }
}

public record GuardianContactUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid GuardianId { get; }
    public string OldEmail { get; }
    public string NewEmail { get; }
    public string OldPhone { get; }
    public string NewPhone { get; }
    public string UpdatedByUserId { get; }

    public GuardianContactUpdatedEvent(Guid guardianId, string oldEmail, string newEmail, string oldPhone, string newPhone, string updatedByUserId)
    {
        GuardianId = guardianId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
        OldPhone = oldPhone;
        NewPhone = newPhone;
        UpdatedByUserId = updatedByUserId;
    }
}

public record GuardianStudentRelationshipAddedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid GuardianId { get; }
    public Guid StudentId { get; }
    public RelationshipType RelationshipType { get; }
    public bool IsPrimary { get; }
    public DateTime EffectiveDate { get; }
    public string AddedByUserId { get; }

    public GuardianStudentRelationshipAddedEvent(Guid guardianId, Guid studentId, RelationshipType relationshipType, bool isPrimary, DateTime effectiveDate, string addedByUserId)
    {
        GuardianId = guardianId;
        StudentId = studentId;
        RelationshipType = relationshipType;
        IsPrimary = isPrimary;
        EffectiveDate = effectiveDate;
        AddedByUserId = addedByUserId;
    }
}

public record GuardianStudentRelationshipEndedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid GuardianId { get; }
    public Guid StudentId { get; }
    public DateTime EndDate { get; }
    public string Reason { get; }
    public string EndedByUserId { get; }

    public GuardianStudentRelationshipEndedEvent(Guid guardianId, Guid studentId, DateTime endDate, string reason, string endedByUserId)
    {
        GuardianId = guardianId;
        StudentId = studentId;
        EndDate = endDate;
        Reason = reason;
        EndedByUserId = endedByUserId;
    }
}

public record GuardianPickupPermissionUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid GuardianId { get; }
    public Guid StudentId { get; }
    public bool CanPickup { get; }
    public string UpdatedByUserId { get; }

    public GuardianPickupPermissionUpdatedEvent(Guid guardianId, Guid studentId, bool canPickup, string updatedByUserId)
    {
        GuardianId = guardianId;
        StudentId = studentId;
        CanPickup = canPickup;
        UpdatedByUserId = updatedByUserId;
    }
}

public record GuardianSetAsPrimaryEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid GuardianId { get; }
    public Guid StudentId { get; }
    public string UpdatedByUserId { get; }

    public GuardianSetAsPrimaryEvent(Guid guardianId, Guid studentId, string updatedByUserId)
    {
        GuardianId = guardianId;
        StudentId = studentId;
        UpdatedByUserId = updatedByUserId;
    }
}

public record GuardianRemovedAsPrimaryEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid GuardianId { get; }
    public Guid StudentId { get; }
    public string UpdatedByUserId { get; }

    public GuardianRemovedAsPrimaryEvent(Guid guardianId, Guid studentId, string updatedByUserId)
    {
        GuardianId = guardianId;
        StudentId = studentId;
        UpdatedByUserId = updatedByUserId;
    }
}

public record GuardianDeactivatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid GuardianId { get; }
    public string Reason { get; }
    public DateTime DeactivationDate { get; }
    public string DeactivatedByUserId { get; }

    public GuardianDeactivatedEvent(Guid guardianId, string reason, DateTime deactivationDate, string deactivatedByUserId)
    {
        GuardianId = guardianId;
        Reason = reason;
        DeactivationDate = deactivationDate;
        DeactivatedByUserId = deactivatedByUserId;
    }
}

// Identity Events
public record IdentityMappedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public string ExternalId { get; }
    public string Issuer { get; }

    public IdentityMappedEvent(Guid userId, string externalId, string issuer)
    {
        UserId = userId;
        ExternalId = externalId;
        Issuer = issuer;
    }
}

public record IdentityUnmappedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public string ExternalId { get; }
    public string Issuer { get; }
    public string DeactivatedByUserId { get; }

    public IdentityUnmappedEvent(Guid userId, string externalId, string issuer, string deactivatedByUserId)
    {
        UserId = userId;
        ExternalId = externalId;
        Issuer = issuer;
        DeactivatedByUserId = deactivatedByUserId;
    }
}

public record IdentityRemappedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public string ExternalId { get; }
    public string Issuer { get; }
    public string ReactivatedByUserId { get; }

    public IdentityRemappedEvent(Guid userId, string externalId, string issuer, string reactivatedByUserId)
    {
        UserId = userId;
        ExternalId = externalId;
        Issuer = issuer;
        ReactivatedByUserId = reactivatedByUserId;
    }
}

// Guardian Relationship Events
public record GuardianRelationshipCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RelationshipId { get; }
    public Guid StudentId { get; }
    public Guid GuardianId { get; }
    public GuardianRelationshipType RelationshipType { get; }

    public GuardianRelationshipCreatedEvent(Guid relationshipId, Guid studentId, Guid guardianId, GuardianRelationshipType relationshipType)
    {
        RelationshipId = relationshipId;
        StudentId = studentId;
        GuardianId = guardianId;
        RelationshipType = relationshipType;
    }
}

public record GuardianRelationshipDeactivatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RelationshipId { get; }
    public Guid StudentId { get; }
    public Guid GuardianId { get; }
    public string Reason { get; }
    public string DeactivatedBy { get; }

    public GuardianRelationshipDeactivatedEvent(Guid relationshipId, Guid studentId, Guid guardianId, string reason, string deactivatedBy)
    {
        RelationshipId = relationshipId;
        StudentId = studentId;
        GuardianId = guardianId;
        Reason = reason;
        DeactivatedBy = deactivatedBy;
    }
}

public record GuardianRelationshipUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RelationshipId { get; }
    public Guid StudentId { get; }
    public Guid GuardianId { get; }
    public string FieldUpdated { get; }
    public string UpdatedBy { get; }

    public GuardianRelationshipUpdatedEvent(Guid relationshipId, Guid studentId, Guid guardianId, string fieldUpdated, string updatedBy)
    {
        RelationshipId = relationshipId;
        StudentId = studentId;
        GuardianId = guardianId;
        FieldUpdated = fieldUpdated;
        UpdatedBy = updatedBy;
    }
}