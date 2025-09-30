using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class BulkJobTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateBulkJob()
    {
        // Arrange
        var tenantSlug = "test-district";
        var jobType = "StudentRollover";
        var requestedBy = "admin@test.com";
        var totalItems = 1000;

        // Act
        var bulkJob = new BulkJob(tenantSlug, jobType, requestedBy, totalItems);

        // Assert
        bulkJob.Should().NotBeNull();
        bulkJob.JobType.Should().Be(jobType);
        bulkJob.RequestedBy.Should().Be(requestedBy);
        bulkJob.TotalItems.Should().Be(totalItems);
        bulkJob.Status.Should().Be("Pending");
        bulkJob.IsDryRun.Should().BeFalse();
        bulkJob.DomainEvents.Should().ContainSingle(e => e is BulkJobCreatedEvent);
    }

    [Fact]
    public void Start_WhenPending_ShouldStartJobAndRaiseEvent()
    {
        // Arrange
        var bulkJob = new BulkJob("test-district", "StudentRollover", "admin@test.com", 1000);

        // Act
        bulkJob.Start();

        // Assert
        bulkJob.Status.Should().Be("Running");
        bulkJob.DomainEvents.Should().Contain(e => e is BulkJobStartedEvent);
    }

    [Fact]
    public void Start_WhenNotPending_ShouldThrowException()
    {
        // Arrange
        var bulkJob = new BulkJob("test-district", "StudentRollover", "admin@test.com", 1000);
        bulkJob.Start();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => bulkJob.Start());
    }

    [Fact]
    public void RecordSuccess_ShouldIncrementSuccessCountAndProgress()
    {
        // Arrange
        var bulkJob = new BulkJob("test-district", "StudentRollover", "admin@test.com", 1000);
        bulkJob.Start();

        // Act
        bulkJob.RecordSuccess();
        bulkJob.RecordSuccess();

        // Assert
        bulkJob.SuccessCount.Should().Be(2);
        bulkJob.Progress.Should().Be(2);
    }

    [Fact]
    public void RecordFailure_ShouldIncrementFailureCountAndProgress()
    {
        // Arrange
        var bulkJob = new BulkJob("test-district", "StudentRollover", "admin@test.com", 1000);
        bulkJob.Start();

        // Act
        bulkJob.RecordFailure("Error processing item");

        // Assert
        bulkJob.FailureCount.Should().Be(1);
        bulkJob.Progress.Should().Be(1);
    }

    [Fact]
    public void RecordFailure_WhenThresholdExceeded_ShouldFailJob()
    {
        // Arrange
        var bulkJob = new BulkJob("test-district", "StudentRollover", "admin@test.com", 10, 
            "ThresholdMode", 0.1m);
        bulkJob.Start();

        // Act
        bulkJob.RecordFailure("Error 1");
        bulkJob.RecordFailure("Error 2");

        // Assert
        bulkJob.Status.Should().Be("Failed");
    }

    [Fact]
    public void Complete_WhenRunning_ShouldCompleteJobAndRaiseEvent()
    {
        // Arrange
        var bulkJob = new BulkJob("test-district", "StudentRollover", "admin@test.com", 10);
        bulkJob.Start();
        for (int i = 0; i < 10; i++) bulkJob.RecordSuccess();

        // Act
        bulkJob.Complete();

        // Assert
        bulkJob.Status.Should().Be("Completed");
        bulkJob.CompletedAt.Should().NotBeNull();
        bulkJob.DomainEvents.Should().Contain(e => e is BulkJobCompletedEvent);
    }

    [Fact]
    public void Fail_WithErrorDetails_ShouldFailJobAndRaiseEvent()
    {
        // Arrange
        var bulkJob = new BulkJob("test-district", "StudentRollover", "admin@test.com", 10);
        bulkJob.Start();
        var errorDetails = "Critical error occurred";

        // Act
        bulkJob.Fail(errorDetails);

        // Assert
        bulkJob.Status.Should().Be("Failed");
        bulkJob.ErrorDetails.Should().Be(errorDetails);
        bulkJob.CompletedAt.Should().NotBeNull();
        bulkJob.DomainEvents.Should().Contain(e => e is BulkJobFailedEvent);
    }

    [Fact]
    public void IsDryRun_ShouldSupportDryRunMode()
    {
        // Arrange & Act
        var bulkJob = new BulkJob("test-district", "StudentRollover", "admin@test.com", 10, 
            "StopOnError", 0.1m, true);

        // Assert
        bulkJob.IsDryRun.Should().BeTrue();
    }
}
