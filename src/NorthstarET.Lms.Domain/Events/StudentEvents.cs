using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Domain.Events;

public record StudentGradeUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid StudentId { get; }
    public GradeLevel OldGradeLevel { get; }
    public GradeLevel NewGradeLevel { get; }
    public string UpdatedByUserId { get; }

    public StudentGradeUpdatedEvent(Guid studentId, GradeLevel oldGradeLevel, GradeLevel newGradeLevel, string updatedByUserId)
    {
        StudentId = studentId;
        OldGradeLevel = oldGradeLevel;
        NewGradeLevel = newGradeLevel;
        UpdatedByUserId = updatedByUserId;
    }
}

public record StudentWithdrawnEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid StudentId { get; }
    public DateTime WithdrawalDate { get; }
    public string Reason { get; }

    public StudentWithdrawnEvent(Guid studentId, DateTime withdrawalDate, string reason)
    {
        StudentId = studentId;
        WithdrawalDate = withdrawalDate;
        Reason = reason;
    }
}