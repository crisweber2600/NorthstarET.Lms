using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public class Staff : TenantScopedEntity
{
    private readonly List<string> _specializations = new();
    
    // Private constructor for EF Core
    private Staff() { }

    public Staff(
        string employeeNumber,
        string firstName,
        string lastName,
        string email,
        DateTime hireDate)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("Employee number is required", nameof(employeeNumber));
        
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        
        if (hireDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Hire date cannot be in the future", nameof(hireDate));

        UserId = Guid.NewGuid();
        EmployeeNumber = employeeNumber;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Status = UserLifecycleStatus.Active;
        HireDate = hireDate;
        
        AddDomainEvent(new StaffHiredEvent(UserId, employeeNumber, $"{firstName} {lastName}", hireDate));
    }

    public Guid UserId { get; private set; }
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserLifecycleStatus Status { get; private set; }
    public DateTime HireDate { get; private set; }
    public DateTime? TerminationDate { get; private set; }
    public string FullName => $"{FirstName} {LastName}";

    public IReadOnlyCollection<string> Specializations => _specializations.AsReadOnly();

    // Navigation properties
    public IdentityMapping? IdentityMapping { get; private set; }
    public ICollection<RoleAssignment> RoleAssignments { get; private set; } = new List<RoleAssignment>();

    public void UpdateEmail(string newEmail, string updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("Email is required", nameof(newEmail));
        
        if (newEmail == Email)
            return; // No change

        var oldEmail = Email;
        Email = newEmail;
        MarkAsModified();
        
        AddDomainEvent(new StaffEmailUpdatedEvent(UserId, oldEmail, newEmail, updatedByUserId));
    }

    public void AddSpecialization(string specialization, string addedByUserId)
    {
        if (string.IsNullOrWhiteSpace(specialization))
            throw new ArgumentException("Specialization is required", nameof(specialization));
        
        if (_specializations.Contains(specialization, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Staff member already has specialization: {specialization}");

        _specializations.Add(specialization);
        MarkAsModified();
        
        AddDomainEvent(new StaffSpecializationAddedEvent(UserId, specialization, addedByUserId));
    }

    public void RemoveSpecialization(string specialization, string removedByUserId)
    {
        var existingSpecialization = _specializations.FirstOrDefault(s => 
            string.Equals(s, specialization, StringComparison.OrdinalIgnoreCase));
        
        if (existingSpecialization == null)
            throw new ArgumentException("Specialization not found", nameof(specialization));

        _specializations.Remove(existingSpecialization);
        MarkAsModified();
        
        AddDomainEvent(new StaffSpecializationRemovedEvent(UserId, existingSpecialization, removedByUserId));
    }

    public void Suspend(string reason, DateTime suspensionDate, string suspendedByUserId)
    {
        if (Status == UserLifecycleStatus.Suspended)
            throw new InvalidOperationException("Staff member is already suspended");
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Suspension reason is required", nameof(reason));
        
        if (suspensionDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Suspension date cannot be in the future", nameof(suspensionDate));

        Status = UserLifecycleStatus.Suspended;
        MarkAsModified();
        
        AddDomainEvent(new StaffSuspendedEvent(UserId, reason, suspensionDate, suspendedByUserId));
    }

    public void Reinstate(DateTime reinstateDate, string reinstatedByUserId)
    {
        if (Status != UserLifecycleStatus.Suspended)
            throw new InvalidOperationException("Can only reinstate suspended staff members");
        
        if (reinstateDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Reinstate date cannot be in the future", nameof(reinstateDate));

        Status = UserLifecycleStatus.Active;
        MarkAsModified();
        
        AddDomainEvent(new StaffReinstatedEvent(UserId, reinstateDate, reinstatedByUserId));
    }

    public void Terminate(DateTime terminationDate, string reason, string terminatedByUserId)
    {
        if (Status == UserLifecycleStatus.Withdrawn)
            throw new InvalidOperationException("Staff member is already terminated");
        
        if (terminationDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Termination date cannot be in the future", nameof(terminationDate));
        
        if (terminationDate < HireDate)
            throw new ArgumentException("Termination date cannot be before hire date", nameof(terminationDate));
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Termination reason is required", nameof(reason));

        Status = UserLifecycleStatus.Withdrawn;
        TerminationDate = terminationDate;
        MarkAsModified();
        
        AddDomainEvent(new StaffTerminatedEvent(UserId, terminationDate, reason, terminatedByUserId));
    }

    public bool IsActive => Status == UserLifecycleStatus.Active;
    
    public bool CanBeAssignedToRole => Status == UserLifecycleStatus.Active;
}

public class IdentityMapping : TenantScopedEntity
{
    // Private constructor for EF Core
    private IdentityMapping() { }

    public IdentityMapping(Guid userId, string externalId, string issuer)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("ExternalId is required", nameof(externalId));
        
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("Issuer is required", nameof(issuer));

        UserId = userId;
        ExternalId = externalId;
        Issuer = issuer;
        MappedDate = DateTime.UtcNow;
        IsActive = true;
        
        AddDomainEvent(new IdentityMappedEvent(userId, externalId, issuer));
    }

    public Guid UserId { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string Issuer { get; private set; } = string.Empty;
    public DateTime MappedDate { get; private set; }
    public DateTime? UnmappedDate { get; private set; }
    public bool IsActive { get; private set; }

    public void Deactivate(string deactivatedByUserId)
    {
        if (!IsActive)
            throw new InvalidOperationException("Identity mapping is already inactive");

        IsActive = false;
        UnmappedDate = DateTime.UtcNow;
        MarkAsModified();
        
        AddDomainEvent(new IdentityUnmappedEvent(UserId, ExternalId, Issuer, deactivatedByUserId));
    }

    public void Reactivate(string reactivatedByUserId)
    {
        if (IsActive)
            throw new InvalidOperationException("Identity mapping is already active");

        IsActive = true;
        UnmappedDate = null;
        MarkAsModified();
        
        AddDomainEvent(new IdentityRemappedEvent(UserId, ExternalId, Issuer, reactivatedByUserId));
    }
}