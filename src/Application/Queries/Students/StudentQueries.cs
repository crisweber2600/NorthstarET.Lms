using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Queries.Students;

/// <summary>
/// Query to get a student by ID
/// </summary>
public class GetStudentByIdQuery : Query<StudentDto>
{
    public required Guid StudentId { get; init; }
}

/// <summary>
/// Query to get a student by student number
/// </summary>
public class GetStudentByNumberQuery : Query<StudentDto>
{
    public required string StudentNumber { get; init; }
}

/// <summary>
/// Query to get student schedule (enrolled classes)
/// </summary>
public class GetStudentScheduleQuery : Query<List<EnrollmentDto>>
{
    public required Guid StudentId { get; init; }
    public Guid? SchoolYearId { get; init; }
}

/// <summary>
/// Query to get students by grade level
/// </summary>
public class GetStudentsByGradeLevelQuery : Query<List<StudentDto>>
{
    public required int GradeLevel { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
