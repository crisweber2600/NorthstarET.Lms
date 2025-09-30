using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Raised when a school year is created
/// </summary>
public sealed class SchoolYearCreatedEvent : DomainEvent
{
    public Guid SchoolYearId { get; }
    public string Label { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public SchoolYearCreatedEvent(Guid schoolYearId, string label, DateTime startDate, DateTime endDate)
    {
        SchoolYearId = schoolYearId;
        Label = label;
        StartDate = startDate;
        EndDate = endDate;
    }
}

/// <summary>
/// Raised when a school year is archived
/// </summary>
public sealed class SchoolYearArchivedEvent : DomainEvent
{
    public Guid SchoolYearId { get; }
    public DateTime ArchivedAt { get; }

    public SchoolYearArchivedEvent(Guid schoolYearId, DateTime archivedAt)
    {
        SchoolYearId = schoolYearId;
        ArchivedAt = archivedAt;
    }
}