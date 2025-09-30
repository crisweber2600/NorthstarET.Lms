using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a school within a district
/// </summary>
public class School : TenantScopedEntity
{
    /// <summary>
    /// The district this school belongs to
    /// </summary>
    public Guid DistrictId { get; private set; }

    /// <summary>
    /// School name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// External code/identifier from SIS or other systems
    /// </summary>
    public string? ExternalCode { get; private set; }

    /// <summary>
    /// Type of school (Elementary, Middle, High, etc.)
    /// </summary>
    public string SchoolType { get; private set; } = string.Empty;

    /// <summary>
    /// School status (Active, Inactive, Closed)
    /// </summary>
    public string Status { get; private set; } = "Active";

    /// <summary>
    /// School address
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// School phone number
    /// </summary>
    public string? Phone { get; private set; }

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

    // EF Core constructor
    protected School() { }

    /// <summary>
    /// Create a new school
    /// </summary>
    public School(
        string tenantSlug,
        Guid districtId,
        string name,
        string schoolType,
        string createdBy,
        string? externalCode = null,
        string? address = null,
        string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug is required", nameof(tenantSlug));
        if (districtId == Guid.Empty)
            throw new ArgumentException("District ID cannot be empty", nameof(districtId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("School name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(schoolType))
            throw new ArgumentException("School type is required", nameof(schoolType));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by is required", nameof(createdBy));

        TenantSlug = tenantSlug;
        DistrictId = districtId;
        Name = name;
        SchoolType = schoolType;
        ExternalCode = externalCode;
        Address = address;
        Phone = phone;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Status = "Active";
    }

    /// <summary>
    /// Update school status
    /// </summary>
    public void UpdateStatus(string newStatus, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(newStatus))
            throw new ArgumentException("Status is required", nameof(newStatus));
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));

        var oldStatus = Status;
        Status = newStatus;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event for RBAC recalculation
        AddDomainEvent(new SchoolStatusChangedEvent(Id, oldStatus, newStatus, updatedBy));
    }

    /// <summary>
    /// Update school information
    /// </summary>
    public void UpdateInfo(
        string? name,
        string? schoolType,
        string? address,
        string? phone,
        string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));

        if (!string.IsNullOrWhiteSpace(name))
            Name = name;
        if (!string.IsNullOrWhiteSpace(schoolType))
            SchoolType = schoolType;

        Address = address;
        Phone = phone;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
