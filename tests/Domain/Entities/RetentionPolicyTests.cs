using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class RetentionPolicyTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreatePolicy()
    {
        // Arrange
        var entityType = "Student";
        var retentionYears = 7;
        var gracePeriodDays = 30;
        var effectiveDate = DateTime.UtcNow;
        var createdBy = "admin@test.com";

        // Act
        var policy = new RetentionPolicy(entityType, retentionYears, gracePeriodDays, effectiveDate, createdBy);

        // Assert
        policy.Should().NotBeNull();
        policy.EntityType.Should().Be(entityType);
        policy.RetentionYears.Should().Be(retentionYears);
        policy.GracePeriodDays.Should().Be(gracePeriodDays);
        policy.EffectiveDate.Should().Be(effectiveDate);
        policy.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void Constructor_WithNegativeRetentionYears_ShouldThrowException()
    {
        // Arrange
        var effectiveDate = DateTime.UtcNow;
        var createdBy = "admin@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new RetentionPolicy("Student", -1, 30, effectiveDate, createdBy));
    }

    [Fact]
    public void Constructor_WithEmptyEntityType_ShouldThrowException()
    {
        // Arrange
        var effectiveDate = DateTime.UtcNow;
        var createdBy = "admin@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new RetentionPolicy("", 7, 30, effectiveDate, createdBy));
    }

    [Fact]
    public void OverrideReason_ShouldSupportCustomRetentionPeriods()
    {
        // Arrange
        var overrideReason = "Legal compliance requirement";
        var effectiveDate = DateTime.UtcNow;
        var createdBy = "admin@test.com";

        // Act
        var policy = new RetentionPolicy("Student", 10, 30, effectiveDate, createdBy, overrideReason);

        // Assert
        policy.OverrideReason.Should().Be(overrideReason);
    }
}
