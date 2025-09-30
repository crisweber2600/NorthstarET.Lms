using NorthstarET.Lms.Application.Commands.Districts;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Queries.Districts;
using NorthstarET.Lms.Domain.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NorthstarET.Lms.Presentation.Api.Controllers;

[ApiController]
[Route("api/v1/districts")]
[Authorize]
public class DistrictsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DistrictsController> _logger;

    public DistrictsController(IMediator mediator, ILogger<DistrictsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DistrictDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDistrict([FromBody] CreateDistrictCommand command)
    {
        _logger.LogInformation("Creating district with slug: {Slug}", command.Slug);
        
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("already exists") == true 
                ? Conflict(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetDistrictBySlug), new { slug = command.Slug }, result.Value);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(DistrictDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDistrictBySlug(string slug)
    {
        var query = new GetDistrictBySlugQuery { Slug = slug };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DistrictDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDistricts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAllDistrictsQuery { PageNumber = page, PageSize = pageSize };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        
        return Ok(result.Value);
    }

    [HttpPatch("{slug}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDistrictStatus(
        string slug,
        [FromBody] UpdateDistrictStatusCommand command)
    {
        if (slug != command.Slug)
        {
            return BadRequest(new { error = "Slug in URL does not match command" });
        }

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpPatch("{slug}/quotas")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDistrictQuotas(
        string slug,
        [FromBody] UpdateDistrictQuotasCommand command)
    {
        if (slug != command.Slug)
        {
            return BadRequest(new { error = "Slug in URL does not match command" });
        }

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
