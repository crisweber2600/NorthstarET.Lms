using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Commands.Enrollments;

/// <summary>
/// Command to enroll a student in a class
/// </summary>
public class EnrollStudentCommand : Command<EnrollmentDto>
{
    public required Guid StudentId { get; init; }
    public required Guid ClassId { get; init; }
    public DateTime? EnrollmentDate { get; init; }
    public bool AllowOverCapacity { get; init; }
}

/// <summary>
/// Command to withdraw a student from a class
/// </summary>
public class WithdrawStudentCommand : Command
{
    public required Guid EnrollmentId { get; init; }
    public required DateTime WithdrawalDate { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Command to transfer a student to another class
/// </summary>
public class TransferStudentCommand : Command<EnrollmentDto>
{
    public required Guid CurrentEnrollmentId { get; init; }
    public required Guid NewClassId { get; init; }
    public required DateTime TransferDate { get; init; }
    public string? Reason { get; init; }
}
