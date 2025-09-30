using FluentValidation;
using NorthstarET.Lms.Application.Commands.Districts;

namespace NorthstarET.Lms.Application.Validators;

/// <summary>
/// Validator for CreateDistrictCommand
/// </summary>
public class CreateDistrictCommandValidator : AbstractValidator<CreateDistrictCommand>
{
    public CreateDistrictCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("District slug is required")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens")
            .MinimumLength(3).WithMessage("Slug must be at least 3 characters")
            .MaximumLength(50).WithMessage("Slug must not exceed 50 characters");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MinimumLength(3).WithMessage("Display name must be at least 3 characters")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters");

        When(x => x.CustomQuotas != null, () =>
        {
            RuleFor(x => x.CustomQuotas!.Students)
                .GreaterThan(0).WithMessage("Student quota must be greater than 0");

            RuleFor(x => x.CustomQuotas!.Staff)
                .GreaterThan(0).WithMessage("Staff quota must be greater than 0");

            RuleFor(x => x.CustomQuotas!.Admins)
                .GreaterThan(0).WithMessage("Admin quota must be greater than 0");
        });
    }
}

/// <summary>
/// Validator for UpdateDistrictStatusCommand
/// </summary>
public class UpdateDistrictStatusCommandValidator : AbstractValidator<UpdateDistrictStatusCommand>
{
    public UpdateDistrictStatusCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("District slug is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(status => status is "active" or "suspended" or "Active" or "Suspended")
            .WithMessage("Status must be 'active' or 'suspended'");

        When(x => x.Status?.ToLowerInvariant() == "suspended", () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required for suspension")
                .MinimumLength(10).WithMessage("Reason must be at least 10 characters");
        });
    }
}

/// <summary>
/// Validator for UpdateDistrictQuotasCommand
/// </summary>
public class UpdateDistrictQuotasCommandValidator : AbstractValidator<UpdateDistrictQuotasCommand>
{
    public UpdateDistrictQuotasCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("District slug is required");

        RuleFor(x => x.Quotas.Students)
            .GreaterThan(0).WithMessage("Student quota must be greater than 0");

        RuleFor(x => x.Quotas.Staff)
            .GreaterThan(0).WithMessage("Staff quota must be greater than 0");

        RuleFor(x => x.Quotas.Admins)
            .GreaterThan(0).WithMessage("Admin quota must be greater than 0");
    }
}
