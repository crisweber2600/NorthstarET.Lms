using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs.Audit;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Application.UseCases.Audit;

public class QueryAuditLogsUseCase
{
    private readonly IAuditRepository _auditRepository;

    public QueryAuditLogsUseCase(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<Result<PagedResult<AuditRecordDto>>> ExecuteAsync(AuditQueryRequest request)
    {
        var auditRecords = await _auditRepository.QueryAsync(
            request.EntityType,
            request.EntityId,
            request.UserId,
            request.EventType,
            request.StartDate,
            request.EndDate,
            request.Page,
            request.PageSize);

        var dtos = auditRecords.Items.Select(record => new AuditRecordDto
        {
            Id = record.Id,
            EventType = record.EventType,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            UserId = record.UserId,
            Timestamp = record.Timestamp,
            IpAddress = record.IpAddress,
            UserAgent = record.UserAgent,
            ChangeDetails = record.ChangeDetails,
            CorrelationId = record.CorrelationId,
            SequenceNumber = record.SequenceNumber
        }).ToList();

        var result = new PagedResult<AuditRecordDto>(
            dtos,
            request.Page,
            request.PageSize,
            auditRecords.TotalCount);

        return Result<PagedResult<AuditRecordDto>>.Success(result);
    }
}

public class VerifyAuditIntegrityUseCase
{
    private readonly IAuditRepository _auditRepository;
    private readonly IAuditChainIntegrityService _integrityService;

    public VerifyAuditIntegrityUseCase(
        IAuditRepository auditRepository,
        IAuditChainIntegrityService integrityService)
    {
        _auditRepository = auditRepository;
        _integrityService = integrityService;
    }

    public async Task<Result<AuditIntegrityResultDto>> ExecuteAsync(AuditIntegrityCheckRequest request)
    {
        var records = await _auditRepository.GetRecordsForIntegrityCheckAsync(
            request.StartSequence,
            request.EndSequence);

        var integrityResults = new List<AuditIntegrityIssue>();

        // Verify hash chain integrity
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var isValid = await _integrityService.VerifyRecordIntegrityAsync(record);

            if (!isValid)
            {
                integrityResults.Add(new AuditIntegrityIssue
                {
                    RecordId = record.Id,
                    SequenceNumber = record.SequenceNumber,
                    IssueType = "Hash Verification Failed",
                    Description = "Record hash does not match calculated hash"
                });
            }

            // Check sequence continuity
            if (i > 0)
            {
                var previousRecord = records[i - 1];
                if (record.SequenceNumber != previousRecord.SequenceNumber + 1)
                {
                    integrityResults.Add(new AuditIntegrityIssue
                    {
                        RecordId = record.Id,
                        SequenceNumber = record.SequenceNumber,
                        IssueType = "Sequence Gap",
                        Description = $"Expected sequence {previousRecord.SequenceNumber + 1}, found {record.SequenceNumber}"
                    });
                }

                // Verify chain linkage
                if (record.PreviousRecordHash != previousRecord.RecordHash)
                {
                    integrityResults.Add(new AuditIntegrityIssue
                    {
                        RecordId = record.Id,
                        SequenceNumber = record.SequenceNumber,
                        IssueType = "Chain Linkage Broken",
                        Description = "Previous record hash does not match expected value"
                    });
                }
            }
        }

        var resultDto = new AuditIntegrityResultDto
        {
            CheckStartTime = DateTime.UtcNow,
            RecordsChecked = records.Count,
            StartSequence = request.StartSequence ?? (records.FirstOrDefault()?.SequenceNumber ?? 0),
            EndSequence = request.EndSequence ?? (records.LastOrDefault()?.SequenceNumber ?? 0),
            IssuesFound = integrityResults.Count,
            Issues = integrityResults,
            OverallIntegrity = integrityResults.Count == 0 ? "Valid" : "Compromised"
        };

        return Result<AuditIntegrityResultDto>.Success(resultDto);
    }
}

public class CreateLegalHoldUseCase
{
    private readonly ILegalHoldRepository _legalHoldRepository;
    private readonly IAuditService _auditService;

    public CreateLegalHoldUseCase(
        ILegalHoldRepository legalHoldRepository,
        IAuditService auditService)
    {
        _legalHoldRepository = legalHoldRepository;
        _auditService = auditService;
    }

    public async Task<Result<LegalHoldDto>> ExecuteAsync(
        CreateLegalHoldRequest request,
        string createdByUserId)
    {
        // Check for existing active hold
        var existingHold = await _legalHoldRepository.GetActiveHoldAsync(
            request.EntityType, request.EntityId);
        
        if (existingHold != null)
        {
            return Result<LegalHoldDto>.Failure("Entity already has an active legal hold");
        }

        var legalHold = new LegalHold(
            request.EntityType,
            request.EntityId,
            request.Reason,
            createdByUserId);

        await _legalHoldRepository.AddAsync(legalHold);
        await _legalHoldRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            "LegalHoldCreated",
            typeof(LegalHold).Name,
            legalHold.Id,
            createdByUserId,
            new { request.EntityType, request.EntityId, request.Reason });

        var dto = new LegalHoldDto
        {
            Id = legalHold.Id,
            EntityType = legalHold.EntityType,
            EntityId = legalHold.EntityId,
            Reason = legalHold.Reason,
            HoldDate = legalHold.HoldDate,
            ReleaseDate = legalHold.ReleaseDate,
            IsActive = legalHold.IsActive,
            AuthorizingUser = legalHold.AuthorizingUser
        };

        return Result<LegalHoldDto>.Success(dto);
    }
}

public class ReleaseLegalHoldUseCase
{
    private readonly ILegalHoldRepository _legalHoldRepository;
    private readonly IAuditService _auditService;

    public ReleaseLegalHoldUseCase(
        ILegalHoldRepository legalHoldRepository,
        IAuditService auditService)
    {
        _legalHoldRepository = legalHoldRepository;
        _auditService = auditService;
    }

    public async Task<Result> ExecuteAsync(Guid legalHoldId, string releasedByUserId)
    {
        var legalHold = await _legalHoldRepository.GetByIdAsync(legalHoldId);
        if (legalHold == null)
        {
            return Result.Failure("Legal hold not found");
        }

        if (!legalHold.IsActive)
        {
            return Result.Failure("Legal hold is already released");
        }

        legalHold.Release(releasedByUserId);
        await _legalHoldRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            "LegalHoldReleased",
            typeof(LegalHold).Name,
            legalHold.Id,
            releasedByUserId,
            new { legalHold.EntityType, legalHold.EntityId });

        return Result.Success();
    }
}

public class GenerateComplianceReportUseCase
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILegalHoldRepository _legalHoldRepository;
    private readonly IRetentionPolicyRepository _retentionPolicyRepository;

    public GenerateComplianceReportUseCase(
        IAuditRepository auditRepository,
        ILegalHoldRepository legalHoldRepository,
        IRetentionPolicyRepository retentionPolicyRepository)
    {
        _auditRepository = auditRepository;
        _legalHoldRepository = legalHoldRepository;
        _retentionPolicyRepository = retentionPolicyRepository;
    }

    public async Task<Result<ComplianceReportDto>> ExecuteAsync(ComplianceReportRequest request)
    {
        var auditSummary = await _auditRepository.GetComplianceSummaryAsync(
            request.StartDate, request.EndDate);

        var activeLegalHolds = await _legalHoldRepository.GetActiveLegalHoldsAsync();

        var retentionPolicies = await _retentionPolicyRepository.GetActivePoliciesAsync();

        var report = new ComplianceReportDto
        {
            GeneratedDate = DateTime.UtcNow,
            ReportPeriodStart = request.StartDate,
            ReportPeriodEnd = request.EndDate,
            AuditSummary = new AuditComplianceSummaryDto
            {
                TotalAuditRecords = auditSummary.TotalRecords,
                RecordsByEventType = auditSummary.EventTypeCounts,
                DataAccessEvents = auditSummary.DataAccessCount,
                DataModificationEvents = auditSummary.DataModificationCount,
                IntegrityIssues = auditSummary.IntegrityIssues
            },
            LegalHoldSummary = new LegalHoldSummaryDto
            {
                ActiveLegalHolds = activeLegalHolds.Count,
                HoldsByEntityType = activeLegalHolds
                    .GroupBy(h => h.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                OldestActiveHold = activeLegalHolds
                    .OrderBy(h => h.HoldDate)
                    .FirstOrDefault()?.HoldDate
            },
            RetentionSummary = new RetentionSummaryDto
            {
                ActivePolicies = retentionPolicies.Count,
                PoliciesByEntityType = retentionPolicies
                    .GroupBy(p => p.EntityType)
                    .ToDictionary(g => g.Key, g => g.First().RetentionYears)
            }
        };

        return Result<ComplianceReportDto>.Success(report);
    }
}