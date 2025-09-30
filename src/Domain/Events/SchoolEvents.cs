using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Event raised when school status changes
/// </summary>
public sealed class SchoolStatusChangedEvent : DomainEvent
{
    public Guid SchoolId { get; }
    public string OldStatus { get; }
    public string NewStatus { get; }
    public string ChangedBy { get; }

    public SchoolStatusChangedEvent(Guid schoolId, string oldStatus, string newStatus, string changedBy)
    {
        SchoolId = schoolId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
    }
}

/// <summary>
/// Event raised when a school is created
/// </summary>
public sealed class SchoolCreatedEvent : DomainEvent
{
    public Guid SchoolId { get; }
    public Guid DistrictId { get; }
    public string Name { get; }
    public string SchoolType { get; }
    public string CreatedBy { get; }

    public SchoolCreatedEvent(Guid schoolId, Guid districtId, string name, string schoolType, string createdBy)
    {
        SchoolId = schoolId;
        DistrictId = districtId;
        Name = name;
        SchoolType = schoolType;
        CreatedBy = createdBy;
    }
}
