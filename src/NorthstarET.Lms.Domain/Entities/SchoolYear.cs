using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public class SchoolYear : TenantScopedEntity
{
    // Private constructor for EF Core
    private SchoolYear() { }

    public SchoolYear(string name, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("School year name is required", nameof(name));
            
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date", nameof(endDate));

        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = SchoolYearStatus.Planning;
        IsReadOnly = false;
    }

    public string Name { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public SchoolYearStatus Status { get; private set; }
    public bool IsReadOnly { get; private set; }

    public int DurationInDays => (EndDate - StartDate).Days;

    public bool IsCurrentYear(DateTime testDate)
    {
        return testDate >= StartDate && testDate <= EndDate;
    }

    public void Activate(string activatedByUserId)
    {
        if (Status == SchoolYearStatus.Active)
            throw new InvalidOperationException("School year is already active");
            
        Status = SchoolYearStatus.Active;
        MarkAsModified();
        AddDomainEvent(new SchoolYearActivatedEvent(Id, Name, activatedByUserId));
    }

    public void Archive(string archivedByUserId)
    {
        if (Status != SchoolYearStatus.Active)
            throw new InvalidOperationException("Only active school years can be archived");
            
        Status = SchoolYearStatus.Archived;
        IsReadOnly = true;
        MarkAsModified();
        AddDomainEvent(new SchoolYearArchivedEvent(Id, Name, archivedByUserId));
    }

    public void UpdateDateRange(DateTime newStartDate, DateTime newEndDate, string updatedByUserId)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot modify archived school years are read-only");
            
        if (newEndDate <= newStartDate)
            throw new ArgumentException("End date must be after start date", nameof(newEndDate));

        StartDate = newStartDate;
        EndDate = newEndDate;
        MarkAsModified();
    }
}