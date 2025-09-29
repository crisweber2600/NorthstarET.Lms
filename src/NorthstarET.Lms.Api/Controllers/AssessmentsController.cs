using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthstarET.Lms.Application.Commands.Assessments;
using NorthstarET.Lms.Application.Queries.Assessments;
using NorthstarET.Lms.Application.DTOs.Assessments;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Application.Services;

namespace NorthstarET.Lms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Always require authentication
public class AssessmentsController : ControllerBase
{
    private readonly ILogger<AssessmentsController> _logger;
    // TODO: Implement IAssessmentService
    // private readonly IAssessmentService _assessmentService;

    public AssessmentsController(
        ILogger<AssessmentsController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // _assessmentService = assessmentService ?? throw new ArgumentNullException(nameof(assessmentService));
    }

    /// <summary>
    /// Create a new assessment definition
    /// </summary>
    /// <param name="request">Assessment creation request with file upload</param>
    /// <returns>Created assessment information</returns>
    [HttpPost]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<AssessmentDto>> CreateAssessment(
        [FromForm] CreateAssessmentCommand request)
    {
        try
        {
            _logger.LogInformation("Creating assessment: {AssessmentName}", request.Name);
            
            var result = await _assessmentService.CreateAssessmentAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Assessment creation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Assessment created successfully: {AssessmentId}", result.Value.Id);
            return Created($"/api/v1/assessments/{result.Value.Id}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assessment: {AssessmentName}", request.Name);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get a specific assessment by ID
    /// </summary>
    /// <param name="id">Assessment ID</param>
    /// <returns>Assessment information</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<AssessmentDto>> GetAssessment(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving assessment: {AssessmentId}", id);
            
            var query = new GetAssessmentQuery { Id = id };
            var result = await _assessmentService.GetAssessmentAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Assessment not found: {AssessmentId}", id);
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assessment: {AssessmentId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// List assessments with pagination and filtering
    /// </summary>
    /// <param name="query">List assessments query parameters</param>
    /// <returns>Paginated list of assessments</returns>
    [HttpGet]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<PagedResult<AssessmentSummaryDto>>> ListAssessments(
        [FromQuery] ListAssessmentsQuery query)
    {
        try
        {
            _logger.LogInformation("Listing assessments - Page: {Page}, Size: {Size}", query.Page, query.Size);
            
            var result = await _assessmentService.ListAssessmentsAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error listing assessments: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing assessments");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Update assessment information
    /// </summary>
    /// <param name="id">Assessment ID</param>
    /// <param name="request">Update assessment request</param>
    /// <returns>Updated assessment information</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<AssessmentDto>> UpdateAssessment(
        Guid id,
        [FromBody] UpdateAssessmentCommand request)
    {
        try
        {
            request.Id = id;
            _logger.LogInformation("Updating assessment: {AssessmentId}", id);
            
            var result = await _assessmentService.UpdateAssessmentAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Assessment update failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assessment: {AssessmentId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Upload new version of assessment file
    /// </summary>
    /// <param name="id">Assessment ID</param>
    /// <param name="request">New version upload request</param>
    /// <returns>Updated assessment with new version</returns>
    [HttpPost("{id:guid}/versions")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<AssessmentDto>> UploadNewVersion(
        Guid id,
        [FromForm] UploadAssessmentVersionCommand request)
    {
        try
        {
            request.AssessmentId = id;
            _logger.LogInformation("Uploading new version for assessment: {AssessmentId}", id);
            
            var result = await _assessmentService.UploadNewVersionAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Assessment version upload failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading new version for assessment: {AssessmentId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Download assessment PDF file
    /// </summary>
    /// <param name="id">Assessment ID</param>
    /// <param name="version">Optional version number (defaults to current)</param>
    /// <returns>PDF file download</returns>
    [HttpGet("{id:guid}/download")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult> DownloadAssessment(Guid id, [FromQuery] int? version = null)
    {
        try
        {
            _logger.LogInformation("Downloading assessment: {AssessmentId}, Version: {Version}", id, version);
            
            var query = new DownloadAssessmentQuery { Id = id, Version = version };
            var result = await _assessmentService.DownloadAssessmentAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Assessment download failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            var assessment = result.Value;
            return File(
                assessment.Content, 
                assessment.ContentType, 
                assessment.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading assessment: {AssessmentId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Pin assessment to specific school year
    /// </summary>
    /// <param name="id">Assessment ID</param>
    /// <param name="request">Pin to school year request</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/pin")]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult> PinToSchoolYear(
        Guid id,
        [FromBody] PinAssessmentCommand request)
    {
        try
        {
            request.AssessmentId = id;
            _logger.LogInformation("Pinning assessment {AssessmentId} to school year {SchoolYearId}", id, request.SchoolYearId);
            
            var result = await _assessmentService.PinToSchoolYearAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Assessment pinning failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Assessment pinned to school year successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pinning assessment {AssessmentId} to school year", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Unpin assessment from school year
    /// </summary>
    /// <param name="id">Assessment ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}/pin")]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult> UnpinFromSchoolYear(Guid id)
    {
        try
        {
            _logger.LogInformation("Unpinning assessment from school year: {AssessmentId}", id);
            
            var command = new UnpinAssessmentCommand { AssessmentId = id };
            var result = await _assessmentService.UnpinFromSchoolYearAsync(command);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Assessment unpinning failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Assessment unpinned from school year successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpinning assessment from school year: {AssessmentId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Archive an assessment (make immutable)
    /// </summary>
    /// <param name="id">Assessment ID</param>
    /// <param name="request">Archive request</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult> ArchiveAssessment(
        Guid id,
        [FromBody] ArchiveAssessmentCommand request)
    {
        try
        {
            request.AssessmentId = id;
            _logger.LogInformation("Archiving assessment: {AssessmentId}", id);
            
            var result = await _assessmentService.ArchiveAssessmentAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Assessment archiving failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Assessment archived successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving assessment: {AssessmentId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get assessment version history
    /// </summary>
    /// <param name="id">Assessment ID</param>
    /// <returns>List of assessment versions</returns>
    [HttpGet("{id:guid}/versions")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<IEnumerable<AssessmentVersionDto>>> GetVersionHistory(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving version history for assessment: {AssessmentId}", id);
            
            var query = new GetAssessmentVersionHistoryQuery { AssessmentId = id };
            var result = await _assessmentService.GetVersionHistoryAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error retrieving version history for assessment {AssessmentId}: {Error}", id, result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version history for assessment: {AssessmentId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}