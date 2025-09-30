using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Domain.Events;

/// <summary>
/// Raised when a student is promoted to the next grade
/// </summary>
public sealed class StudentPromotedEvent : DomainEvent
{
    public Guid StudentId { get; }
    public GradeLevel FromGrade { get; }
    public GradeLevel ToGrade { get; }
    public Guid SchoolYearId { get; }

    public StudentPromotedEvent(Guid studentId, GradeLevel fromGrade, GradeLevel toGrade, Guid schoolYearId)
    {
        StudentId = studentId;
        FromGrade = fromGrade;
        ToGrade = toGrade;
        SchoolYearId = schoolYearId;
    }
}

/// <summary>
/// Raised when a student graduates
/// </summary>
public sealed class StudentGraduatedEvent : DomainEvent
{
    public Guid StudentId { get; }
    public DateTime GraduationDate { get; }
    public Guid SchoolYearId { get; }

    public StudentGraduatedEvent(Guid studentId, DateTime graduationDate, Guid schoolYearId)
    {
        StudentId = studentId;
        GraduationDate = graduationDate;
        SchoolYearId = schoolYearId;
    }
}

/// <summary>
/// Raised when a student is enrolled in a class
/// </summary>
public sealed class StudentEnrolledEvent : DomainEvent
{
    public Guid StudentId { get; }
    public Guid ClassId { get; }
    public Guid SchoolYearId { get; }
    public DateTime EnrollmentDate { get; }

    public StudentEnrolledEvent(Guid studentId, Guid classId, Guid schoolYearId, DateTime enrollmentDate)
    {
        StudentId = studentId;
        ClassId = classId;
        SchoolYearId = schoolYearId;
        EnrollmentDate = enrollmentDate;
    }
}