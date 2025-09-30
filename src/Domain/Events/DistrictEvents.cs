using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Raised when a new district tenant is provisioned
/// </summary>
public sealed class DistrictProvisionedEvent : DomainEvent
{
    public Guid DistrictId { get; }
    public string Slug { get; }
    public string DisplayName { get; }
    public DateTime ProvisionedAt { get; }

    public DistrictProvisionedEvent(Guid districtId, string slug, string displayName)
    {
        DistrictId = districtId;
        Slug = slug;
        DisplayName = displayName;
        ProvisionedAt = OccurredAt;
    }
}

/// <summary>
/// Raised when a district is suspended
/// </summary>
public sealed class DistrictSuspendedEvent : DomainEvent
{
    public Guid DistrictId { get; }
    public string Reason { get; }

    public DistrictSuspendedEvent(Guid districtId, string reason)
    {
        DistrictId = districtId;
        Reason = reason;
    }
}

/// <summary>
/// Raised when a district is activated
/// </summary>
public sealed class DistrictActivatedEvent : DomainEvent
{
    public Guid DistrictId { get; }

    public DistrictActivatedEvent(Guid districtId)
    {
        DistrictId = districtId;
    }
}