using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthstarET.Lms.Application.Commands.Districts;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.DTOs.Districts;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Application.Queries.Districts;
using NorthstarET.Lms.Application.Services;

namespace NorthstarET.Lms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Always require authentication
public class DistrictsController : ControllerBase
{
    private readonly DistrictService _districtService;
    private readonly ILogger<DistrictsController> _logger;

    public DistrictsController(
        DistrictService districtService,
        ILogger<DistrictsController> logger)
    {
        _districtService = districtService ?? throw new ArgumentNullException(nameof(districtService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new school district
    /// </summary>
    /// <param name="request">District creation request</param>
    /// <returns>Created district information</returns>
    [HttpPost]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult<Application.DTOs.Districts.DistrictDto>> CreateDistrict(
        [FromBody] CreateDistrictCommand request)
    {
        try
        {
            _logger.LogInformation("Creating district with slug: {Slug}", request.Slug);
            
            var result = await _districtService.CreateDistrictAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("District creation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("District created successfully: {DistrictId}", result.Value.Id);
            return Created($"/api/v1/districts/{result.Value.Id}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating district with slug: {Slug}", request.Slug);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get a specific district by ID
    /// </summary>
    /// <param name="id">District ID</param>
    /// <returns>District information</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "PlatformAdmin,DistrictAdmin")]
    public async Task<ActionResult<Application.DTOs.Districts.DistrictDto>> GetDistrict(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving district: {DistrictId}", id);
            
            var query = new GetDistrictQuery { Id = id };
            var result = await _districtService.GetDistrictAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("District not found: {DistrictId}", id);
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving district: {DistrictId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// List districts with pagination and filtering
    /// </summary>
    /// <param name="query">List districts query parameters</param>
    /// <returns>Paginated list of districts</returns>
    [HttpGet]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult<PagedResult<DistrictSummaryDto>>> ListDistricts(
        [FromQuery] ListDistrictsQuery query)
    {
        try
        {
            _logger.LogInformation("Listing districts - Page: {Page}, Size: {Size}", query.Page, query.Size);
            
            var result = await _districtService.ListDistrictsAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error listing districts: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing districts");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Update district information
    /// </summary>
    /// <param name="id">District ID</param>
    /// <param name="request">Update district request</param>
    /// <returns>Updated district information</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult<Application.DTOs.Districts.DistrictDto>> UpdateDistrict(
        Guid id,
        [FromBody] UpdateDistrictCommand request)
    {
        try
        {
            request.Id = id;
            _logger.LogInformation("Updating district: {DistrictId}", id);
            
            var result = await _districtService.UpdateDistrictAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("District update failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating district: {DistrictId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Suspend a district
    /// </summary>
    /// <param name="id">District ID</param>
    /// <param name="request">Suspension request details</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult> SuspendDistrict(
        Guid id,
        [FromBody] SuspendDistrictCommand request)
    {
        try
        {
            request.Id = id;
            _logger.LogInformation("Suspending district: {DistrictId}", id);
            
            var result = await _districtService.SuspendDistrictAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("District suspension failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "District suspended successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending district: {DistrictId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Reactivate a suspended district
    /// </summary>
    /// <param name="id">District ID</param>
    /// <param name="request">Reactivation request details</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult> ReactivateDistrict(
        Guid id,
        [FromBody] ReactivateDistrictCommand request)
    {
        try
        {
            request.Id = id;
            _logger.LogInformation("Reactivating district: {DistrictId}", id);
            
            var result = await _districtService.ReactivateDistrictAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("District reactivation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "District reactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating district: {DistrictId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Delete a district (with retention compliance check)
    /// </summary>
    /// <param name="id">District ID</param>
    /// <returns>Deletion status or compliance check result</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult> DeleteDistrict(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting district: {DistrictId}", id);
            
            var command = new DeleteDistrictCommand { Id = id };
            var result = await _districtService.DeleteDistrictAsync(command);
            
            if (!result.IsSuccess)
            {
                if (result.Error.Contains("retention") || result.Error.Contains("legal hold"))
                {
                    return Conflict(new { 
                        canDelete = false, 
                        error = result.Error,
                        message = "Cannot delete district due to retention policy or legal hold constraints" 
                    });
                }
                
                _logger.LogWarning("District deletion failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { 
                canDelete = true,
                message = "District scheduled for deletion after retention period compliance"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting district: {DistrictId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get district quota status
    /// </summary>
    /// <param name="id">District ID</param>
    /// <returns>Current quota utilization</returns>
    [HttpGet("{id:guid}/quota-status")]
    [Authorize(Roles = "PlatformAdmin,DistrictAdmin")]
    public async Task<ActionResult<DistrictQuotaStatusDto>> GetQuotaStatus(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving quota status for district: {DistrictId}", id);
            
            var query = new GetDistrictQuotaStatusQuery { DistrictId = id };
            var result = await _districtService.GetQuotaStatusAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error retrieving quota status: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quota status for district: {DistrictId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Update district quotas
    /// </summary>
    /// <param name="id">District ID</param>
    /// <param name="request">Updated quota values</param>
    /// <returns>Success result</returns>
    [HttpPatch("{id:guid}/quotas")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult> UpdateQuotas(
        Guid id,
        [FromBody] UpdateDistrictQuotasCommand request)
    {
        try
        {
            request.Id = id;
            _logger.LogInformation("Updating quotas for district: {DistrictId}", id);
            
            var result = await _districtService.UpdateQuotasAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Quota update failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Quotas updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quotas for district: {DistrictId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}