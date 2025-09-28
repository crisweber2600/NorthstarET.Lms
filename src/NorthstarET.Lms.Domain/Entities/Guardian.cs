using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public class Guardian : TenantScopedEntity
{
    private readonly List<GuardianStudentRelationship> _studentRelationships = new();

    // Private constructor for EF Core
    private Guardian() { }

    public Guardian(
        string firstName,
        string lastName,
        string email,
        string phone)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        UserId = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone ?? string.Empty;
        Status = UserLifecycleStatus.Active;
        
        AddDomainEvent(new GuardianCreatedEvent(UserId, $"{firstName} {lastName}", email));
    }

    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public UserLifecycleStatus Status { get; private set; }
    
    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public IdentityMapping? IdentityMapping { get; private set; }
    public IReadOnlyCollection<GuardianStudentRelationship> StudentRelationships => _studentRelationships.AsReadOnly();

    public void UpdateContactInfo(string email, string phone, string updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        var oldEmail = Email;
        var oldPhone = Phone;
        
        Email = email;
        Phone = phone ?? string.Empty;
        MarkAsModified();
        
        if (oldEmail != email || oldPhone != Phone)
        {
            AddDomainEvent(new GuardianContactUpdatedEvent(UserId, oldEmail, email, oldPhone, Phone, updatedByUserId));
        }
    }

    public GuardianStudentRelationship AddStudentRelationship(
        Guid studentId, 
        RelationshipType relationshipType, 
        bool isPrimary, 
        bool canPickup,
        DateTime effectiveDate,
        string addedByUserId)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId cannot be empty", nameof(studentId));
        
        if (effectiveDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Effective date cannot be in the future", nameof(effectiveDate));

        // Check for existing active relationship
        var existingRelationship = _studentRelationships
            .FirstOrDefault(r => r.StudentId == studentId && r.IsActive);
        
        if (existingRelationship != null)
            throw new InvalidOperationException("Active relationship with this student already exists");

        var relationship = new GuardianStudentRelationship(
            UserId, studentId, relationshipType, isPrimary, canPickup, effectiveDate);
        
        _studentRelationships.Add(relationship);
        MarkAsModified();
        
        AddDomainEvent(new GuardianStudentRelationshipAddedEvent(
            UserId, studentId, relationshipType, isPrimary, effectiveDate, addedByUserId));

        return relationship;
    }

    public void EndStudentRelationship(Guid studentId, DateTime endDate, string reason, string endedByUserId)
    {
        var relationship = _studentRelationships
            .FirstOrDefault(r => r.StudentId == studentId && r.IsActive);
        
        if (relationship == null)
            throw new ArgumentException("Active relationship with this student not found", nameof(studentId));
        
        if (endDate > DateTime.UtcNow.Date)
            throw new ArgumentException("End date cannot be in the future", nameof(endDate));
        
        if (endDate < relationship.EffectiveDate)
            throw new ArgumentException("End date cannot be before effective date", nameof(endDate));

        relationship.EndRelationship(endDate, reason);
        MarkAsModified();
        
        AddDomainEvent(new GuardianStudentRelationshipEndedEvent(
            UserId, studentId, endDate, reason, endedByUserId));
    }

    public void Deactivate(string reason, DateTime deactivationDate, string deactivatedByUserId)
    {
        if (Status != UserLifecycleStatus.Active)
            throw new InvalidOperationException("Guardian is not currently active");
        
        if (deactivationDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Deactivation date cannot be in the future", nameof(deactivationDate));

        Status = UserLifecycleStatus.Withdrawn;
        
        // End all active relationships
        var activeRelationships = _studentRelationships.Where(r => r.IsActive).ToList();
        foreach (var relationship in activeRelationships)
        {
            relationship.EndRelationship(deactivationDate, "Guardian deactivated");
        }
        
        MarkAsModified();
        
        AddDomainEvent(new GuardianDeactivatedEvent(UserId, reason, deactivationDate, deactivatedByUserId));
    }

    public bool IsActive => Status == UserLifecycleStatus.Active;
    
    public bool HasActiveRelationshipWith(Guid studentId)
    {
        return _studentRelationships.Any(r => r.StudentId == studentId && r.IsActive);
    }
    
    public bool CanPickupStudent(Guid studentId)
    {
        var relationship = _studentRelationships
            .FirstOrDefault(r => r.StudentId == studentId && r.IsActive);
        
        return relationship?.CanPickup == true;
    }
}

public class GuardianStudentRelationship : TenantScopedEntity
{
    // Private constructor for EF Core
    private GuardianStudentRelationship() { }

    public GuardianStudentRelationship(
        Guid guardianId,
        Guid studentId,
        RelationshipType relationshipType,
        bool isPrimary,
        bool canPickup,
        DateTime effectiveDate)
    {
        if (guardianId == Guid.Empty)
            throw new ArgumentException("GuardianId cannot be empty", nameof(guardianId));
        
        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId cannot be empty", nameof(studentId));
        
        if (effectiveDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Effective date cannot be in the future", nameof(effectiveDate));

        GuardianId = guardianId;
        StudentId = studentId;
        RelationshipType = relationshipType;
        IsPrimary = isPrimary;
        CanPickup = canPickup;
        EffectiveDate = effectiveDate;
    }

    public Guid GuardianId { get; private set; }
    public Guid StudentId { get; private set; }
    public RelationshipType RelationshipType { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool CanPickup { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? EndReason { get; private set; }

    // Navigation properties
    public Guardian Guardian { get; private set; } = null!;
    public Student Student { get; private set; } = null!;

    public bool IsActive => EndDate == null;

    public void UpdatePickupPermission(bool canPickup, string updatedByUserId)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive relationship");

        if (CanPickup == canPickup)
            return; // No change

        CanPickup = canPickup;
        MarkAsModified();
        
        AddDomainEvent(new GuardianPickupPermissionUpdatedEvent(
            GuardianId, StudentId, canPickup, updatedByUserId));
    }

    public void SetAsPrimary(string updatedByUserId)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive relationship");
        
        if (IsPrimary)
            return; // Already primary

        IsPrimary = true;
        MarkAsModified();
        
        AddDomainEvent(new GuardianSetAsPrimaryEvent(GuardianId, StudentId, updatedByUserId));
    }

    public void RemoveAsPrimary(string updatedByUserId)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive relationship");
        
        if (!IsPrimary)
            return; // Already not primary

        IsPrimary = false;
        MarkAsModified();
        
        AddDomainEvent(new GuardianRemovedAsPrimaryEvent(GuardianId, StudentId, updatedByUserId));
    }

    internal void EndRelationship(DateTime endDate, string reason)
    {
        if (!IsActive)
            throw new InvalidOperationException("Relationship is already ended");
        
        EndDate = endDate;
        EndReason = reason;
        MarkAsModified();
    }
}