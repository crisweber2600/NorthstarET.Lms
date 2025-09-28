using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthstarET.Lms.Application.DTOs.Audit;
using NorthstarET.Lms.Application.Queries.Audit;
using NorthstarET.Lms.Application.Services;

namespace NorthstarET.Lms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Always require authentication
public class AuditController : ControllerBase
{
    private readonly AuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        AuditService auditService,
        ILogger<AuditController> logger)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search audit records with pagination and filtering
    /// </summary>
    /// <param name="query">Audit search query parameters</param>
    /// <returns>Paginated list of audit records</returns>
    [HttpGet("records")]
    [Authorize(Roles = "DistrictAdmin,ComplianceOfficer")]
    public async Task<ActionResult<PagedResult<AuditRecordDto>>> SearchAuditRecords(
        [FromQuery] SearchAuditRecordsQuery query)
    {
        try
        {
            _logger.LogInformation("Searching audit records - Page: {Page}, Size: {Size}", query.Page, query.Size);
            
            var result = await _auditService.SearchAuditRecordsAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error searching audit records: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit records");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get a specific audit record by ID
    /// </summary>
    /// <param name="id">Audit record ID</param>
    /// <returns>Detailed audit record information</returns>
    [HttpGet("records/{id:guid}")]
    [Authorize(Roles = "DistrictAdmin,ComplianceOfficer")]
    public async Task<ActionResult<AuditRecordDetailDto>> GetAuditRecord(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving audit record: {AuditRecordId}", id);
            
            var query = new GetAuditRecordQuery { Id = id };
            var result = await _auditService.GetAuditRecordAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Audit record not found: {AuditRecordId}", id);
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit record: {AuditRecordId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get audit trail for a specific entity
    /// </summary>
    /// <param name="query">Entity audit trail query parameters</param>
    /// <returns>Chronological list of audit records for the entity</returns>
    [HttpGet("entity-trail")]
    [Authorize(Roles = "DistrictAdmin,ComplianceOfficer,SchoolUser")]
    public async Task<ActionResult<PagedResult<AuditRecordDto>>> GetEntityAuditTrail(
        [FromQuery] GetEntityAuditTrailQuery query)
    {
        try
        {
            _logger.LogInformation("Retrieving audit trail for entity: {EntityType} {EntityId}", 
                query.EntityType, query.EntityId);
            
            var result = await _auditService.GetEntityAuditTrailAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error retrieving audit trail for entity {EntityType} {EntityId}: {Error}", 
                    query.EntityType, query.EntityId, result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for entity: {EntityType} {EntityId}", 
                query.EntityType, query.EntityId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Verify audit chain integrity for a date range
    /// </summary>
    /// <param name="query">Chain verification query parameters</param>
    /// <returns>Chain integrity verification results</returns>
    [HttpPost("verify-chain")]
    [Authorize(Roles = "DistrictAdmin,ComplianceOfficer")]
    public async Task<ActionResult<AuditChainVerificationDto>> VerifyAuditChain(
        [FromBody] VerifyAuditChainQuery query)
    {
        try
        {
            _logger.LogInformation("Verifying audit chain from {StartDate} to {EndDate}", 
                query.StartDate, query.EndDate);
            
            var result = await _auditService.VerifyAuditChainAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Audit chain verification failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying audit chain");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Export audit records for compliance reporting
    /// </summary>
    /// <param name="query">Export query parameters</param>
    /// <returns>Export job information or direct download</returns>
    [HttpPost("export")]
    [Authorize(Roles = "DistrictAdmin,ComplianceOfficer")]
    public async Task<ActionResult> ExportAuditRecords(
        [FromBody] ExportAuditRecordsQuery query)
    {
        try
        {
            _logger.LogInformation("Exporting audit records from {StartDate} to {EndDate}", 
                query.StartDate, query.EndDate);
            
            var result = await _auditService.ExportAuditRecordsAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Audit export failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            // For large exports, return job ID. For small exports, return file directly.
            if (result.Value.RequiresAsyncProcessing)
            {
                return Accepted(new { 
                    jobId = result.Value.JobId,
                    estimatedCompletionTime = result.Value.EstimatedCompletionTime
                });
            }
            else
            {
                return File(
                    result.Value.Content, 
                    result.Value.ContentType, 
                    result.Value.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit records");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get audit statistics for dashboard display
    /// </summary>
    /// <param name="query">Statistics query parameters</param>
    /// <returns>Audit statistics summary</returns>
    [HttpGet("statistics")]
    [Authorize(Roles = "DistrictAdmin,ComplianceOfficer")]
    public async Task<ActionResult<AuditStatisticsDto>> GetAuditStatistics(
        [FromQuery] GetAuditStatisticsQuery query)
    {
        try
        {
            _logger.LogInformation("Retrieving audit statistics for date range: {StartDate} to {EndDate}", 
                query.StartDate, query.EndDate);
            
            var result = await _auditService.GetAuditStatisticsAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error retrieving audit statistics: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit statistics");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get user activity summary for a specific user
    /// </summary>
    /// <param name="query">User activity query parameters</param>
    /// <returns>User's activity summary</returns>
    [HttpGet("user-activity")]
    [Authorize(Roles = "DistrictAdmin,ComplianceOfficer")]
    public async Task<ActionResult<PagedResult<UserActivityDto>>> GetUserActivity(
        [FromQuery] GetUserActivityQuery query)
    {
        try
        {
            _logger.LogInformation("Retrieving user activity for user: {UserId}", query.UserId);
            
            var result = await _auditService.GetUserActivityAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error retrieving user activity for user {UserId}: {Error}", 
                    query.UserId, result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity for user: {UserId}", query.UserId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get compliance report for regulatory requirements
    /// </summary>
    /// <param name="query">Compliance report query parameters</param>
    /// <returns>Compliance report data</returns>
    [HttpPost("compliance-report")]
    [Authorize(Roles = "DistrictAdmin,ComplianceOfficer")]
    public async Task<ActionResult<ComplianceReportDto>> GetComplianceReport(
        [FromBody] GenerateComplianceReportQuery query)
    {
        try
        {
            _logger.LogInformation("Generating compliance report for type: {ReportType}", query.ReportType);
            
            var result = await _auditService.GenerateComplianceReportAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Compliance report generation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}