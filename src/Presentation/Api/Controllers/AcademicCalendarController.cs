using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NorthstarET.Lms.Presentation.Api.Controllers;

[ApiController]
[Route("api/v1/{tenant}/calendar")]
[Authorize]
public class AcademicCalendarController : ControllerBase
{
    private readonly IAcademicCalendarService _calendarService;
    private readonly ILogger<AcademicCalendarController> _logger;

    public AcademicCalendarController(
        IAcademicCalendarService calendarService,
        ILogger<AcademicCalendarController> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAcademicCalendar(
        string tenant,
        [FromBody] CreateAcademicCalendarRequest request)
    {
        _logger.LogInformation("Creating academic calendar for school year {SchoolYearId}", 
            request.SchoolYearId);
        
        var result = await _calendarService.CreateAcademicCalendarAsync(
            request.SchoolYearId,
            request.SchoolId,
            request.FirstDayOfSchool,
            request.LastDayOfSchool);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetAcademicCalendar), 
            new { tenant, id = result.Value }, new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAcademicCalendar(string tenant, Guid id)
    {
        // Placeholder - would need a GetAcademicCalendarQuery
        return NotFound(new { error = "Calendar retrieval not implemented" });
    }

    [HttpPost("{id:guid}/instructional-days")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddInstructionalDays(
        string tenant,
        Guid id,
        [FromBody] List<DateTime> dates)
    {
        var result = await _calendarService.AddInstructionalDaysAsync(id, dates);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/closures")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddClosure(
        string tenant,
        Guid id,
        [FromBody] AddClosureRequest request)
    {
        var result = await _calendarService.AddClosureAsync(
            id,
            request.Date,
            request.Reason);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishCalendar(string tenant, Guid id)
    {
        var result = await _calendarService.PublishCalendarAsync(id);
        
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found") == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}

public record CreateAcademicCalendarRequest(
    Guid SchoolYearId,
    Guid SchoolId,
    DateTime FirstDayOfSchool,
    DateTime LastDayOfSchool);

public record AddClosureRequest(DateTime Date, string Reason);
