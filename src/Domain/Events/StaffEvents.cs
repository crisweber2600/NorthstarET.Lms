using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Event raised when a staff member is created
/// </summary>
public record StaffCreatedEvent(
    Guid StaffId,
    Guid UserId,
    string FullName,
    string Email,
    string CreatedBy) : DomainEvent;

/// <summary>
/// Event raised when staff employment status changes
/// </summary>
public record StaffEmploymentStatusChangedEvent(
    Guid StaffId,
    Guid UserId,
    string OldStatus,
    string NewStatus,
    string ChangedBy) : DomainEvent;

/// <summary>
/// Event raised when staff is suspended
/// </summary>
public record StaffSuspendedEvent(
    Guid StaffId,
    Guid UserId,
    string Reason,
    string SuspendedBy) : DomainEvent;
