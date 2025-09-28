using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Application.Services;

public class DistrictService
{
    private readonly IDistrictRepository _districtRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public DistrictService(
        IDistrictRepository districtRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _districtRepository = districtRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<Result<DistrictDto>> CreateDistrictAsync(CreateDistrictDto createDistrictDto, string createdBy)
    {
        // Check for duplicate slug
        var existingDistrict = await _districtRepository.GetBySlugAsync(createDistrictDto.Slug);
        if (existingDistrict != null)
        {
            return Result.Failure<DistrictDto>($"District with slug '{createDistrictDto.Slug}' already exists");
        }

        // Create district entity
        var district = new DistrictTenant(createDistrictDto.Slug, createDistrictDto.DisplayName);
        
        if (createDistrictDto.Quotas != null)
        {
            var quotas = new DistrictQuotas(
                createDistrictDto.Quotas.MaxStudents,
                createDistrictDto.Quotas.MaxStaff,
                createDistrictDto.Quotas.MaxAdmins);
            district.UpdateQuotas(quotas, createdBy);
        }

        // Save to repository
        await _districtRepository.AddAsync(district);
        await _unitOfWork.SaveChangesAsync();

        // Create audit record
        await _auditService.LogPlatformAuditEventAsync(new CreateAuditRecordDto
        {
            Action = "CREATE_DISTRICT",
            EntityType = "District",
            EntityId = district.Id,
            UserId = createdBy,
            Details = $"Created district: {createDistrictDto.DisplayName} ({createDistrictDto.Slug})",
            IpAddress = "127.0.0.1" // TODO: Get from HTTP context
        });

        return Result.Success(MapToDto(district));
    }

    public async Task<DistrictDto?> GetDistrictAsync(Guid districtId)
    {
        var district = await _districtRepository.GetByIdAsync(districtId);
        return district != null ? MapToDto(district) : null;
    }

    public async Task<Result<DistrictDto>> UpdateDistrictQuotasAsync(Guid districtId, DistrictQuotas newQuotas, string updatedBy)
    {
        var district = await _districtRepository.GetByIdAsync(districtId);
        if (district == null)
        {
            return Result.Failure<DistrictDto>("District not found");
        }

        district.UpdateQuotas(newQuotas, updatedBy);
        await _districtRepository.UpdateAsync(district);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(MapToDto(district));
    }

    public async Task<Result<DistrictDto>> SuspendDistrictAsync(Guid districtId, string reason, string suspendedBy)
    {
        var district = await _districtRepository.GetByIdAsync(districtId);
        if (district == null)
        {
            return Result.Failure<DistrictDto>("District not found");
        }

        district.Suspend(reason, suspendedBy);
        await _districtRepository.UpdateAsync(district);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(MapToDto(district));
    }

    public async Task<Result<bool>> DeleteDistrictAsync(Guid districtId, string deletedBy)
    {
        var district = await _districtRepository.GetByIdAsync(districtId);
        if (district == null)
        {
            return Result.Failure<bool>("District not found");
        }

        // Check for active retention policies
        var hasActiveRetention = await _districtRepository.HasActiveRetentionPoliciesAsync(districtId);
        if (hasActiveRetention)
        {
            return Result.Failure<bool>("Cannot delete district with active retention policies");
        }

        // For now, we don't actually delete, just mark as deleted
        district.Suspend("Deleted by administrator", deletedBy);
        await _districtRepository.UpdateAsync(district);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(true);
    }

    private static DistrictDto MapToDto(DistrictTenant district)
    {
        return new DistrictDto
        {
            Id = district.Id,
            Slug = district.Slug,
            DisplayName = district.DisplayName,
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = district.Quotas.MaxStudents,
                MaxStaff = district.Quotas.MaxStaff,
                MaxAdmins = district.Quotas.MaxAdmins
            },
            Status = district.Status,
            CreatedAt = district.CreatedAt
        };
    }
}