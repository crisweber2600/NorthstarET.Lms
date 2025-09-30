using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NorthstarET.Lms.Presentation.Api.Controllers;

[ApiController]
[Route("api/v1/{tenant}/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleManagementService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleManagementService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    [HttpPost("assign")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRole(
        string tenant,
        [FromBody] AssignRoleRequest request)
    {
        _logger.LogInformation("Assigning role {RoleId} to user {UserId}", 
            request.RoleDefinitionId, request.UserId);
        
        var result = await _roleService.AssignRoleAsync(
            request.UserId,
            request.RoleDefinitionId,
            request.SchoolId,
            request.ClassId,
            request.SchoolYearId);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetRoleAssignment), 
            new { tenant, id = result.Value }, new { id = result.Value });
    }

    [HttpGet("assignments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleAssignment(string tenant, Guid id)
    {
        // Placeholder - would need a GetRoleAssignmentQuery
        return NotFound(new { error = "Role assignment retrieval not implemented" });
    }

    [HttpPost("check-permission")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckPermission(
        string tenant,
        [FromBody] CheckPermissionRequest request)
    {
        var result = await _roleService.HasPermissionAsync(
            request.UserId,
            request.Permission,
            request.ResourceId);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { hasPermission = result.Value });
    }

    [HttpDelete("assignments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeRole(string tenant, Guid id)
    {
        var result = await _roleService.RevokeRoleAsync(id);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}

public record AssignRoleRequest(
    Guid UserId,
    Guid RoleDefinitionId,
    Guid? SchoolId,
    Guid? ClassId,
    Guid? SchoolYearId);

public record CheckPermissionRequest(
    Guid UserId,
    string Permission,
    Guid? ResourceId);
