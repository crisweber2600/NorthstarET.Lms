using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents an academic calendar for a school year with terms and closures
/// </summary>
public class AcademicCalendar : TenantScopedEntity
{
    /// <summary>
    /// The school year this calendar belongs to
    /// </summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>
    /// Academic terms within the school year
    /// </summary>
    private readonly List<Term> _terms = new();
    public IReadOnlyList<Term> Terms => _terms.AsReadOnly();

    /// <summary>
    /// School closures (holidays, breaks, etc.)
    /// </summary>
    private readonly List<Closure> _closures = new();
    public IReadOnlyList<Closure> Closures => _closures.AsReadOnly();

    /// <summary>
    /// Whether the calendar is archived (immutable)
    /// </summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// When the calendar was archived
    /// </summary>
    public DateTime? ArchivedAt { get; private set; }

    // EF Core constructor
    protected AcademicCalendar() { }

    /// <summary>
    /// Create a new academic calendar
    /// </summary>
    public AcademicCalendar(string tenantSlug, Guid schoolYearId, IEnumerable<Term> terms, string createdBy)
    {
        if (schoolYearId == Guid.Empty)
            throw new ArgumentException("School year ID cannot be empty", nameof(schoolYearId));

        var termList = terms?.ToList() ?? throw new ArgumentNullException(nameof(terms));
        
        if (termList.Count == 0)
            throw new ArgumentException("At least one term is required", nameof(terms));

        ValidateTerms(termList);

        SchoolYearId = schoolYearId;
        _terms.AddRange(termList);

        InitializeTenant(tenantSlug);
        SetAuditFields(createdBy);

        AddDomainEvent(new AcademicCalendarCreatedEvent(Id, SchoolYearId, _terms.Count)
        {
            TenantSlug = tenantSlug,
            TriggeredBy = createdBy
        });
    }

    /// <summary>
    /// Add school closures to the calendar
    /// </summary>
    public void AddClosures(IEnumerable<Closure> closures, string updatedBy)
    {
        if (IsArchived)
            throw new InvalidOperationException("Cannot modify archived calendar");

        var closureList = closures?.ToList() ?? throw new ArgumentNullException(nameof(closures));
        
        if (closureList.Count == 0)
            return;

        _closures.AddRange(closureList);
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new CalendarClosuresAddedEvent(Id, closureList.Count)
        {
            TenantSlug = TenantSlug,
            TriggeredBy = updatedBy
        });
    }

    /// <summary>
    /// Archive the calendar (make it immutable)
    /// </summary>
    public void Archive(string updatedBy)
    {
        if (IsArchived)
            throw new InvalidOperationException("Calendar is already archived");

        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new AcademicCalendarArchivedEvent(Id, ArchivedAt.Value)
        {
            TenantSlug = TenantSlug,
            TriggeredBy = updatedBy
        });
    }

    /// <summary>
    /// Calculate instructional days for a specific term, excluding closures and weekends
    /// </summary>
    public int CalculateInstructionalDays(Guid termId)
    {
        var term = _terms.FirstOrDefault(t => t.Id == termId);
        if (term == null)
            throw new ArgumentException($"Term with ID {termId} not found", nameof(termId));

        return CalculateInstructionalDaysForPeriod(term.StartDate, term.EndDate);
    }

    /// <summary>
    /// Calculate total instructional days for the entire school year
    /// </summary>
    public int CalculateTotalInstructionalDays()
    {
        if (_terms.Count == 0)
            return 0;

        var startDate = _terms.Min(t => t.StartDate);
        var endDate = _terms.Max(t => t.EndDate);

        return CalculateInstructionalDaysForPeriod(startDate, endDate);
    }

    /// <summary>
    /// Check if a date is an instructional day (not weekend, not closure, within term)
    /// </summary>
    public bool IsInstructionalDay(DateTime date)
    {
        var checkDate = date.Date;

        // Check if it's a weekend
        if (checkDate.DayOfWeek == DayOfWeek.Saturday || checkDate.DayOfWeek == DayOfWeek.Sunday)
            return false;

        // Check if it's within any term
        if (!_terms.Any(t => t.ContainsDate(checkDate)))
            return false;

        // Check if it's a closure
        if (_closures.Any(c => c.ContainsDate(checkDate)))
            return false;

        return true;
    }

    /// <summary>
    /// Validate calendar completeness
    /// </summary>
    public bool IsComplete()
    {
        // Must have at least one term
        if (_terms.Count == 0)
            return false;

        // Terms should not overlap
        for (int i = 0; i < _terms.Count; i++)
        {
            for (int j = i + 1; j < _terms.Count; j++)
            {
                if (_terms[i].OverlapsWith(_terms[j]))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get all dates that fall within terms but are not instructional days
    /// </summary>
    public IEnumerable<DateTime> GetNonInstructionalDates()
    {
        if (_terms.Count == 0)
            yield break;

        var startDate = _terms.Min(t => t.StartDate);
        var endDate = _terms.Max(t => t.EndDate);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // If it's within a term but not an instructional day
            if (_terms.Any(t => t.ContainsDate(date)) && !IsInstructionalDay(date))
            {
                yield return date;
            }
        }
    }

    private void ValidateTerms(List<Term> terms)
    {
        // Check for overlapping terms
        for (int i = 0; i < terms.Count; i++)
        {
            for (int j = i + 1; j < terms.Count; j++)
            {
                if (terms[i].OverlapsWith(terms[j]))
                    throw new ArgumentException($"Terms '{terms[i].Name}' and '{terms[j].Name}' overlap");
            }
        }
    }

    private int CalculateInstructionalDaysForPeriod(DateTime startDate, DateTime endDate)
    {
        int instructionalDays = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (IsInstructionalDay(date))
                instructionalDays++;
        }

        return instructionalDays;
    }
}