using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Commands.Schools;

/// <summary>
/// Command to create a new school
/// </summary>
public class CreateSchoolCommand : Command<SchoolDto>
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required Guid DistrictId { get; init; }
    public string? Address { get; init; }
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// Command to update school status
/// </summary>
public class UpdateSchoolStatusCommand : Command
{
    public required Guid SchoolId { get; init; }
    public required string Status { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Command to update school details
/// </summary>
public class UpdateSchoolDetailsCommand : Command
{
    public required Guid SchoolId { get; init; }
    public string? Name { get; init; }
    public string? Address { get; init; }
    public string? PhoneNumber { get; init; }
}
