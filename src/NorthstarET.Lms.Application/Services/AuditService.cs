using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Services;
using System.Text;

namespace NorthstarET.Lms.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IPlatformAuditRepository _platformAuditRepository;
    private readonly IAuditChainService _auditChainService;
    private readonly ITenantContextAccessor _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(
        IAuditRepository auditRepository,
        IPlatformAuditRepository platformAuditRepository,
        IAuditChainService auditChainService,
        ITenantContextAccessor tenantContext,
        IUnitOfWork unitOfWork)
    {
        _auditRepository = auditRepository;
        _platformAuditRepository = platformAuditRepository;
        _auditChainService = auditChainService;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuditRecord> LogAuditEventAsync(CreateAuditRecordDto auditDto)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("Cannot create tenant audit record without tenant context");
        }

        var previousHash = await _auditChainService.GetPreviousAuditHashAsync(tenantId, DateTime.UtcNow);
        
        var auditRecord = new AuditRecord(
            auditDto.Action,
            auditDto.EntityType,
            auditDto.EntityId,
            auditDto.UserId,
            auditDto.Details,
            auditDto.IpAddress,
            auditDto.UserAgent);

        var hash = _auditChainService.GenerateAuditHash(auditRecord, previousHash);
        auditRecord.SetHash(hash);

        await _auditRepository.AddAsync(auditRecord);
        await _unitOfWork.SaveChangesAsync();

        return auditRecord;
    }

    public async Task<PlatformAuditRecord> LogPlatformAuditEventAsync(CreateAuditRecordDto auditDto)
    {
        var auditRecord = new PlatformAuditRecord(
            auditDto.Action,
            auditDto.EntityType,
            auditDto.EntityId,
            auditDto.UserId,
            auditDto.Details,
            auditDto.IpAddress,
            auditDto.UserAgent);

        await _platformAuditRepository.AddAsync(auditRecord);
        await _unitOfWork.SaveChangesAsync();

        return auditRecord;
    }

    public async Task<PagedResult<AuditRecord>> QueryAuditRecordsAsync(AuditQueryDto queryDto)
    {
        return await _auditRepository.QueryAsync(queryDto);
    }

    public async Task<AuditExportResultDto> ExportAuditRecordsAsync(AuditExportDto exportDto)
    {
        var records = await _auditRepository.GetByDateRangeAsync(
            exportDto.StartDate,
            exportDto.EndDate,
            exportDto.EntityTypes);

        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,Action,EntityType,EntityId,UserId,Details,IpAddress,Hash");

        foreach (var record in records)
        {
            csv.AppendLine($"{record.Timestamp:yyyy-MM-dd HH:mm:ss},{record.Action},{record.EntityType},{record.EntityId},{record.UserId},\"{record.Details.Replace("\"", "\"\"")}\",{record.IpAddress},{record.Hash}");
        }

        var fileName = $"audit_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var data = Encoding.UTF8.GetBytes(csv.ToString());

        return new AuditExportResultDto
        {
            Data = data,
            FileName = fileName,
            ContentType = "text/csv"
        };
    }

    public async Task<bool> ValidateAuditChainAsync(DateTime startDate, DateTime endDate)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return false;
        }

        var records = await _auditRepository.GetChainAsync(tenantId, startDate, endDate);
        return await _auditChainService.ValidateAuditChainAsync(records);
    }

    public async Task<IEnumerable<string>> DetectAuditTamperingAsync(DateTime startDate, DateTime endDate)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return Enumerable.Empty<string>();
        }

        var records = await _auditRepository.GetChainAsync(tenantId, startDate, endDate);
        return await _auditChainService.DetectTamperingAsync(records);
    }
}