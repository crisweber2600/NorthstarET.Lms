using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public abstract class TenantScopedEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string TenantId { get; protected set; } = string.Empty;
    public DateTime CreatedDate { get; protected set; } = DateTime.UtcNow;
    public DateTime? LastModifiedDate { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void MarkAsModified()
    {
        LastModifiedDate = DateTime.UtcNow;
    }
}