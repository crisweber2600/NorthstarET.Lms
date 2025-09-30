using FluentValidation;
using NorthstarET.Lms.Application.Commands.Schools;

namespace NorthstarET.Lms.Application.Validators;

/// <summary>
/// Validator for CreateSchoolCommand
/// </summary>
public class CreateSchoolCommandValidator : AbstractValidator<CreateSchoolCommand>
{
    public CreateSchoolCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("School name is required")
            .MinimumLength(3).WithMessage("School name must be at least 3 characters")
            .MaximumLength(200).WithMessage("School name must not exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("School code is required")
            .Matches("^[A-Z0-9-]+$").WithMessage("School code must contain only uppercase letters, numbers, and hyphens")
            .MinimumLength(2).WithMessage("School code must be at least 2 characters")
            .MaximumLength(20).WithMessage("School code must not exceed 20 characters");

        RuleFor(x => x.DistrictId)
            .NotEmpty().WithMessage("District ID is required");

        When(x => !string.IsNullOrEmpty(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone number must be in valid E.164 format");
        });
    }
}

/// <summary>
/// Validator for UpdateSchoolStatusCommand
/// </summary>
public class UpdateSchoolStatusCommandValidator : AbstractValidator<UpdateSchoolStatusCommand>
{
    public UpdateSchoolStatusCommandValidator()
    {
        RuleFor(x => x.SchoolId)
            .NotEmpty().WithMessage("School ID is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(status => status is "active" or "inactive" or "Active" or "Inactive")
            .WithMessage("Status must be 'active' or 'inactive'");
    }
}
