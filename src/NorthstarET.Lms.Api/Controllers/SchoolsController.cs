using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthstarET.Lms.Application.Commands;
using NorthstarET.Lms.Application.Queries;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Application.Services;

namespace NorthstarET.Lms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Always require authentication
public class SchoolsController : ControllerBase
{
    private readonly ISchoolService _schoolService;
    private readonly ILogger<SchoolsController> _logger;

    public SchoolsController(
        ISchoolService schoolService,
        ILogger<SchoolsController> logger)
    {
        _schoolService = schoolService ?? throw new ArgumentNullException(nameof(schoolService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new school
    /// </summary>
    /// <param name="request">School creation request</param>
    /// <returns>Created school information</returns>
    [HttpPost]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult<SchoolDto>> CreateSchool(
        [FromBody] CreateSchoolCommand request)
    {
        try
        {
            _logger.LogInformation("Creating school with code: {SchoolCode}", request.Code);
            
            var result = await _schoolService.CreateSchoolAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("School creation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("School created successfully: {SchoolId}", result.Value.Id);
            return Created($"/api/v1/schools/{result.Value.Id}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating school with code: {SchoolCode}", request.Code);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get a specific school by ID
    /// </summary>
    /// <param name="id">School ID</param>
    /// <returns>School information</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<SchoolDto>> GetSchool(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving school: {SchoolId}", id);
            
            var query = new GetSchoolQuery { Id = id };
            var result = await _schoolService.GetSchoolAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("School not found: {SchoolId}", id);
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving school: {SchoolId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// List schools with pagination and filtering
    /// </summary>
    /// <param name="query">List schools query parameters</param>
    /// <returns>Paginated list of schools</returns>
    [HttpGet]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<PagedResult<SchoolSummaryDto>>> ListSchools(
        [FromQuery] ListSchoolsQuery query)
    {
        try
        {
            _logger.LogInformation("Listing schools - Page: {Page}, Size: {Size}", query.Page, query.Size);
            
            var result = await _schoolService.ListSchoolsAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error listing schools: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing schools");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Update school information
    /// </summary>
    /// <param name="id">School ID</param>
    /// <param name="request">Update school request</param>
    /// <returns>Updated school information</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult<SchoolDto>> UpdateSchool(
        Guid id,
        [FromBody] UpdateSchoolCommand request)
    {
        try
        {
            request.Id = id;
            _logger.LogInformation("Updating school: {SchoolId}", id);
            
            var result = await _schoolService.UpdateSchoolAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("School update failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating school: {SchoolId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Deactivate a school
    /// </summary>
    /// <param name="id">School ID</param>
    /// <param name="request">Deactivation request</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult> DeactivateSchool(
        Guid id,
        [FromBody] DeactivateSchoolCommand request)
    {
        try
        {
            request.Id = id;
            _logger.LogInformation("Deactivating school: {SchoolId}", id);
            
            var result = await _schoolService.DeactivateSchoolAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("School deactivation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "School deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating school: {SchoolId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get classes for a specific school
    /// </summary>
    /// <param name="id">School ID</param>
    /// <param name="query">Query parameters for class listing</param>
    /// <returns>List of classes in the school</returns>
    [HttpGet("{id:guid}/classes")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<PagedResult<ClassSummaryDto>>> GetSchoolClasses(
        Guid id,
        [FromQuery] GetSchoolClassesQuery query)
    {
        try
        {
            query.SchoolId = id;
            _logger.LogInformation("Retrieving classes for school: {SchoolId}", id);
            
            var result = await _schoolService.GetSchoolClassesAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error retrieving classes for school {SchoolId}: {Error}", id, result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving classes for school: {SchoolId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}