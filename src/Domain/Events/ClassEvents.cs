using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Event raised when a class is created
/// </summary>
public record ClassCreatedEvent(
    Guid ClassId,
    Guid SchoolId,
    Guid SchoolYearId,
    string Name,
    string Code,
    string CreatedBy) : DomainEvent;

/// <summary>
/// Event raised when class status changes
/// </summary>
public record ClassStatusChangedEvent(
    Guid ClassId,
    string OldStatus,
    string NewStatus,
    string ChangedBy) : DomainEvent;
