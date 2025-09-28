namespace NorthstarET.Lms.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
    Guid EventId { get; }
}