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
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;
    private readonly RoleAuthorizationService _roleAuthService;
    private readonly ILogger<StaffController> _logger;

    public StaffController(
        IStaffService staffService,
        RoleAuthorizationService roleAuthService,
        ILogger<StaffController> logger)
    {
        _staffService = staffService ?? throw new ArgumentNullException(nameof(staffService));
        _roleAuthService = roleAuthService ?? throw new ArgumentNullException(nameof(roleAuthService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new staff member
    /// </summary>
    /// <param name="request">Staff creation request</param>
    /// <returns>Created staff information</returns>
    [HttpPost]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<StaffDto>> CreateStaff(
        [FromBody] CreateStaffCommand request)
    {
        try
        {
            _logger.LogInformation("Creating staff with employee number: {EmployeeNumber}", request.EmployeeNumber);
            
            var result = await _staffService.CreateStaffAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Staff creation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Staff created successfully: {StaffId}", result.Value.UserId);
            return Created($"/api/v1/staff/{result.Value.UserId}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating staff with employee number: {EmployeeNumber}", request.EmployeeNumber);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get a specific staff member by ID
    /// </summary>
    /// <param name="userId">Staff user ID</param>
    /// <returns>Staff information with role assignments</returns>
    [HttpGet("{userId:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<StaffDetailDto>> GetStaff(Guid userId)
    {
        try
        {
            _logger.LogInformation("Retrieving staff: {StaffId}", userId);
            
            var query = new GetStaffQuery { UserId = userId };
            var result = await _staffService.GetStaffAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Staff not found: {StaffId}", userId);
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving staff: {StaffId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// List staff members with pagination and filtering
    /// </summary>
    /// <param name="query">List staff query parameters</param>
    /// <returns>Paginated list of staff members</returns>
    [HttpGet]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<PagedResult<StaffSummaryDto>>> ListStaff(
        [FromQuery] ListStaffQuery query)
    {
        try
        {
            _logger.LogInformation("Listing staff - Page: {Page}, Size: {Size}", query.Page, query.Size);
            
            var result = await _staffService.ListStaffAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error listing staff: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing staff");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Update staff information
    /// </summary>
    /// <param name="userId">Staff user ID</param>
    /// <param name="request">Update staff request</param>
    /// <returns>Updated staff information</returns>
    [HttpPut("{userId:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<StaffDto>> UpdateStaff(
        Guid userId,
        [FromBody] UpdateStaffCommand request)
    {
        try
        {
            request.UserId = userId;
            _logger.LogInformation("Updating staff: {StaffId}", userId);
            
            var result = await _staffService.UpdateStaffAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Staff update failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating staff: {StaffId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Assign role to staff member
    /// </summary>
    /// <param name="userId">Staff user ID</param>
    /// <param name="request">Role assignment request</param>
    /// <returns>Created role assignment</returns>
    [HttpPost("{userId:guid}/roles")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<RoleAssignmentDto>> AssignRole(
        Guid userId,
        [FromBody] AssignRoleCommand request)
    {
        try
        {
            request.UserId = userId;
            _logger.LogInformation("Assigning role {RoleId} to staff {StaffId}", request.RoleDefinitionId, userId);
            
            var result = await _roleAuthService.AssignRoleAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Role assignment failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Role assigned successfully: {AssignmentId}", result.Value.Id);
            return Created($"/api/v1/staff/{userId}/roles/{result.Value.Id}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to staff {StaffId}", request.RoleDefinitionId, userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Remove role assignment from staff member
    /// </summary>
    /// <param name="userId">Staff user ID</param>
    /// <param name="assignmentId">Role assignment ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{userId:guid}/roles/{assignmentId:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult> RemoveRoleAssignment(
        Guid userId,
        Guid assignmentId)
    {
        try
        {
            _logger.LogInformation("Removing role assignment {AssignmentId} from staff {StaffId}", assignmentId, userId);
            
            var command = new RemoveRoleAssignmentCommand { AssignmentId = assignmentId, UserId = userId };
            var result = await _roleAuthService.RemoveRoleAssignmentAsync(command);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Role assignment removal failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Role assignment removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role assignment {AssignmentId} from staff {StaffId}", assignmentId, userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Terminate staff member
    /// </summary>
    /// <param name="userId">Staff user ID</param>
    /// <param name="request">Termination request</param>
    /// <returns>Success result</returns>
    [HttpPost("{userId:guid}/terminate")]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult> TerminateStaff(
        Guid userId,
        [FromBody] TerminateStaffCommand request)
    {
        try
        {
            request.UserId = userId;
            _logger.LogInformation("Terminating staff: {StaffId}", userId);
            
            var result = await _staffService.TerminateStaffAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Staff termination failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Staff terminated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating staff: {StaffId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get role assignments for staff member
    /// </summary>
    /// <param name="userId">Staff user ID</param>
    /// <param name="query">Query parameters</param>
    /// <returns>List of role assignments</returns>
    [HttpGet("{userId:guid}/roles")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<PagedResult<RoleAssignmentDto>>> GetRoleAssignments(
        Guid userId,
        [FromQuery] GetStaffRoleAssignmentsQuery query)
    {
        try
        {
            query.UserId = userId;
            _logger.LogInformation("Retrieving role assignments for staff: {StaffId}", userId);
            
            var result = await _roleAuthService.GetUserRoleAssignmentsAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error retrieving role assignments for staff {StaffId}: {Error}", userId, result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role assignments for staff: {StaffId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}