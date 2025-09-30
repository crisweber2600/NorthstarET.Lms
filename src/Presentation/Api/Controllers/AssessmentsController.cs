using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NorthstarET.Lms.Presentation.Api.Controllers;

[ApiController]
[Route("api/v1/{tenant}/assessments")]
[Authorize]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;
    private readonly ILogger<AssessmentsController> _logger;

    public AssessmentsController(
        IAssessmentService assessmentService,
        ILogger<AssessmentsController> logger)
    {
        _assessmentService = assessmentService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadAssessment(
        string tenant,
        [FromForm] IFormFile file,
        [FromForm] string title,
        [FromForm] string subject,
        [FromForm] string gradeLevels,
        [FromForm] Guid districtId)
    {
        if (file.Length > 100 * 1024 * 1024) // 100MB limit
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, 
                new { error = "File size exceeds 100MB limit" });
        }

        _logger.LogInformation("Uploading assessment {Title} for district {DistrictId}", 
            title, districtId);
        
        using var stream = file.OpenReadStream();
        var result = await _assessmentService.UploadAssessmentFileAsync(
            districtId,
            title,
            subject,
            gradeLevels,
            stream,
            file.FileName);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("quota") == true
                ? StatusCode(StatusCodes.Status507InsufficientStorage, new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetAssessment), 
            new { tenant, id = result.Value }, new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssessment(string tenant, Guid id)
    {
        // Placeholder - would need a GetAssessmentQuery
        return NotFound(new { error = "Assessment retrieval not implemented" });
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishAssessment(string tenant, Guid id)
    {
        var result = await _assessmentService.PublishAssessmentAsync(id);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/verify")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyAssessmentIntegrity(string tenant, Guid id)
    {
        var result = await _assessmentService.VerifyAssessmentIntegrityAsync(id);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(new { isValid = result.Value });
    }

    [HttpGet("quota")]
    [ProducesResponseType(typeof(QuotaInfo), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuotaUsage(string tenant, [FromQuery] Guid districtId)
    {
        var result = await _assessmentService.GetQuotaUsageAsync(districtId);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}

public record QuotaInfo(long UsedBytes, long LimitBytes, double PercentUsed);
