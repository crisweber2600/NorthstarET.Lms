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
        
        var mapping = await _identityService.CreateMappingAsync(
            request.Issuer,
            request.ExternalId,
            Guid.NewGuid(), // TODO: Get from user creation service
            User.Identity?.Name ?? "system");
        
        return Ok(new { internalUserId = mapping.InternalUserId });
    }

    [HttpGet("lookup/{issuer}/{externalId}")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LookupInternalUserId(string tenant, string issuer, string externalId)
    {
        var userId = await _identityService.ResolveInternalUserIdAsync(issuer, externalId);
        
        if (userId == null)
        {
            return NotFound(new { error = "Identity mapping not found" });
        }

        return Ok(new { internalUserId = userId.Value });
    }

    [HttpPost("{mappingId:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendIdentity(string tenant, Guid mappingId, [FromBody] SuspendIdentityRequest request)
    {
        await _identityService.SuspendMappingAsync(
            mappingId,
            request.SuspendedUntil,
            request.Reason,
            User.Identity?.Name ?? "system");

        return NoContent();
    }

    [HttpPost("sync-from-graph")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SyncFromMicrosoftGraph(string tenant)
    {
        _logger.LogInformation("Starting Microsoft Graph sync for tenant {Tenant}", tenant);
        
        // TODO: Implement Graph sync logic
        return Ok(new { syncedCount = 0 });
    }
}

public record MapExternalIdentityRequest(
    string ExternalId,
    string Issuer,
    string Email,
    string DisplayName);

public record SuspendIdentityRequest(
    DateTime SuspendedUntil,
    string Reason);
