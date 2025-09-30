using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NorthstarET.Lms.Tests.Presentation.Contracts;

public class AssessmentsContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AssessmentsContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact] public void POST_AssessmentDefinitions_ShouldFail() => Assert.Fail("Assessment definition endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public void GET_AssessmentDefinitions_ShouldFail() => Assert.Fail("Assessment definition query endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public void POST_AssessmentUploads_ShouldFail() => Assert.Fail("Assessment upload endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public void GET_AssessmentDownloads_ShouldFail() => Assert.Fail("Assessment download endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public void GET_AssessmentQuota_ShouldFail() => Assert.Fail("Assessment quota endpoint not implemented - expected as per BDD-first requirement");
}