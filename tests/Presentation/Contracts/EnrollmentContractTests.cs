using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NorthstarET.Lms.Tests.Presentation.Contracts;

public class EnrollmentContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EnrollmentContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact] public async Task POST_Enrollments_ShouldFail() => Assert.Fail("Enrollment creation endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public async Task GET_Enrollments_ShouldFail() => Assert.Fail("Enrollment query endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public async Task PATCH_EnrollmentStatus_ShouldFail() => Assert.Fail("Enrollment status endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public async Task POST_EnrollmentsBulk_ShouldFail() => Assert.Fail("Bulk enrollment endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public async Task POST_StudentRollover_ShouldFail() => Assert.Fail("Student rollover endpoint not implemented - expected as per BDD-first requirement");
}