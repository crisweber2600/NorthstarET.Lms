using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NorthstarET.Lms.Presentation.Api.Controllers;

[ApiController]
[Route("api/v1/{tenant}/bulk")]
[Authorize]
public class BulkController : ControllerBase
{
    private readonly IBulkOperationService _bulkService;
    private readonly ILogger<BulkController> _logger;

    public BulkController(IBulkOperationService bulkService, ILogger<BulkController> logger)
    {
        _bulkService = bulkService;
        _logger = logger;
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportBulkData(
        string tenant,
        [FromBody] BulkImportRequest request)
    {
        _logger.LogInformation("Starting bulk import operation: {Type}", request.OperationType);
        
        var result = await _bulkService.StartBulkOperationAsync(
            request.OperationType,
            request.Data,
            request.ErrorStrategy,
            request.DryRun);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return AcceptedAtAction(nameof(GetBulkOperationStatus), 
            new { tenant, jobId = result.Value }, 
            new { jobId = result.Value, status = "processing" });
    }

    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBulkOperationStatus(string tenant, Guid jobId)
    {
        var result = await _bulkService.GetBulkJobStatusAsync(jobId);
        
        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("rollover")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RolloverSchoolYear(
        string tenant,
        [FromBody] RolloverRequest request)
    {
        _logger.LogInformation("Starting school year rollover from {FromYear} to {ToYear}", 
            request.FromSchoolYearId, request.ToSchoolYearId);
        
        var result = await _bulkService.RolloverSchoolYearAsync(
            request.FromSchoolYearId,
            request.ToSchoolYearId,
            request.CopyEnrollments,
            request.CopyStaffAssignments);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return AcceptedAtAction(nameof(GetBulkOperationStatus), 
            new { tenant, jobId = result.Value }, 
            new { jobId = result.Value, status = "processing" });
    }

    [HttpPost("jobs/{jobId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBulkOperation(string tenant, Guid jobId)
    {
        _logger.LogInformation("Canceling bulk operation {JobId}", jobId);
        
        var result = await _bulkService.CancelBulkOperationAsync(jobId);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpGet("jobs/{jobId:guid}/errors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBulkOperationErrors(string tenant, Guid jobId)
    {
        var result = await _bulkService.GetBulkJobErrorsAsync(jobId);
        
        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}

public record BulkImportRequest(
    string OperationType,
    object Data,
    string ErrorStrategy,
    bool DryRun);

public record RolloverRequest(
    Guid FromSchoolYearId,
    Guid ToSchoolYearId,
    bool CopyEnrollments,
    bool CopyStaffAssignments);
