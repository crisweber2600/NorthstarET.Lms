using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NorthstarET.Lms.Tests.Presentation.Contracts;

public class ComplianceContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ComplianceContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact] public void POST_LegalHolds_ShouldFail() => Assert.Fail("Legal hold endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public void GET_RetentionPolicies_ShouldFail() => Assert.Fail("Retention policy endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public void GET_AuditTrail_ShouldFail() => Assert.Fail("Audit trail endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public void POST_DataExports_ShouldFail() => Assert.Fail("Data export endpoint not implemented - expected as per BDD-first requirement");
    [Fact] public void GET_ComplianceReports_ShouldFail() => Assert.Fail("Compliance report endpoint not implemented - expected as per BDD-first requirement");
}