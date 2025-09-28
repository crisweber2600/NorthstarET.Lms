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

public record StudentEnrolledEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid StudentId { get; }
    public Guid ClassId { get; }
    public Guid SchoolYearId { get; }
    public GradeLevel GradeLevel { get; }
    public DateTime EnrollmentDate { get; }

    public StudentEnrolledEvent(Guid studentId, Guid classId, Guid schoolYearId, GradeLevel gradeLevel, DateTime enrollmentDate)
    {
        StudentId = studentId;
        ClassId = classId;
        SchoolYearId = schoolYearId;
        GradeLevel = gradeLevel;
        EnrollmentDate = enrollmentDate;
    }
}

public record StudentTransferredEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid StudentId { get; }
    public Guid FromClassId { get; }
    public Guid ToClassId { get; }
    public DateTime TransferDate { get; }
    public string Reason { get; }
    public string TransferredByUserId { get; }

    public StudentTransferredEvent(Guid studentId, Guid fromClassId, Guid toClassId, DateTime transferDate, string reason, string transferredByUserId)
    {
        StudentId = studentId;
        FromClassId = fromClassId;
        ToClassId = toClassId;
        TransferDate = transferDate;
        Reason = reason;
        TransferredByUserId = transferredByUserId;
    }
}

public record StudentGraduatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid StudentId { get; }
    public DateTime GraduationDate { get; }
    public string GraduatedByUserId { get; }

    public StudentGraduatedEvent(Guid studentId, DateTime graduationDate, string graduatedByUserId)
    {
        StudentId = studentId;
        GraduationDate = graduationDate;
        GraduatedByUserId = graduatedByUserId;
    }
}

public record StudentReinstatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid StudentId { get; }
    public DateTime ReinstateDate { get; }
    public string Reason { get; }
    public string ReinstatedByUserId { get; }

    public StudentReinstatedEvent(Guid studentId, DateTime reinstateDate, string reason, string reinstatedByUserId)
    {
        StudentId = studentId;
        ReinstateDate = reinstateDate;
        Reason = reason;
        ReinstatedByUserId = reinstatedByUserId;
    }
}

public record StudentGradeLevelUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid StudentId { get; }
    public GradeLevel OldGradeLevel { get; }
    public GradeLevel NewGradeLevel { get; }
    public string UpdatedByUserId { get; }

    public StudentGradeLevelUpdatedEvent(Guid studentId, GradeLevel oldGradeLevel, GradeLevel newGradeLevel, string updatedByUserId)
    {
        StudentId = studentId;
        OldGradeLevel = oldGradeLevel;
        NewGradeLevel = newGradeLevel;
        UpdatedByUserId = updatedByUserId;
    }
}