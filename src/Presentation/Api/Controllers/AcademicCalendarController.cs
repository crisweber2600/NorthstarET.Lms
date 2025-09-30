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
        
        var calendar = await _calendarService.CreateCalendarAsync(
            request.SchoolYearId,
            User.Identity?.Name ?? "system");

        return CreatedAtAction(nameof(GetAcademicCalendar), 
            new { tenant, id = calendar.Id }, new { id = calendar.Id });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAcademicCalendar(string tenant, Guid id)
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
        await _calendarService.AddInstructionalDaysAsync(
            id,
            dates,
            User.Identity?.Name ?? "system");

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
        await _calendarService.AddClosuresAsync(
            id,
            new List<DateTime> { request.Date },
            request.Reason,
            User.Identity?.Name ?? "system");

        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishCalendar(string tenant, Guid id)
    {
        await _calendarService.PublishCalendarAsync(
            id,
            User.Identity?.Name ?? "system");

        return NoContent();
    }
}

public record CreateAcademicCalendarRequest(
    Guid SchoolYearId,
    Guid SchoolId,
    DateTime FirstDayOfSchool,
    DateTime LastDayOfSchool);

public record AddClosureRequest(DateTime Date, string Reason);
