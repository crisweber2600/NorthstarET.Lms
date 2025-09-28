using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public class GuardianRelationship : TenantScopedEntity
{
    // Private constructor for EF Core
    private GuardianRelationship() { }

    public GuardianRelationship(
        Guid studentId,
        Guid guardianId,
        GuardianRelationshipType relationshipType,
        bool isPrimary = false,
        bool hasPickupPermission = true)
    {
        StudentId = studentId;
        GuardianId = guardianId;
        RelationshipType = relationshipType;
        IsPrimary = isPrimary;
        HasPickupPermission = hasPickupPermission;
        IsActive = true;
        
        AddDomainEvent(new GuardianRelationshipCreatedEvent(Id, studentId, guardianId, relationshipType));
    }

    public Guid StudentId { get; private set; }
    public Guid GuardianId { get; private set; }
    public GuardianRelationshipType RelationshipType { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool HasPickupPermission { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? DeactivatedDate { get; private set; }
    public string? DeactivationReason { get; private set; }

    // Navigation properties
    public virtual Student Student { get; private set; } = null!;
    public virtual Guardian Guardian { get; private set; } = null!;

    public void Deactivate(string reason, string deactivatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Guardian relationship is already inactive");
            
        IsActive = false;
        DeactivatedDate = DateTime.UtcNow;
        DeactivationReason = reason;
        MarkAsModified();
        
        AddDomainEvent(new GuardianRelationshipDeactivatedEvent(Id, StudentId, GuardianId, reason, deactivatedBy));
    }

    public void SetPrimaryStatus(bool isPrimary, string updatedBy)
    {
        if (IsPrimary == isPrimary)
            return; // No change
            
        IsPrimary = isPrimary;
        MarkAsModified();
        
        AddDomainEvent(new GuardianRelationshipUpdatedEvent(Id, StudentId, GuardianId, "PrimaryStatus", updatedBy));
    }

    public void SetPickupPermission(bool hasPermission, string updatedBy)
    {
        if (HasPickupPermission == hasPermission)
            return; // No change
            
        HasPickupPermission = hasPermission;
        MarkAsModified();
        
        AddDomainEvent(new GuardianRelationshipUpdatedEvent(Id, StudentId, GuardianId, "PickupPermission", updatedBy));
    }
}