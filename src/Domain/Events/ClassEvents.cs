using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Event raised when a class is created
/// </summary>
public sealed class ClassCreatedEvent : DomainEvent
{
    public Guid ClassId { get; }
    public Guid SchoolId { get; }
    public Guid SchoolYearId { get; }
    public string Name { get; }
    public string Code { get; }
    public string CreatedBy { get; }

    public ClassCreatedEvent(Guid classId, Guid schoolId, Guid schoolYearId, string name, string code, string createdBy)
    {
        ClassId = classId;
        SchoolId = schoolId;
        SchoolYearId = schoolYearId;
        Name = name;
        Code = code;
        CreatedBy = createdBy;
    }
}

/// <summary>
/// Event raised when class status changes
/// </summary>
public sealed class ClassStatusChangedEvent : DomainEvent
{
    public Guid ClassId { get; }
    public string OldStatus { get; }
    public string NewStatus { get; }
    public string ChangedBy { get; }

    public ClassStatusChangedEvent(Guid classId, string oldStatus, string newStatus, string changedBy)
    {
        ClassId = classId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
    }
}
