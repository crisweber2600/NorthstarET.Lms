using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a school district tenant with multi-tenant isolation and quota management
/// </summary>
public class DistrictTenant : TenantScopedEntity
{
    /// <summary>
    /// Unique slug for the district (immutable after creation)
    /// </summary>
    public TenantSlug Slug { get; private set; }

    /// <summary>
    /// Display name for the district
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Current status of the district
    /// </summary>
    public DistrictStatus Status { get; private set; }

    /// <summary>
    /// Resource quotas for this district
    /// </summary>
    public Quota Quotas { get; private set; }

    /// <summary>
    /// When the district was activated
    /// </summary>
    public DateTime? ActivatedAt { get; private set; }

    /// <summary>
    /// When the district was suspended (if applicable)
    /// </summary>
    public DateTime? SuspendedAt { get; private set; }

    /// <summary>
    /// Reason for suspension (if applicable)
    /// </summary>
    public string? SuspendedReason { get; private set; }

    // EF Core constructor
    protected DistrictTenant() 
    {
        Slug = new TenantSlug("placeholder");
        DisplayName = string.Empty;
        Quotas = Quota.Default;
    }

    /// <summary>
    /// Create a new district tenant with default quotas
    /// </summary>
    public DistrictTenant(string slug, string displayName, string createdBy) : this()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be null or empty", nameof(displayName));

        Slug = new TenantSlug(slug);
        DisplayName = displayName.Trim();
        Status = DistrictStatus.Active;
        Quotas = Quota.Default;
        ActivatedAt = DateTime.UtcNow;

        // Initialize tenant scope
        InitializeTenant(slug);
        SetAuditFields(createdBy);

        // Raise domain event
        AddDomainEvent(new DistrictProvisionedEvent(Id, Slug, DisplayName)
        {
            TenantSlug = slug,
            TriggeredBy = createdBy
        });
    }

    /// <summary>
    /// Create a new district tenant with custom quotas
    /// </summary>
    public DistrictTenant(string slug, string displayName, Quota customQuotas, string createdBy) : this(slug, displayName, createdBy)
    {
        Quotas = customQuotas ?? throw new ArgumentNullException(nameof(customQuotas));
    }

    /// <summary>
    /// Suspend the district with a reason
    /// </summary>
    public void Suspend(string reason, string updatedBy)
    {
        if (Status == DistrictStatus.Suspended)
            throw new InvalidOperationException("District is already suspended");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Suspension reason is required", nameof(reason));

        Status = DistrictStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
        SuspendedReason = reason.Trim();
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new DistrictSuspendedEvent(Id, reason)
        {
            TenantSlug = Slug,
            TriggeredBy = updatedBy
        });
    }

    /// <summary>
    /// Activate the district
    /// </summary>
    public void Activate(string updatedBy)
    {
        if (Status == DistrictStatus.Active)
            throw new InvalidOperationException("District is already active");

        Status = DistrictStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        SuspendedAt = null;
        SuspendedReason = null;
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new DistrictActivatedEvent(Id)
        {
            TenantSlug = Slug,
            TriggeredBy = updatedBy
        });
    }

    /// <summary>
    /// Update district quotas
    /// </summary>
    public void UpdateQuotas(Quota newQuotas, string updatedBy)
    {
        Quotas = newQuotas ?? throw new ArgumentNullException(nameof(newQuotas));
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Update display name
    /// </summary>
    public void UpdateDisplayName(string newDisplayName, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(newDisplayName))
            throw new ArgumentException("Display name cannot be null or empty", nameof(newDisplayName));

        DisplayName = newDisplayName.Trim();
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Check if deletion is allowed (no legal holds or retention windows)
    /// </summary>
    public bool CanBeDeleted()
    {
        // TODO: Check for active legal holds and retention windows
        return Status == DistrictStatus.Suspended;
    }
}

/// <summary>
/// District status enumeration
/// </summary>
public enum DistrictStatus
{
    Active,
    Suspended,
    Deleted
}