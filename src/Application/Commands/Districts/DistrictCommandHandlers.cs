using MediatR;
using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Application.Commands.Districts;

/// <summary>
/// Handler for creating a new district
/// </summary>
public class CreateDistrictCommandHandler : IRequestHandler<CreateDistrictCommand, Result<DistrictDto>>
{
    private readonly IDistrictRepository _districtRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDistrictCommandHandler(IDistrictRepository districtRepository, IUnitOfWork unitOfWork)
    {
        _districtRepository = districtRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DistrictDto>> Handle(CreateDistrictCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if district slug already exists
            var existingDistrict = await _districtRepository.GetBySlugAsync(request.Slug, cancellationToken);
            if (existingDistrict != null)
            {
                return Result.Failure<DistrictDto>("District slug already exists");
            }

            // Create the district
            var district = request.CustomQuotas != null
                ? new DistrictTenant(
                    request.Slug,
                    request.DisplayName,
                    new Quota(request.CustomQuotas.Students, request.CustomQuotas.Staff, request.CustomQuotas.Admins),
                    request.RequestedBy ?? "System")
                : new DistrictTenant(
                    request.Slug,
                    request.DisplayName,
                    request.RequestedBy ?? "System");

            // Save the district
            _districtRepository.Add(district);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return the created district DTO
            var districtDto = new DistrictDto
            {
                Id = district.Id,
                Slug = district.Slug,
                DisplayName = district.DisplayName,
                Status = district.Status.ToString(),
                Quotas = new QuotaDto
                {
                    Students = district.Quotas.Students,
                    Staff = district.Quotas.Staff,
                    Admins = district.Quotas.Admins
                },
                CreatedAt = district.CreatedAt,
                ActivatedAt = district.ActivatedAt,
                SuspendedAt = district.SuspendedAt,
                SuspendedReason = district.SuspendedReason
            };

            return Result.Success(districtDto);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<DistrictDto>($"Invalid input: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure<DistrictDto>($"Failed to create district: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for updating district status
/// </summary>
public class UpdateDistrictStatusCommandHandler : IRequestHandler<UpdateDistrictStatusCommand, Result>
{
    private readonly IDistrictRepository _districtRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDistrictStatusCommandHandler(IDistrictRepository districtRepository, IUnitOfWork unitOfWork)
    {
        _districtRepository = districtRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateDistrictStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var district = await _districtRepository.GetBySlugAsync(request.Slug, cancellationToken);
            if (district == null)
            {
                return Result.Failure("District not found");
            }

            switch (request.Status.ToLowerInvariant())
            {
                case "suspended":
                    if (string.IsNullOrWhiteSpace(request.Reason))
                    {
                        return Result.Failure("Reason is required for suspension");
                    }
                    district.Suspend(request.Reason, request.RequestedBy ?? "System");
                    break;

                case "active":
                    district.Activate(request.RequestedBy ?? "System");
                    break;

                default:
                    return Result.Failure($"Invalid status: {request.Status}");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure($"Invalid operation: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to update district status: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for updating district quotas
/// </summary>
public class UpdateDistrictQuotasCommandHandler : IRequestHandler<UpdateDistrictQuotasCommand, Result>
{
    private readonly IDistrictRepository _districtRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDistrictQuotasCommandHandler(IDistrictRepository districtRepository, IUnitOfWork unitOfWork)
    {
        _districtRepository = districtRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateDistrictQuotasCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var district = await _districtRepository.GetBySlugAsync(request.Slug, cancellationToken);
            if (district == null)
            {
                return Result.Failure("District not found");
            }

            var newQuotas = new Quota(
                request.Quotas.Students,
                request.Quotas.Staff,
                request.Quotas.Admins);

            district.UpdateQuotas(newQuotas, request.RequestedBy ?? "System");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure($"Invalid quotas: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to update district quotas: {ex.Message}");
        }
    }
}