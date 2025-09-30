using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Event raised when school status changes
/// </summary>
public record SchoolStatusChangedEvent(
    Guid SchoolId,
    string OldStatus,
    string NewStatus,
    string ChangedBy) : DomainEvent;

/// <summary>
/// Event raised when a school is created
/// </summary>
public record SchoolCreatedEvent(
    Guid SchoolId,
    Guid DistrictId,
    string Name,
    string SchoolType,
    string CreatedBy) : DomainEvent;
