using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Queries.Districts;

/// <summary>
/// Query to get a district by slug
/// </summary>
public class GetDistrictBySlugQuery : Query<DistrictDto>
{
    public required string Slug { get; init; }
}

/// <summary>
/// Query to get all districts (platform admin only)
/// </summary>
public class GetAllDistrictsQuery : Query<List<DistrictDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
