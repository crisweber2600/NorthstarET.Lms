using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Queries.Classes;

/// <summary>
/// Query to get a class by ID
/// </summary>
public class GetClassByIdQuery : Query<ClassDto>
{
    public required Guid ClassId { get; init; }
}

/// <summary>
/// Query to get all classes for a school
/// </summary>
public class GetClassesBySchoolQuery : Query<List<ClassDto>>
{
    public required Guid SchoolId { get; init; }
    public Guid? SchoolYearId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Query to get class roster (students enrolled)
/// </summary>
public class GetClassRosterQuery : Query<List<StudentDto>>
{
    public required Guid ClassId { get; init; }
}
