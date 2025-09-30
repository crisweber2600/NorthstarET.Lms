using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a staff member in the district
/// </summary>
public class Staff : TenantScopedEntity
{
    /// <summary>
    /// User ID (from identity system)
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// External ID from IdP
    /// </summary>
    public string? ExternalId { get; private set; }

    /// <summary>
    /// External issuer (IdP identifier)
    /// </summary>
    public string? ExternalIssuer { get; private set; }

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Employment status (Active, Suspended, Terminated)
    /// </summary>
    public string EmploymentStatus { get; private set; } = "Active";

    /// <summary>
    /// Hire date
    /// </summary>
    public DateTime HireDate { get; private set; }

    /// <summary>
    /// End date (if terminated)
    /// </summary>
    public DateTime? EndDate { get; private set; }

    /// <summary>
    /// Created by (user identifier)
    /// </summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Created at timestamp
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last updated by (user identifier)
    /// </summary>
    public string? UpdatedBy { get; private set; }

    /// <summary>
    /// Last updated at timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Full name (computed property)
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    // EF Core constructor
    protected Staff() { }

    /// <summary>
    /// Create a new staff member
    /// </summary>
    public Staff(
        string tenantSlug,
        Guid userId,
        string firstName,
        string lastName,
        string email,
        DateTime hireDate,
        string createdBy,
        string? externalId = null,
        string? externalIssuer = null)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug is required", nameof(tenantSlug));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by is required", nameof(createdBy));

        TenantSlug = tenantSlug;
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        HireDate = hireDate;
        ExternalId = externalId;
        ExternalIssuer = externalIssuer;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        EmploymentStatus = "Active";

        AddDomainEvent(new StaffCreatedEvent(Id, UserId, FullName, Email, createdBy));
    }

    /// <summary>
    /// Update employment status
    /// </summary>
    public void UpdateEmploymentStatus(string newStatus, string updatedBy, DateTime? endDate = null)
    {
        if (string.IsNullOrWhiteSpace(newStatus))
            throw new ArgumentException("Status is required", nameof(newStatus));
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));

        var oldStatus = EmploymentStatus;
        EmploymentStatus = newStatus;
        EndDate = endDate;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new StaffEmploymentStatusChangedEvent(Id, UserId, oldStatus, newStatus, updatedBy));
    }

    /// <summary>
    /// Suspend staff member (revokes active assignments but preserves history)
    /// </summary>
    public void Suspend(string reason, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));

        UpdateEmploymentStatus("Suspended", updatedBy);
        AddDomainEvent(new StaffSuspendedEvent(Id, UserId, reason, updatedBy));
    }

    /// <summary>
    /// Update staff information
    /// </summary>
    public void UpdateInfo(
        string? firstName,
        string? lastName,
        string? email,
        string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));

        if (!string.IsNullOrWhiteSpace(firstName))
            FirstName = firstName;
        if (!string.IsNullOrWhiteSpace(lastName))
            LastName = lastName;
        if (!string.IsNullOrWhiteSpace(email))
            Email = email;

        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
