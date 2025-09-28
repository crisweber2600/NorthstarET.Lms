namespace NorthstarET.Lms.Domain.Events;

public record DistrictProvisionedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid DistrictId { get; }
    public string Slug { get; }
    public string DisplayName { get; }
    public string CreatedByUserId { get; }

    public DistrictProvisionedEvent(Guid districtId, string slug, string displayName, string createdByUserId)
    {
        DistrictId = districtId;
        Slug = slug;
        DisplayName = displayName;
        CreatedByUserId = createdByUserId;
    }
}

public record DistrictSuspendedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid DistrictId { get; }
    public string Reason { get; }
    public string SuspendedByUserId { get; }

    public DistrictSuspendedEvent(Guid districtId, string reason, string suspendedByUserId)
    {
        DistrictId = districtId;
        Reason = reason;
        SuspendedByUserId = suspendedByUserId;
    }
}