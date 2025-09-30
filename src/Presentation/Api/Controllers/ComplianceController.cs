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
        var result = await _auditService.QueryAuditRecordsAsync(
            entityType,
            entityId,
            actorId,
            startDate,
            endDate);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("audit/verify-integrity")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyAuditIntegrity(string tenant)
    {
        _logger.LogInformation("Verifying audit trail integrity for tenant {Tenant}", tenant);
        
        var result = await _auditService.VerifyAuditIntegrityAsync();
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { isValid = result.Value, message = result.Value ? "Audit trail is intact" : "Audit trail has been tampered" });
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
        
        var result = await _retentionService.ApplyLegalHoldAsync(
            request.EntityType,
            request.EntityId,
            request.Reason,
            request.CaseNumber);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetLegalHold), 
            new { tenant, id = result.Value }, new { id = result.Value });
    }

    [HttpGet("legal-holds/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLegalHold(string tenant, Guid id)
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
        var result = await _retentionService.ReleaseLegalHoldAsync(id, request.ReleasedBy);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpPost("retention/purge-eligible")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurgeEligibleRecords(string tenant)
    {
        _logger.LogInformation("Starting purge of eligible records for tenant {Tenant}", tenant);
        
        var result = await _retentionService.PurgeEligibleRecordsAsync();
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { purgedCount = result.Value });
    }

    [HttpGet("retention/compliance-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComplianceReport(string tenant)
    {
        var result = await _retentionService.GetComplianceReportAsync();
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}

public record ApplyLegalHoldRequest(
    string EntityType,
    Guid EntityId,
    string Reason,
    string CaseNumber);

public record ReleaseLegalHoldRequest(string ReleasedBy);
