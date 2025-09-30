using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Retention policy for entity types per FERPA requirements
/// </summary>
public class RetentionPolicy : Entity
{
    public string EntityType { get; private set; } = string.Empty;
    public int RetentionYears { get; private set; }
    public int GracePeriodDays { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public string? OverrideReason { get; private set; }

    protected RetentionPolicy() { }

    public RetentionPolicy(
        string entityType,
        int retentionYears,
        int gracePeriodDays,
        DateTime effectiveDate,
        string createdBy,
        string? overrideReason = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required", nameof(entityType));
        if (retentionYears < 0)
            throw new ArgumentException("Retention years cannot be negative", nameof(retentionYears));

        SetAuditFields(createdBy);
        EntityType = entityType;
        RetentionYears = retentionYears;
        GracePeriodDays = gracePeriodDays;
        EffectiveDate = effectiveDate;
        OverrideReason = overrideReason;
    }
}
