using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Raised when an academic calendar is created
/// </summary>
public sealed class AcademicCalendarCreatedEvent : DomainEvent
{
    public Guid AcademicCalendarId { get; }
    public Guid SchoolYearId { get; }
    public int TermCount { get; }

    public AcademicCalendarCreatedEvent(Guid academicCalendarId, Guid schoolYearId, int termCount)
    {
        AcademicCalendarId = academicCalendarId;
        SchoolYearId = schoolYearId;
        TermCount = termCount;
    }
}

/// <summary>
/// Raised when closures are added to a calendar
/// </summary>
public sealed class CalendarClosuresAddedEvent : DomainEvent
{
    public Guid AcademicCalendarId { get; }
    public int ClosureCount { get; }
    public DateTime AddedAt { get; }

    public CalendarClosuresAddedEvent(Guid academicCalendarId, int closureCount)
    {
        AcademicCalendarId = academicCalendarId;
        ClosureCount = closureCount;
        AddedAt = OccurredAt;
    }
}

/// <summary>
/// Raised when an academic calendar is archived
/// </summary>
public sealed class AcademicCalendarArchivedEvent : DomainEvent
{
    public Guid AcademicCalendarId { get; }
    public DateTime ArchivedAt { get; }

    public AcademicCalendarArchivedEvent(Guid academicCalendarId, DateTime archivedAt)
    {
        AcademicCalendarId = academicCalendarId;
        ArchivedAt = archivedAt;
    }
}