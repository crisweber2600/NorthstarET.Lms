using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs.Districts;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Application.UseCases.Districts;

public class CreateDistrictUseCase
{
    private readonly IDistrictRepository _districtRepository;
    private readonly IAuditService _auditService;

    public CreateDistrictUseCase(
        IDistrictRepository districtRepository,
        IAuditService auditService)
    {
        _districtRepository = districtRepository;
        _auditService = auditService;
    }

    public async Task<Result<DistrictDto>> ExecuteAsync(CreateDistrictDto request, string createdByUserId)
    {
        // Validate business rules
        if (await _districtRepository.SlugExistsAsync(request.Slug))
        {
            return Result.Failure<DistrictDto>("District slug already exists");
        }

        // Create domain entity
        var quotas = new DistrictQuotas
        {
            MaxStudents = request.Quotas.MaxStudents,
            MaxStaff = request.Quotas.MaxStaff,
            MaxAdmins = request.Quotas.MaxAdmins
        };

        var district = new DistrictTenant(
            request.Slug,
            request.DisplayName,
            quotas,
            createdByUserId);

        // Persist the district
        await _districtRepository.AddAsync(district);
        await _districtRepository.SaveChangesAsync();

        // Generate audit record
        await _auditService.LogAsync(
            "DistrictCreated",
            typeof(DistrictTenant).Name,
            district.Id,
            createdByUserId,
            new { request.Slug, request.DisplayName, request.Quotas });

        // Map to DTO
        var dto = new DistrictDto
        {
            DistrictId = district.Id,
            Slug = district.Slug,
            DisplayName = district.DisplayName,
            Status = district.Status,
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = district.Quotas.MaxStudents,
                MaxStaff = district.Quotas.MaxStaff,
                MaxAdmins = district.Quotas.MaxAdmins
            },
            CreatedDate = district.CreatedAt,
            LastModifiedDate = district.LastModifiedDate ?? district.CreatedAt
        };

        return Result<DistrictDto>.Success(dto);
    }
}

public class GetDistrictUseCase
{
    private readonly IDistrictRepository _districtRepository;

    public GetDistrictUseCase(IDistrictRepository districtRepository)
    {
        _districtRepository = districtRepository;
    }

    public async Task<Result<DistrictDto>> ExecuteAsync(Guid districtId)
    {
        var district = await _districtRepository.GetByIdAsync(districtId);
        if (district == null)
        {
            return Result.Failure<DistrictDto>("District not found");
        }

        var dto = new DistrictDto
        {
            DistrictId = district.Id,
            Slug = district.Slug,
            DisplayName = district.DisplayName,
            Status = district.Status,
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = district.Quotas.MaxStudents,
                MaxStaff = district.Quotas.MaxStaff,
                MaxAdmins = district.Quotas.MaxAdmins
            },
            CreatedDate = district.CreatedAt,
            LastModifiedDate = district.LastModifiedDate ?? district.CreatedAt
        };

        return Result<DistrictDto>.Success(dto);
    }
}

public class SuspendDistrictUseCase
{
    private readonly IDistrictRepository _districtRepository;
    private readonly IAuditService _auditService;

    public SuspendDistrictUseCase(
        IDistrictRepository districtRepository,
        IAuditService auditService)
    {
        _districtRepository = districtRepository;
        _auditService = auditService;
    }

    public async Task<Result> ExecuteAsync(Guid districtId, string reason, string suspendedByUserId)
    {
        var district = await _districtRepository.GetByIdAsync(districtId);
        if (district == null)
        {
            return Result.Failure("District not found");
        }

        try
        {
            district.Suspend(reason, suspendedByUserId);
            await _districtRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                "DistrictSuspended",
                typeof(DistrictTenant).Name,
                district.Id,
                suspendedByUserId,
                new { reason });

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
