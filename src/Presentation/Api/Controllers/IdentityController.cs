using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NorthstarET.Lms.Presentation.Api.Controllers;

[ApiController]
[Route("api/v1/{tenant}/identity")]
[Authorize]
public class IdentityController : ControllerBase
{
    private readonly IIdentityMappingService _identityService;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(IIdentityMappingService identityService, ILogger<IdentityController> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    [HttpPost("map")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MapExternalIdentity(
        string tenant,
        [FromBody] MapExternalIdentityRequest request)
    {
        _logger.LogInformation("Mapping external identity from issuer {Issuer}", request.Issuer);
        
        var result = await _identityService.MapExternalIdentityAsync(
            request.ExternalId,
            request.Issuer,
            request.Email,
            request.DisplayName);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("already mapped") == true
                ? Conflict(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(new { internalUserId = result.Value });
    }

    [HttpGet("lookup/{externalId}")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LookupInternalUserId(string tenant, string externalId)
    {
        var result = await _identityService.GetInternalUserIdAsync(externalId);
        
        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(new { internalUserId = result.Value });
    }

    [HttpPost("{internalUserId:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendIdentity(string tenant, Guid internalUserId)
    {
        var result = await _identityService.SuspendIdentityMappingAsync(internalUserId);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpPost("sync-from-graph")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SyncFromMicrosoftGraph(string tenant)
    {
        _logger.LogInformation("Starting Microsoft Graph sync for tenant {Tenant}", tenant);
        
        var result = await _identityService.SyncFromMicrosoftGraphAsync();
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { syncedCount = result.Value });
    }
}

public record MapExternalIdentityRequest(
    string ExternalId,
    string Issuer,
    string Email,
    string DisplayName);
