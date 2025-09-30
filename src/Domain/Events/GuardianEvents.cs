using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

public sealed class GuardianCreatedEvent : DomainEvent
{
    public Guid GuardianId { get; }
    public string FullName { get; }
    public string? Email { get; }
    public string CreatedBy { get; }

    public GuardianCreatedEvent(Guid guardianId, string fullName, string? email, string createdBy)
    {
        GuardianId = guardianId;
        FullName = fullName;
        Email = email;
        CreatedBy = createdBy;
    }
}
