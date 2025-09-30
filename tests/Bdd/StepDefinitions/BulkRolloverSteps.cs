using Reqnroll;
using FluentAssertions;

namespace NorthstarET.Lms.Tests.Bdd.StepDefinitions;

[Binding]
public class BulkRolloverSteps
{
    [Given(@"school year ""([^""]*)"" is archived")]
    public void GivenSchoolYearIsArchived(string schoolYear)
    {
        Assert.Fail("School year archiving not implemented - expected as per BDD-first requirement");
    }

    [Given(@"school year ""([^""]*)"" is active")]
    public void GivenSchoolYearIsActive(string schoolYear)
    {
        Assert.Fail("School year status management not implemented - expected as per BDD-first requirement");
    }

    [Given(@"the following students exist in ""([^""]*)"":")]
    public void GivenTheFollowingStudentsExistIn(string schoolYear, DataTable studentTable)
    {
        Assert.Fail("Student enrollment history not implemented - expected as per BDD-first requirement");
    }

    [When(@"I request a rollover preview for Grade (\d+) students to Grade (\d+)")]
    public void WhenIRequestARolloverPreviewForGradeStudentsToGrade(int fromGrade, int toGrade)
    {
        Assert.Fail("Bulk rollover preview not implemented - expected as per BDD-first requirement");
    }

    [When(@"I execute the bulk rollover with ""([^""]*)"" error handling")]
    public void WhenIExecuteTheBulkRolloverWithErrorHandling(string errorHandlingMode)
    {
        Assert.Fail("Bulk rollover execution not implemented - expected as per BDD-first requirement");
    }

    [Then(@"the preview should show:")]
    public void ThenThePreviewShouldShow(DataTable previewTable)
    {
        Assert.Fail("Rollover preview display not implemented - expected as per BDD-first requirement");
    }

    [Then(@"no actual data changes should occur")]
    public void ThenNoActualDataChangesShouldOccur()
    {
        Assert.Fail("Preview mode validation not implemented - expected as per BDD-first requirement");
    }

    [Then(@"a BulkRolloverPreviewEvent should be raised")]
    public void ThenABulkRolloverPreviewEventShouldBeRaised()
    {
        Assert.Fail("Bulk rollover events not implemented - expected as per BDD-first requirement");
    }
}