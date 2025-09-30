namespace NorthstarET.Lms.Domain.Common;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Unique identifier for this event
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when this event occurred
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracking related events
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// User who triggered this event
    /// </summary>
    public string? TriggeredBy { get; init; }

    /// <summary>
    /// Tenant context for this event
    /// </summary>
    public string? TenantSlug { get; init; }
}