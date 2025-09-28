using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public class AcademicCalendar : TenantScopedEntity
{
    private readonly List<Term> _terms = new();
    private readonly List<Closure> _closures = new();

    // Private constructor for EF Core
    private AcademicCalendar() { }

    public AcademicCalendar(Guid schoolYearId)
    {
        if (schoolYearId == Guid.Empty)
            throw new ArgumentException("SchoolYearId cannot be empty", nameof(schoolYearId));

        SchoolYearId = schoolYearId;
        
        AddDomainEvent(new AcademicCalendarCreatedEvent(Id, schoolYearId));
    }

    public Guid SchoolYearId { get; private set; }
    public SchoolYear SchoolYear { get; private set; } = null!;

    public IReadOnlyCollection<Term> Terms => _terms.AsReadOnly();
    public IReadOnlyCollection<Closure> Closures => _closures.AsReadOnly();

    public void AddTerm(Term term)
    {
        if (term == null)
            throw new ArgumentNullException(nameof(term));

        // Check for sequence number conflicts
        if (_terms.Any(t => t.SequenceNumber == term.SequenceNumber))
            throw new InvalidOperationException($"Term with sequence number {term.SequenceNumber} already exists");

        // Check for date overlaps
        if (_terms.Any(t => DatesOverlap(t.StartDate, t.EndDate, term.StartDate, term.EndDate)))
            throw new InvalidOperationException("Term dates overlap with existing term");

        _terms.Add(term);
        MarkAsModified();
        
        AddDomainEvent(new TermAddedEvent(Id, term.Id, term.Name, term.StartDate, term.EndDate, term.SequenceNumber));
    }

    public void RemoveTerm(Guid termId)
    {
        var term = _terms.FirstOrDefault(t => t.Id == termId);
        if (term == null)
            throw new ArgumentException("Term not found", nameof(termId));

        _terms.Remove(term);
        MarkAsModified();
        
        AddDomainEvent(new TermRemovedEvent(Id, termId, term.Name));
    }

    public void AddClosure(Closure closure)
    {
        if (closure == null)
            throw new ArgumentNullException(nameof(closure));

        _closures.Add(closure);
        MarkAsModified();
        
        AddDomainEvent(new ClosureAddedEvent(Id, closure.Id, closure.Name, closure.StartDate, closure.EndDate, closure.IsRecurring));
    }

    public bool ValidateCompleteness()
    {
        // Calendar is complete if it has at least one term
        return _terms.Count > 0;
    }

    public int GetInstructionalDays(Guid termId)
    {
        var term = _terms.FirstOrDefault(t => t.Id == termId);
        if (term == null)
            throw new ArgumentException("Term not found", nameof(termId));

        var businessDays = CalculateBusinessDays(term.StartDate, term.EndDate);
        
        // Subtract closure days that overlap with the term
        var closureDays = _closures
            .Where(c => DatesOverlap(c.StartDate, c.EndDate, term.StartDate, term.EndDate))
            .Sum(c => CalculateBusinessDays(
                c.StartDate > term.StartDate ? c.StartDate : term.StartDate, 
                c.EndDate < term.EndDate ? c.EndDate : term.EndDate));

        return Math.Max(0, businessDays - closureDays);
    }

    public AcademicCalendar CopyToSchoolYear(Guid targetSchoolYearId, int dayOffset, string copiedByUserId)
    {
        if (targetSchoolYearId == Guid.Empty)
            throw new ArgumentException("TargetSchoolYearId cannot be empty", nameof(targetSchoolYearId));
        
        if (string.IsNullOrWhiteSpace(copiedByUserId))
            throw new ArgumentException("CopiedByUserId is required", nameof(copiedByUserId));

        var newCalendar = new AcademicCalendar(targetSchoolYearId);

        // Copy terms with adjusted dates
        foreach (var term in _terms)
        {
            var newTerm = new Term(
                term.Name,
                term.StartDate.AddDays(dayOffset),
                term.EndDate.AddDays(dayOffset),
                term.SequenceNumber);
            
            newCalendar._terms.Add(newTerm);
        }

        // Copy non-date-specific closures
        foreach (var closure in _closures.Where(c => !c.IsRecurring))
        {
            var newClosure = new Closure(
                closure.Name,
                closure.StartDate.AddDays(dayOffset),
                closure.EndDate.AddDays(dayOffset),
                closure.IsRecurring);
            
            newCalendar._closures.Add(newClosure);
        }

        newCalendar.AddDomainEvent(new AcademicCalendarCopiedEvent(
            Id, newCalendar.Id, SchoolYearId, targetSchoolYearId, dayOffset, copiedByUserId));

        return newCalendar;
    }

    private static bool DatesOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        return start1 <= end2 && start2 <= end1;
    }

    private static int CalculateBusinessDays(DateTime startDate, DateTime endDate)
    {
        var businessDays = 0;
        var current = startDate;

        while (current <= endDate)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                businessDays++;
            }
            current = current.AddDays(1);
        }

        return businessDays;
    }
}

public class Term : TenantScopedEntity
{
    // Private constructor for EF Core
    private Term() { }

    public Term(string name, DateTime startDate, DateTime endDate, int sequenceNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Term name is required", nameof(name));
        
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date");
        
        if (sequenceNumber <= 0)
            throw new ArgumentException("Sequence number must be positive", nameof(sequenceNumber));

        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        SequenceNumber = sequenceNumber;
    }

    public string Name { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int SequenceNumber { get; private set; }
}

public class Closure : TenantScopedEntity
{
    // Private constructor for EF Core
    private Closure() { }

    public Closure(string name, DateTime startDate, DateTime endDate, bool isRecurring)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Closure name is required", nameof(name));
        
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date");

        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        IsRecurring = isRecurring;
    }

    public string Name { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsRecurring { get; private set; }
}