using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents an academic school year with temporal scoping for all academic data
/// </summary>
public class SchoolYear : TenantScopedEntity
{
    /// <summary>
    /// Human-readable label for the school year (e.g., "2024-2025")
    /// </summary>
    public string Label { get; private set; }

    /// <summary>
    /// Start date of the school year
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// End date of the school year
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Current status of the school year
    /// </summary>
    public SchoolYearStatus Status { get; private set; }

    /// <summary>
    /// When the school year was archived (if applicable)
    /// </summary>
    public DateTime? ArchivedAt { get; private set; }

    // EF Core constructor
    protected SchoolYear()
    {
        Label = string.Empty;
    }

    /// <summary>
    /// Create a new school year
    /// </summary>
    public SchoolYear(string tenantSlug, string label, DateTime startDate, DateTime endDate, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label cannot be null or empty", nameof(label));

        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date");

        if (endDate.Subtract(startDate).TotalDays < 150)
            throw new ArgumentException("School year must be at least 150 days long");

        Label = label.Trim();
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        Status = SchoolYearStatus.Draft;

        InitializeTenant(tenantSlug);
        SetAuditFields(createdBy);

        AddDomainEvent(new SchoolYearCreatedEvent(Id, Label, StartDate, EndDate)
        {
            TenantSlug = tenantSlug,
            TriggeredBy = createdBy
        });
    }

    /// <summary>
    /// Activate the school year
    /// </summary>
    public void Activate(string updatedBy)
    {
        if (Status != SchoolYearStatus.Draft)
            throw new InvalidOperationException("Only draft school years can be activated");

        Status = SchoolYearStatus.Active;
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Archive the school year (locks all child data)
    /// </summary>
    public void Archive(string updatedBy)
    {
        if (Status == SchoolYearStatus.Archived)
            throw new InvalidOperationException("School year is already archived");

        Status = SchoolYearStatus.Archived;
        ArchivedAt = DateTime.UtcNow;
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new SchoolYearArchivedEvent(Id, ArchivedAt.Value)
        {
            TenantSlug = TenantSlug,
            TriggeredBy = updatedBy
        });
    }

    /// <summary>
    /// Check if the school year is currently active
    /// </summary>
    public bool IsCurrentlyActive()
    {
        var now = DateTime.Now.Date;
        return Status == SchoolYearStatus.Active && 
               StartDate <= now && 
               EndDate >= now;
    }

    /// <summary>
    /// Check if modifications are allowed
    /// </summary>
    public bool AllowsModifications()
    {
        return Status != SchoolYearStatus.Archived;
    }

    /// <summary>
    /// Validate that this school year doesn't overlap with another
    /// </summary>
    public bool OverlapsWith(SchoolYear other)
    {
        if (other == null) return false;
        
        return StartDate < other.EndDate && EndDate > other.StartDate;
    }

    /// <summary>
    /// Check if a date falls within this school year
    /// </summary>
    public bool ContainsDate(DateTime date)
    {
        var checkDate = date.Date;
        return checkDate >= StartDate && checkDate <= EndDate;
    }
}

/// <summary>
/// School year status enumeration
/// </summary>
public enum SchoolYearStatus
{
    Draft,
    Active,
    Archived
}