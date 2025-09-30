using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Queries.Schools;

/// <summary>
/// Query to get a school by ID
/// </summary>
public class GetSchoolByIdQuery : Query<SchoolDto>
{
    public required Guid SchoolId { get; init; }
}

/// <summary>
/// Query to get all schools in a district
/// </summary>
public class GetSchoolsByDistrictQuery : Query<List<SchoolDto>>
{
    public required Guid DistrictId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
