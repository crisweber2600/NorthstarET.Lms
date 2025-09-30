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
        
        var job = await _bulkService.ExecuteBulkOperationAsync(
            request.OperationType,
            request.TotalRows,
            request.ErrorStrategy,
            request.ErrorThreshold,
            request.DryRun,
            User.Identity?.Name ?? "system");

        return AcceptedAtAction(nameof(GetBulkOperationStatus), 
            new { tenant, jobId = job.Id }, 
            new { jobId = job.Id, status = "processing" });
    }

    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBulkOperationStatus(string tenant, Guid jobId)
    {
        var job = await _bulkService.GetJobStatusAsync(jobId);

        return Ok(job);
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
        
        var job = await _bulkService.ExecuteBulkOperationAsync(
            "SchoolYearRollover",
            1, // Will be updated during processing
            "FailFast",
            null,
            request.DryRun,
            User.Identity?.Name ?? "system");

        return AcceptedAtAction(nameof(GetBulkOperationStatus), 
            new { tenant, jobId = job.Id }, 
            new { jobId = job.Id, status = "processing" });
    }

    [HttpPost("jobs/{jobId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBulkOperation(string tenant, Guid jobId)
    {
        _logger.LogInformation("Canceling bulk operation {JobId}", jobId);
        
        await _bulkService.CompleteJobAsync(jobId, "Canceled by user");

        return NoContent();
    }

    [HttpGet("jobs/{jobId:guid}/errors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBulkOperationErrors(string tenant, Guid jobId)
    {
        var job = await _bulkService.GetJobStatusAsync(jobId);
        
        return Ok(new { errors = job.ErrorDetails });
    }
}

public record BulkImportRequest(
    string OperationType,
    int TotalRows,
    string ErrorStrategy,
    int? ErrorThreshold,
    bool DryRun);

public record RolloverRequest(
    Guid FromSchoolYearId,
    Guid ToSchoolYearId,
    bool CopyEnrollments,
    bool CopyStaffAssignments,
    bool DryRun = false);
