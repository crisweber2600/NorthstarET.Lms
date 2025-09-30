using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Commands.Classes;

/// <summary>
/// Command to create a new class
/// </summary>
public class CreateClassCommand : Command<ClassDto>
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required Guid SchoolId { get; init; }
    public required Guid SchoolYearId { get; init; }
    public required int MaxCapacity { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Command to update class capacity
/// </summary>
public class UpdateClassCapacityCommand : Command
{
    public required Guid ClassId { get; init; }
    public required int NewCapacity { get; init; }
}

/// <summary>
/// Command to update class status
/// </summary>
public class UpdateClassStatusCommand : Command
{
    public required Guid ClassId { get; init; }
    public required string Status { get; init; }
}
