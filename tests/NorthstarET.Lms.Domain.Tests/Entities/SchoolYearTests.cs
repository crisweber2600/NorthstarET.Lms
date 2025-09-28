using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Tests.Entities;

public class SchoolYearTests
{
    [Fact]
    public void CreateSchoolYear_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "2024-2025";
        var startDate = new DateTime(2024, 8, 15);
        var endDate = new DateTime(2025, 6, 15);

        // Act
        var schoolYear = new SchoolYear(name, startDate, endDate);

        // Assert
        schoolYear.Name.Should().Be(name);
        schoolYear.StartDate.Should().Be(startDate);
        schoolYear.EndDate.Should().Be(endDate);
        schoolYear.Status.Should().Be(SchoolYearStatus.Planning);
        schoolYear.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateSchoolYear_WithEndDateBeforeStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "2024-2025";
        var startDate = new DateTime(2024, 8, 15);
        var endDate = new DateTime(2024, 6, 15); // Before start date

        // Act & Assert
        var act = () => new SchoolYear(name, startDate, endDate);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*End date must be after start date*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void CreateSchoolYear_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        var startDate = new DateTime(2024, 8, 15);
        var endDate = new DateTime(2025, 6, 15);

        // Act & Assert
        var act = () => new SchoolYear(invalidName, startDate, endDate);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*School year name*required*");
    }

    [Fact]
    public void ActivateSchoolYear_WhenInPlanningStatus_ShouldChangeToActive()
    {
        // Arrange
        var schoolYear = CreateValidSchoolYear();
        var activatedBy = "district-admin-123";

        // Act
        schoolYear.Activate(activatedBy);

        // Assert
        schoolYear.Status.Should().Be(SchoolYearStatus.Active);
        schoolYear.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SchoolYearActivatedEvent>();
    }

    [Fact]
    public void ActivateSchoolYear_WhenAlreadyActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var schoolYear = CreateValidSchoolYear();
        schoolYear.Activate("admin-1");

        // Act & Assert
        var act = () => schoolYear.Activate("admin-2");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already active*");
    }

    [Fact]
    public void ArchiveSchoolYear_WhenActive_ShouldChangeToArchived()
    {
        // Arrange
        var schoolYear = CreateValidSchoolYear();
        schoolYear.Activate("admin-123");
        var archivedBy = "district-admin-456";

        // Act
        schoolYear.Archive(archivedBy);

        // Assert
        schoolYear.Status.Should().Be(SchoolYearStatus.Archived);
        schoolYear.IsReadOnly.Should().BeTrue();
        schoolYear.DomainEvents.Should().HaveCount(2); // Activation + Archive
        var archiveEvent = schoolYear.DomainEvents.Last();
        archiveEvent.Should().BeOfType<SchoolYearArchivedEvent>();
    }

    [Fact]
    public void ArchiveSchoolYear_WhenInPlanning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var schoolYear = CreateValidSchoolYear(); // Still in Planning status

        // Act & Assert
        var act = () => schoolYear.Archive("admin-123");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Only active school years can be archived*");
    }

    [Fact]
    public void IsCurrentYear_WhenDateWithinRange_ShouldReturnTrue()
    {
        // Arrange
        var schoolYear = new SchoolYear(
            "2024-2025",
            new DateTime(2024, 8, 15),
            new DateTime(2025, 6, 15));
        var testDate = new DateTime(2024, 10, 15);

        // Act & Assert
        schoolYear.IsCurrentYear(testDate).Should().BeTrue();
    }

    [Fact]
    public void IsCurrentYear_WhenDateOutsideRange_ShouldReturnFalse()
    {
        // Arrange
        var schoolYear = new SchoolYear(
            "2024-2025", 
            new DateTime(2024, 8, 15),
            new DateTime(2025, 6, 15));
        var testDate = new DateTime(2023, 10, 15);

        // Act & Assert
        schoolYear.IsCurrentYear(testDate).Should().BeFalse();
    }

    [Fact]
    public void DurationInDays_ShouldCalculateCorrectly()
    {
        // Arrange
        var schoolYear = new SchoolYear(
            "2024-2025",
            new DateTime(2024, 8, 15),
            new DateTime(2024, 8, 25)); // 10 days

        // Act & Assert
        schoolYear.DurationInDays.Should().Be(10);
    }

    [Fact]
    public void UpdateDateRange_WhenNotArchived_ShouldUpdateDates()
    {
        // Arrange
        var schoolYear = CreateValidSchoolYear();
        var newStartDate = new DateTime(2024, 8, 20);
        var newEndDate = new DateTime(2025, 6, 20);
        var updatedBy = "admin-123";

        // Act
        schoolYear.UpdateDateRange(newStartDate, newEndDate, updatedBy);

        // Assert
        schoolYear.StartDate.Should().Be(newStartDate);
        schoolYear.EndDate.Should().Be(newEndDate);
        schoolYear.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateDateRange_WhenArchived_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var schoolYear = CreateValidSchoolYear();
        schoolYear.Activate("admin-1");
        schoolYear.Archive("admin-2");

        // Act & Assert
        var act = () => schoolYear.UpdateDateRange(DateTime.Today, DateTime.Today.AddDays(365), "admin-3");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*archived school years are read-only*");
    }

    [Fact]
    public void UpdateDateRange_WithInvalidRange_ShouldThrowArgumentException()
    {
        // Arrange
        var schoolYear = CreateValidSchoolYear();
        var startDate = new DateTime(2024, 8, 15);
        var endDate = new DateTime(2024, 6, 15); // Before start date

        // Act & Assert
        var act = () => schoolYear.UpdateDateRange(startDate, endDate, "admin-123");
        act.Should().Throw<ArgumentException>()
           .WithMessage("*End date must be after start date*");
    }

    private static SchoolYear CreateValidSchoolYear()
    {
        return new SchoolYear(
            "2024-2025",
            new DateTime(2024, 8, 15),
            new DateTime(2025, 6, 15));
    }
}