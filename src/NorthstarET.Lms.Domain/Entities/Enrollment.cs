using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public class Enrollment : TenantScopedEntity
{
    // Private constructor for EF Core
    private Enrollment() { }

    public Enrollment(
        Guid studentId,
        Guid classId,
        Guid schoolYearId,
        GradeLevel gradeLevel,
        DateTime enrollmentDate)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId cannot be empty", nameof(studentId));
        
        if (classId == Guid.Empty)
            throw new ArgumentException("ClassId cannot be empty", nameof(classId));
        
        if (schoolYearId == Guid.Empty)
            throw new ArgumentException("SchoolYearId cannot be empty", nameof(schoolYearId));
        
        if (enrollmentDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Enrollment date cannot be in the future", nameof(enrollmentDate));

        StudentId = studentId;
        ClassId = classId;
        SchoolYearId = schoolYearId;
        GradeLevel = gradeLevel;
        EnrollmentDate = enrollmentDate;
        Status = EnrollmentStatus.Active;
        
        AddDomainEvent(new StudentEnrolledEvent(studentId, classId, schoolYearId, gradeLevel, enrollmentDate));
    }

    public Guid StudentId { get; private set; }
    public Guid ClassId { get; private set; }
    public Guid SchoolYearId { get; private set; }
    public GradeLevel GradeLevel { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrollmentDate { get; private set; }
    public DateTime? WithdrawalDate { get; private set; }
    public string? WithdrawalReason { get; private set; }

    // Navigation properties
    public Student Student { get; private set; } = null!;
    public Class Class { get; private set; } = null!;
    public SchoolYear SchoolYear { get; private set; } = null!;

    public bool IsActive => Status == EnrollmentStatus.Active;

    public void Withdraw(DateTime withdrawalDate, string withdrawalReason, string withdrawnByUserId)
    {
        if (Status == EnrollmentStatus.Withdrawn)
            throw new InvalidOperationException("Enrollment is already withdrawn");
        
        if (withdrawalDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Withdrawal date cannot be in the future", nameof(withdrawalDate));
        
        if (withdrawalDate < EnrollmentDate)
            throw new ArgumentException("Withdrawal date cannot be before enrollment date", nameof(withdrawalDate));
        
        if (string.IsNullOrWhiteSpace(withdrawalReason))
            throw new ArgumentException("Withdrawal reason is required", nameof(withdrawalReason));

        Status = EnrollmentStatus.Withdrawn;
        WithdrawalDate = withdrawalDate;
        WithdrawalReason = withdrawalReason;
        MarkAsModified();
        
        AddDomainEvent(new StudentWithdrawnEvent(StudentId, withdrawalDate, withdrawalReason));
    }

    public void Transfer(Guid newClassId, DateTime transferDate, string transferReason, string transferredByUserId)
    {
        if (Status == EnrollmentStatus.Withdrawn)
            throw new InvalidOperationException("Cannot transfer withdrawn enrollment");
        
        if (newClassId == ClassId)
            throw new ArgumentException("Cannot transfer to the same class", nameof(newClassId));
        
        if (transferDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Transfer date cannot be in the future", nameof(transferDate));
        
        if (transferDate < EnrollmentDate)
            throw new ArgumentException("Transfer date cannot be before enrollment date", nameof(transferDate));
        
        if (string.IsNullOrWhiteSpace(transferReason))
            throw new ArgumentException("Transfer reason is required", nameof(transferReason));

        var oldClassId = ClassId;
        ClassId = newClassId;
        Status = EnrollmentStatus.Transferred;
        MarkAsModified();
        
        AddDomainEvent(new StudentTransferredEvent(StudentId, oldClassId, newClassId, transferDate, transferReason, transferredByUserId));
    }

    public void Graduate(DateTime graduationDate, string graduatedByUserId)
    {
        if (Status == EnrollmentStatus.Withdrawn)
            throw new InvalidOperationException("Cannot graduate withdrawn enrollment");
        
        if (graduationDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Graduation date cannot be in the future", nameof(graduationDate));
        
        if (graduationDate < EnrollmentDate)
            throw new ArgumentException("Graduation date cannot be before enrollment date", nameof(graduationDate));

        Status = EnrollmentStatus.Graduated;
        WithdrawalDate = graduationDate;
        WithdrawalReason = "Graduated";
        MarkAsModified();
        
        AddDomainEvent(new StudentGraduatedEvent(StudentId, graduationDate, graduatedByUserId));
    }

    public void Reinstate(DateTime reinstateDate, string reinstateReason, string reinstatedByUserId)
    {
        if (Status == EnrollmentStatus.Active)
            throw new InvalidOperationException("Cannot reinstate active enrollment");
        
        if (Status == EnrollmentStatus.Graduated)
            throw new InvalidOperationException("Cannot reinstate graduated enrollment");
        
        if (reinstateDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Reinstate date cannot be in the future", nameof(reinstateDate));
        
        if (string.IsNullOrWhiteSpace(reinstateReason))
            throw new ArgumentException("Reinstate reason is required", nameof(reinstateReason));

        Status = EnrollmentStatus.Active;
        WithdrawalDate = null;
        WithdrawalReason = null;
        MarkAsModified();
        
        AddDomainEvent(new StudentReinstatedEvent(StudentId, reinstateDate, reinstateReason, reinstatedByUserId));
    }

    public void UpdateGradeLevel(GradeLevel newGradeLevel, string updatedByUserId)
    {
        if (GradeLevel == newGradeLevel)
            return; // No change, don't add event
        
        var oldGradeLevel = GradeLevel;
        GradeLevel = newGradeLevel;
        MarkAsModified();
        
        AddDomainEvent(new StudentGradeLevelUpdatedEvent(StudentId, oldGradeLevel, newGradeLevel, updatedByUserId));
    }
}