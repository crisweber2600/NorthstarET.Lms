using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a student enrollment in a class for a school year
/// </summary>
public class Enrollment : TenantScopedEntity
{
    /// <summary>
    /// Student ID
    /// </summary>
    public Guid StudentId { get; private set; }

    /// <summary>
    /// Class ID
    /// </summary>
    public Guid ClassId { get; private set; }

    /// <summary>
    /// School year ID
    /// </summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>
    /// Enrollment status (Active, Withdrawn, Transferred)
    /// </summary>
    public string EnrollmentStatus { get; private set; } = "Active";

    /// <summary>
    /// Entry date (when student enrolled in class)
    /// </summary>
    public DateTime EntryDate { get; private set; }

    /// <summary>
    /// Exit date (when student left class)
    /// </summary>
    public DateTime? ExitDate { get; private set; }

    /// <summary>
    /// Exit reason
    /// </summary>
    public string? ExitReason { get; private set; }

    // EF Core constructor
    protected Enrollment() { }

    /// <summary>
    /// Create a new enrollment
    /// </summary>
    public Enrollment(
        string tenantSlug,
        Guid studentId,
        Guid classId,
        Guid schoolYearId,
        DateTime entryDate,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug is required", nameof(tenantSlug));
        if (studentId == Guid.Empty)
            throw new ArgumentException("Student ID cannot be empty", nameof(studentId));
        if (classId == Guid.Empty)
            throw new ArgumentException("Class ID cannot be empty", nameof(classId));
        if (schoolYearId == Guid.Empty)
            throw new ArgumentException("School year ID cannot be empty", nameof(schoolYearId));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by is required", nameof(createdBy));

        InitializeTenant(tenantSlug);
        SetAuditFields(createdBy);
        StudentId = studentId;
        ClassId = classId;
        SchoolYearId = schoolYearId;
        EntryDate = entryDate;
        EnrollmentStatus = "Active";

        AddDomainEvent(new EnrollmentCreatedEvent(Id, StudentId, ClassId, SchoolYearId, EntryDate, createdBy));
    }

    /// <summary>
    /// Withdraw enrollment
    /// </summary>
    public void Withdraw(DateTime exitDate, string reason, string updatedBy)
    {
        if (EnrollmentStatus != "Active")
            throw new InvalidOperationException("Can only withdraw active enrollments");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Exit reason is required", nameof(reason));
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));
        if (exitDate < EntryDate)
            throw new ArgumentException("Exit date cannot be before entry date", nameof(exitDate));

        EnrollmentStatus = "Withdrawn";
        ExitDate = exitDate;
        ExitReason = reason;
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new EnrollmentWithdrawnEvent(Id, StudentId, ClassId, ExitDate.Value, reason, updatedBy));
    }

    /// <summary>
    /// Transfer enrollment to another class
    /// </summary>
    public void Transfer(DateTime exitDate, string reason, string updatedBy)
    {
        if (EnrollmentStatus != "Active")
            throw new InvalidOperationException("Can only transfer active enrollments");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Transfer reason is required", nameof(reason));
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));
        if (exitDate < EntryDate)
            throw new ArgumentException("Exit date cannot be before entry date", nameof(exitDate));

        EnrollmentStatus = "Transferred";
        ExitDate = exitDate;
        ExitReason = reason;
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new EnrollmentTransferredEvent(Id, StudentId, ClassId, ExitDate.Value, reason, updatedBy));
    }

    /// <summary>
    /// Verify that modification is allowed (cannot modify if school year is archived)
    /// </summary>
    public bool CanModify(bool isSchoolYearArchived)
    {
        return !isSchoolYearArchived;
    }
}
