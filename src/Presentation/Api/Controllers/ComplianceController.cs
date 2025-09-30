using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NorthstarET.Lms.Presentation.Api.Controllers;

[ApiController]
[Route("api/v1/{tenant}/compliance")]
[Authorize]
public class ComplianceController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly IRetentionService _retentionService;
    private readonly ILogger<ComplianceController> _logger;

    public ComplianceController(
        IAuditService auditService,
        IRetentionService retentionService,
        ILogger<ComplianceController> logger)
    {
        _auditService = auditService;
        _retentionService = retentionService;
        _logger = logger;
    }

    // Audit endpoints
    [HttpGet("audit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditRecords(
        string tenant,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] string? actorId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var records = await _auditService.QueryAuditRecordsAsync(
            entityType,
            entityId,
            actorId,
            startDate,
            endDate);

        return Ok(records);
    }

    [HttpPost("audit/verify-integrity")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyAuditIntegrity(string tenant)
    {
        _logger.LogInformation("Verifying audit trail integrity for tenant {Tenant}", tenant);
        
        var isValid = await _auditService.VerifyAuditChainIntegrityAsync(tenant);

        return Ok(new { isValid, message = isValid ? "Audit trail is intact" : "Audit trail has been tampered" });
    }

    // Retention and legal hold endpoints
    [HttpPost("legal-holds")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyLegalHold(
        string tenant,
        [FromBody] ApplyLegalHoldRequest request)
    {
        _logger.LogInformation("Applying legal hold on {EntityType} {EntityId}", 
            request.EntityType, request.EntityId);
        
        var legalHold = await _retentionService.ApplyLegalHoldAsync(
            request.EntityType,
            request.EntityId,
            request.CaseNumber,
            request.Reason,
            User.Identity?.Name ?? "system");

        return CreatedAtAction(nameof(GetLegalHold), 
            new { tenant, id = legalHold.Id }, new { id = legalHold.Id });
    }

    [HttpGet("legal-holds/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetLegalHold(string tenant, Guid id)
    {
        // Placeholder - would need a query implementation
        return NotFound(new { error = "Legal hold retrieval not implemented" });
    }

    [HttpPost("legal-holds/{id:guid}/release")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReleaseLegalHold(
        string tenant,
        Guid id,
        [FromBody] ReleaseLegalHoldRequest request)
    {
        await _retentionService.ReleaseLegalHoldAsync(id, request.Reason, User.Identity?.Name ?? "system");

        return NoContent();
    }

    [HttpPost("retention/purge-eligible")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurgeEligibleRecords(string tenant, [FromBody] PurgeRequest request)
    {
        _logger.LogInformation("Starting purge of eligible records for tenant {Tenant}", tenant);
        
        var eligibleIds = await _retentionService.IdentifyEntitiesForPurgeAsync(request.EntityType);
        await _retentionService.ExecutePurgeAsync(request.EntityType, eligibleIds, User.Identity?.Name ?? "system");

        return Ok(new { purgedCount = eligibleIds.Count });
    }

    [HttpGet("retention/compliance-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetComplianceReport(string tenant)
    {
        // Placeholder - would need service implementation
        return Ok(new { message = "Compliance report not implemented" });
    }
}

public record ApplyLegalHoldRequest(
    string EntityType,
    Guid EntityId,
    string CaseNumber,
    string Reason);

public record ReleaseLegalHoldRequest(string Reason);

public record PurgeRequest(string EntityType);
