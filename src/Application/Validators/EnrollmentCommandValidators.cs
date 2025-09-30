using FluentValidation;
using NorthstarET.Lms.Application.Commands.Enrollments;

namespace NorthstarET.Lms.Application.Validators;

/// <summary>
/// Validator for EnrollStudentCommand
/// </summary>
public class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required");

        RuleFor(x => x.ClassId)
            .NotEmpty().WithMessage("Class ID is required");

        When(x => x.EnrollmentDate.HasValue, () =>
        {
            RuleFor(x => x.EnrollmentDate!.Value)
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Enrollment date cannot be in the future");
        });
    }
}

/// <summary>
/// Validator for WithdrawStudentCommand
/// </summary>
public class WithdrawStudentCommandValidator : AbstractValidator<WithdrawStudentCommand>
{
    public WithdrawStudentCommandValidator()
    {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage("Enrollment ID is required");

        RuleFor(x => x.WithdrawalDate)
            .NotEmpty().WithMessage("Withdrawal date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Withdrawal date cannot be in the future");
    }
}

/// <summary>
/// Validator for TransferStudentCommand
/// </summary>
public class TransferStudentCommandValidator : AbstractValidator<TransferStudentCommand>
{
    public TransferStudentCommandValidator()
    {
        RuleFor(x => x.CurrentEnrollmentId)
            .NotEmpty().WithMessage("Current enrollment ID is required");

        RuleFor(x => x.NewClassId)
            .NotEmpty().WithMessage("New class ID is required");

        RuleFor(x => x.TransferDate)
            .NotEmpty().WithMessage("Transfer date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Transfer date cannot be in the future");
    }
}
