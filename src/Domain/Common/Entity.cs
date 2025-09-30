namespace NorthstarET.Lms.Domain.Common;

/// <summary>
/// Base class for all domain entities that provides identity and domain event handling
/// </summary>
public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Unique identifier for this entity
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when this entity was created
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this entity
    /// </summary>
    public string CreatedBy { get; protected set; } = string.Empty;

    /// <summary>
    /// Timestamp when this entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// User who last updated this entity
    /// </summary>
    public string? UpdatedBy { get; protected set; }

    /// <summary>
    /// Optimistic concurrency token
    /// </summary>
    public byte[] RowVersion { get; protected set; } = Array.Empty<byte>();

    /// <summary>
    /// Domain events raised by this entity
    /// </summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Add a domain event to be published
    /// </summary>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Remove a domain event
    /// </summary>
    public void RemoveDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clear all domain events
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Update audit fields for entity modification
    /// </summary>
    protected void UpdateAuditFields(string updatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Set audit fields for entity creation
    /// </summary>
    protected void SetAuditFields(string createdBy)
    {
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !Equals(left, right);
    }
}