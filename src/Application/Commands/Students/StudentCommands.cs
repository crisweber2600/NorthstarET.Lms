using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Commands.Students;

/// <summary>
/// Command to create a new student
/// </summary>
public class CreateStudentCommand : Command<StudentDto>
{
    public required string StudentNumber { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required int GradeLevel { get; init; }
    public string? MiddleName { get; init; }
}

/// <summary>
/// Command to update student grade level
/// </summary>
public class UpdateStudentGradeLevelCommand : Command
{
    public required Guid StudentId { get; init; }
    public required int NewGradeLevel { get; init; }
}

/// <summary>
/// Command to update student status
/// </summary>
public class UpdateStudentStatusCommand : Command
{
    public required Guid StudentId { get; init; }
    public required string Status { get; init; }
}
