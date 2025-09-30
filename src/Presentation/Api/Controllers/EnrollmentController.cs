using NorthstarET.Lms.Application.Commands.Enrollments;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Domain.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NorthstarET.Lms.Presentation.Api.Controllers;

[ApiController]
[Route("api/v1/{tenant}/enrollments")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EnrollmentController> _logger;

    public EnrollmentController(IMediator mediator, ILogger<EnrollmentController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnrollStudent(string tenant, [FromBody] EnrollStudentCommand command)
    {
        _logger.LogInformation("Enrolling student {StudentId} in class {ClassId}", 
            command.StudentId, command.ClassId);
        
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetEnrollment), 
            new { tenant, id = result.Value.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollment(string tenant, Guid id)
    {
        // This would need a GetEnrollmentByIdQuery in Application layer
        // For now, return NotFound as placeholder
        return NotFound(new { error = "Enrollment retrieval not implemented" });
    }

    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawStudent(
        string tenant,
        Guid id,
        [FromBody] WithdrawStudentCommand command)
    {
        if (id != command.EnrollmentId)
        {
            return BadRequest(new { error = "Enrollment ID in URL does not match command" });
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

    [HttpPost("{id:guid}/transfer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferStudent(
        string tenant,
        Guid id,
        [FromBody] TransferStudentCommand command)
    {
        if (id != command.EnrollmentId)
        {
            return BadRequest(new { error = "Enrollment ID in URL does not match command" });
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
