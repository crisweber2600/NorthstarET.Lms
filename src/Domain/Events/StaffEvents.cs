using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Event raised when a staff member is created
/// </summary>
public sealed class StaffCreatedEvent : DomainEvent
{
    public Guid StaffId { get; }
    public Guid UserId { get; }
    public string FullName { get; }
    public string Email { get; }
    public string CreatedBy { get; }

    public StaffCreatedEvent(Guid staffId, Guid userId, string fullName, string email, string createdBy)
    {
        StaffId = staffId;
        UserId = userId;
        FullName = fullName;
        Email = email;
        CreatedBy = createdBy;
    }
}

/// <summary>
/// Event raised when staff employment status changes
/// </summary>
public sealed class StaffEmploymentStatusChangedEvent : DomainEvent
{
    public Guid StaffId { get; }
    public Guid UserId { get; }
    public string OldStatus { get; }
    public string NewStatus { get; }
    public string ChangedBy { get; }

    public StaffEmploymentStatusChangedEvent(Guid staffId, Guid userId, string oldStatus, string newStatus, string changedBy)
    {
        StaffId = staffId;
        UserId = userId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
    }
}

/// <summary>
/// Event raised when staff is suspended
/// </summary>
public sealed class StaffSuspendedEvent : DomainEvent
{
    public Guid StaffId { get; }
    public Guid UserId { get; }
    public string Reason { get; }
    public string SuspendedBy { get; }

    public StaffSuspendedEvent(Guid staffId, Guid userId, string reason, string suspendedBy)
    {
        StaffId = staffId;
        UserId = userId;
        Reason = reason;
        SuspendedBy = suspendedBy;
    }
}
