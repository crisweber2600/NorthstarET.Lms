using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a class within a school for a specific school year
/// </summary>
public class Class : TenantScopedEntity
{
    /// <summary>
    /// The school this class belongs to
    /// </summary>
    public Guid SchoolId { get; private set; }

    /// <summary>
    /// The school year this class is in
    /// </summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>
    /// Class name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Class code (unique per school year + school)
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Grade band (e.g., "K-2", "3-5", "9-12")
    /// </summary>
    public string? GradeBand { get; private set; }

    /// <summary>
    /// Maximum capacity
    /// </summary>
    public int Capacity { get; private set; }

    /// <summary>
    /// Class status (Active, Inactive, Closed)
    /// </summary>
    public string Status { get; private set; } = "Active";

    /// <summary>
    /// Override flag for exceeding capacity
    /// </summary>
    public bool CapacityOverrideEnabled { get; private set; }

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
    protected Class() { }

    /// <summary>
    /// Create a new class
    /// </summary>
    public Class(
        string tenantSlug,
        Guid schoolId,
        Guid schoolYearId,
        string name,
        string code,
        int capacity,
        string createdBy,
        string? gradeBand = null)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug is required", nameof(tenantSlug));
        if (schoolId == Guid.Empty)
            throw new ArgumentException("School ID cannot be empty", nameof(schoolId));
        if (schoolYearId == Guid.Empty)
            throw new ArgumentException("School year ID cannot be empty", nameof(schoolYearId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Class name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Class code is required", nameof(code));
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by is required", nameof(createdBy));

        TenantSlug = tenantSlug;
        SchoolId = schoolId;
        SchoolYearId = schoolYearId;
        Name = name;
        Code = code;
        Capacity = capacity;
        GradeBand = gradeBand;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Status = "Active";
        CapacityOverrideEnabled = false;

        AddDomainEvent(new ClassCreatedEvent(Id, SchoolId, SchoolYearId, Name, Code, createdBy));
    }

    /// <summary>
    /// Update class status
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

        AddDomainEvent(new ClassStatusChangedEvent(Id, oldStatus, newStatus, updatedBy));
    }

    /// <summary>
    /// Enable or disable capacity override
    /// </summary>
    public void SetCapacityOverride(bool enabled, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));

        CapacityOverrideEnabled = enabled;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update class capacity
    /// </summary>
    public void UpdateCapacity(int newCapacity, string updatedBy)
    {
        if (newCapacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0", nameof(newCapacity));
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));

        Capacity = newCapacity;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
