using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class AssessmentDefinitionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateAssessment()
    {
        // Arrange
        var tenantSlug = "test-district";
        var districtId = Guid.NewGuid();
        var title = "Mathematics Assessment Q1";
        var subject = "Mathematics";
        var gradeLevels = "9,10,11,12";
        var storageUri = "blob://assessments/math-q1.pdf";
        var fileSize = 5 * 1024 * 1024; // 5MB
        var uploadDigest = "sha256:abc123";
        var createdBy = "admin@test.com";

        // Act
        var assessment = new AssessmentDefinition(tenantSlug, districtId, title, subject, 
            gradeLevels, storageUri, fileSize, uploadDigest, createdBy);

        // Assert
        assessment.Should().NotBeNull();
        assessment.DistrictId.Should().Be(districtId);
        assessment.Title.Should().Be(title);
        assessment.Subject.Should().Be(subject);
        assessment.Version.Should().Be(1);
        assessment.FileSize.Should().Be(fileSize);
        assessment.IsPublished.Should().BeFalse();
        assessment.CreatedBy.Should().Be(createdBy);
        assessment.DomainEvents.Should().ContainSingle(e => e is AssessmentDefinitionCreatedEvent);
    }

    [Fact]
    public void Constructor_WithFileSizeExceeding100MB_ShouldThrowException()
    {
        // Arrange
        var tenantSlug = "test-district";
        var districtId = Guid.NewGuid();
        var fileSize = 101 * 1024 * 1024; // 101MB
        var createdBy = "admin@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new AssessmentDefinition(tenantSlug, districtId, "Test", "Math", "9", 
                "blob://test.pdf", fileSize, "sha256:abc", createdBy));
    }

    [Fact]
    public void Publish_WhenNotPublished_ShouldPublishAndRaiseEvent()
    {
        // Arrange
        var assessment = new AssessmentDefinition("test-district", Guid.NewGuid(), 
            "Test Assessment", "Math", "9", "blob://test.pdf", 5000000, "sha256:abc", "admin@test.com");
        var publishedBy = "admin@test.com";

        // Act
        assessment.Publish(publishedBy);

        // Assert
        assessment.IsPublished.Should().BeTrue();
        assessment.DomainEvents.Should().Contain(e => e is AssessmentDefinitionPublishedEvent);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ShouldThrowException()
    {
        // Arrange
        var assessment = new AssessmentDefinition("test-district", Guid.NewGuid(), 
            "Test Assessment", "Math", "9", "blob://test.pdf", 5000000, "sha256:abc", "admin@test.com");
        assessment.Publish("admin@test.com");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            assessment.Publish("admin@test.com"));
    }

    [Fact]
    public void Version_ShouldStartAt1OnCreation()
    {
        // Arrange & Act
        var assessment = new AssessmentDefinition("test-district", Guid.NewGuid(), 
            "Test Assessment", "Math", "9", "blob://test.pdf", 5000000, "sha256:abc", "admin@test.com");

        // Assert
        assessment.Version.Should().Be(1);
    }
}
