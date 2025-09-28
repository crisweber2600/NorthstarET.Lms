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

// Staff Events
public record StaffHiredEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public string EmployeeNumber { get; }
    public string FullName { get; }
    public DateTime HireDate { get; }

    public StaffHiredEvent(Guid userId, string employeeNumber, string fullName, DateTime hireDate)
    {
        UserId = userId;
        EmployeeNumber = employeeNumber;
        FullName = fullName;
        HireDate = hireDate;
    }
}

public record StaffEmailUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public string OldEmail { get; }
    public string NewEmail { get; }
    public string UpdatedByUserId { get; }

    public StaffEmailUpdatedEvent(Guid userId, string oldEmail, string newEmail, string updatedByUserId)
    {
        UserId = userId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
        UpdatedByUserId = updatedByUserId;
    }
}

public record StaffSpecializationAddedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public string Specialization { get; }
    public string AddedByUserId { get; }

    public StaffSpecializationAddedEvent(Guid userId, string specialization, string addedByUserId)
    {
        UserId = userId;
        Specialization = specialization;
        AddedByUserId = addedByUserId;
    }
}

public record StaffSpecializationRemovedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public string Specialization { get; }
    public string RemovedByUserId { get; }

    public StaffSpecializationRemovedEvent(Guid userId, string specialization, string removedByUserId)
    {
        UserId = userId;
        Specialization = specialization;
        RemovedByUserId = removedByUserId;
    }
}

public record StaffSuspendedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public string Reason { get; }
    public DateTime SuspensionDate { get; }
    public string SuspendedByUserId { get; }

    public StaffSuspendedEvent(Guid userId, string reason, DateTime suspensionDate, string suspendedByUserId)
    {
        UserId = userId;
        Reason = reason;
        SuspensionDate = suspensionDate;
        SuspendedByUserId = suspendedByUserId;
    }
}

public record StaffReinstatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public DateTime ReinstateDate { get; }
    public string ReinstatedByUserId { get; }

    public StaffReinstatedEvent(Guid userId, DateTime reinstateDate, string reinstatedByUserId)
    {
        UserId = userId;
        ReinstateDate = reinstateDate;
        ReinstatedByUserId = reinstatedByUserId;
    }
}

public record StaffTerminatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid UserId { get; }
    public DateTime TerminationDate { get; }
    public string Reason { get; }
    public string TerminatedByUserId { get; }

    public StaffTerminatedEvent(Guid userId, DateTime terminationDate, string reason, string terminatedByUserId)
    {
        UserId = userId;
        TerminationDate = terminationDate;
        Reason = reason;
        TerminatedByUserId = terminatedByUserId;
    }
}