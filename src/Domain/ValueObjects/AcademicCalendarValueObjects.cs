using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.ValueObjects;

/// <summary>
/// Represents a term within an academic calendar
/// </summary>
public sealed class Term : ValueObject
{
    public Guid Id { get; }
    public string Name { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public Term(string name, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Term name cannot be null or empty", nameof(name));

        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date");

        Id = Guid.NewGuid();
        Name = name.Trim();
        StartDate = startDate.Date;
        EndDate = endDate.Date;
    }

    /// <summary>
    /// Check if this term overlaps with another term
    /// </summary>
    public bool OverlapsWith(Term other)
    {
        if (other == null) return false;
        return StartDate < other.EndDate && EndDate > other.StartDate;
    }

    /// <summary>
    /// Check if a date falls within this term
    /// </summary>
    public bool ContainsDate(DateTime date)
    {
        var checkDate = date.Date;
        return checkDate >= StartDate && checkDate <= EndDate;
    }

    /// <summary>
    /// Get the duration of this term in days
    /// </summary>
    public int DurationInDays => (EndDate - StartDate).Days + 1;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
    }

    public override string ToString() => $"{Name} ({StartDate:MM/dd/yyyy} - {EndDate:MM/dd/yyyy})";
}

/// <summary>
/// Represents a closure (holiday, break, etc.) within an academic calendar
/// </summary>
public sealed class Closure : ValueObject
{
    public Guid Id { get; }
    public string Name { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string Reason { get; }

    public Closure(string name, DateTime startDate, DateTime endDate, string reason)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Closure name cannot be null or empty", nameof(name));

        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date");

        Id = Guid.NewGuid();
        Name = name.Trim();
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        Reason = reason?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Check if a date falls within this closure
    /// </summary>
    public bool ContainsDate(DateTime date)
    {
        var checkDate = date.Date;
        return checkDate >= StartDate && checkDate <= EndDate;
    }

    /// <summary>
    /// Get the duration of this closure in days
    /// </summary>
    public int DurationInDays => (EndDate - StartDate).Days + 1;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
    }

    public override string ToString() => $"{Name} ({StartDate:MM/dd/yyyy} - {EndDate:MM/dd/yyyy}): {Reason}";
}