using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a guardian/parent associated with students
/// </summary>
public class Guardian : TenantScopedEntity
{
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
    public string? Email { get; private set; }

    /// <summary>
    /// Phone number
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Address
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Full name (computed property)
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    // EF Core constructor
    protected Guardian() { }

    /// <summary>
    /// Create a new guardian
    /// </summary>
    public Guardian(
        string tenantSlug,
        string firstName,
        string lastName,
        string createdBy,
        string? email = null,
        string? phone = null,
        string? address = null)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug is required", nameof(tenantSlug));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by is required", nameof(createdBy));

        InitializeTenant(tenantSlug);
        SetAuditFields(createdBy);
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Address = address;

        AddDomainEvent(new GuardianCreatedEvent(Id, FullName, Email, createdBy));
    }

    /// <summary>
    /// Update guardian information
    /// </summary>
    public void UpdateInfo(
        string? firstName,
        string? lastName,
        string? email,
        string? phone,
        string? address,
        string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));

        if (!string.IsNullOrWhiteSpace(firstName))
            FirstName = firstName;
        if (!string.IsNullOrWhiteSpace(lastName))
            LastName = lastName;

        Email = email;
        Phone = phone;
        Address = address;
        UpdateAuditFields(updatedBy);
    }
}
