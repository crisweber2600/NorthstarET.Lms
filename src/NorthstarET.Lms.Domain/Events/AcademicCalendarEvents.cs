namespace NorthstarET.Lms.Domain.Events;

public record AcademicCalendarCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid CalendarId { get; }
    public Guid SchoolYearId { get; }

    public AcademicCalendarCreatedEvent(Guid calendarId, Guid schoolYearId)
    {
        CalendarId = calendarId;
        SchoolYearId = schoolYearId;
    }
}

public record TermAddedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid CalendarId { get; }
    public Guid TermId { get; }
    public string TermName { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int SequenceNumber { get; }

    public TermAddedEvent(Guid calendarId, Guid termId, string termName, DateTime startDate, DateTime endDate, int sequenceNumber)
    {
        CalendarId = calendarId;
        TermId = termId;
        TermName = termName;
        StartDate = startDate;
        EndDate = endDate;
        SequenceNumber = sequenceNumber;
    }
}

public record TermRemovedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid CalendarId { get; }
    public Guid TermId { get; }
    public string TermName { get; }

    public TermRemovedEvent(Guid calendarId, Guid termId, string termName)
    {
        CalendarId = calendarId;
        TermId = termId;
        TermName = termName;
    }
}

public record ClosureAddedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid CalendarId { get; }
    public Guid ClosureId { get; }
    public string ClosureName { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public bool IsRecurring { get; }

    public ClosureAddedEvent(Guid calendarId, Guid closureId, string closureName, DateTime startDate, DateTime endDate, bool isRecurring)
    {
        CalendarId = calendarId;
        ClosureId = closureId;
        ClosureName = closureName;
        StartDate = startDate;
        EndDate = endDate;
        IsRecurring = isRecurring;
    }
}

public record AcademicCalendarCopiedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid SourceCalendarId { get; }
    public Guid NewCalendarId { get; }
    public Guid SourceSchoolYearId { get; }
    public Guid TargetSchoolYearId { get; }
    public int DayOffset { get; }
    public string CopiedByUserId { get; }

    public AcademicCalendarCopiedEvent(Guid sourceCalendarId, Guid newCalendarId, Guid sourceSchoolYearId, Guid targetSchoolYearId, int dayOffset, string copiedByUserId)
    {
        SourceCalendarId = sourceCalendarId;
        NewCalendarId = newCalendarId;
        SourceSchoolYearId = sourceSchoolYearId;
        TargetSchoolYearId = targetSchoolYearId;
        DayOffset = dayOffset;
        CopiedByUserId = copiedByUserId;
    }
}