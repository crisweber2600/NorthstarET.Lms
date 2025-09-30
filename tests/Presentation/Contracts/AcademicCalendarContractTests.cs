using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NorthstarET.Lms.Tests.Presentation.Contracts;

public class AcademicCalendarContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AcademicCalendarContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact] public async Task POST_AcademicCalendars_ShouldFail() => Assert.Fail("Academic calendar creation endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public async Task GET_AcademicCalendars_ShouldFail() => Assert.Fail("Academic calendar query endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public async Task PUT_AcademicCalendars_ShouldFail() => Assert.Fail("Academic calendar update endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public async Task POST_AcademicCalendarClosures_ShouldFail() => Assert.Fail("Calendar closure endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public async Task GET_InstructionalDays_ShouldFail() => Assert.Fail("Instructional days calculation endpoint not implemented - expected as per BDD-first requirement");
}