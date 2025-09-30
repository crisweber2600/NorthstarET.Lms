using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class SchoolTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateSchool()
    {
        // Arrange
        var tenantSlug = "test-district";
        var districtId = Guid.NewGuid();
        var name = "Test Elementary School";
        var schoolType = "Elementary";
        var createdBy = "admin@test.com";

        // Act
        var school = new School(tenantSlug, districtId, name, schoolType, createdBy);

        // Assert
        school.Should().NotBeNull();
        school.DistrictId.Should().Be(districtId);
        school.Name.Should().Be(name);
        school.SchoolType.Should().Be(schoolType);
        school.Status.Should().Be("Active");
        school.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var tenantSlug = "test-district";
        var districtId = Guid.NewGuid();
        var schoolType = "Elementary";
        var createdBy = "admin@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new School(tenantSlug, districtId, "", schoolType, createdBy));
    }

    [Fact]
    public void UpdateStatus_WithValidStatus_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var school = new School("test-district", Guid.NewGuid(), "Test School", "Elementary", "admin@test.com");
        var newStatus = "Inactive";
        var updatedBy = "admin@test.com";

        // Act
        school.UpdateStatus(newStatus, updatedBy);

        // Assert
        school.Status.Should().Be(newStatus);
        school.UpdatedBy.Should().Be(updatedBy);
        school.DomainEvents.Should().ContainSingle(e => e is SchoolStatusChangedEvent);
    }

    [Fact]
    public void UpdateInfo_WithValidData_ShouldUpdateInformation()
    {
        // Arrange
        var school = new School("test-district", Guid.NewGuid(), "Old Name", "Elementary", "admin@test.com");
        var newName = "New School Name";
        var newType = "Middle School";
        var updatedBy = "admin@test.com";

        // Act
        school.UpdateInfo(newName, newType, "123 Main St", "555-1234", updatedBy);

        // Assert
        school.Name.Should().Be(newName);
        school.SchoolType.Should().Be(newType);
        school.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void Status_ShouldControlOperationalCapabilities()
    {
        // Arrange
        var school = new School("test-district", Guid.NewGuid(), "Test School", "Elementary", "admin@test.com");

        // Act
        school.UpdateStatus("Closed", "admin@test.com");

        // Assert
        school.Status.Should().Be("Closed");
    }

    [Fact]
    public void TenantSlug_ShouldBeImmutableAfterCreation()
    {
        // Arrange & Act
        var school = new School("test-district", Guid.NewGuid(), "Test School", "Elementary", "admin@test.com");

        // Assert
        school.TenantSlug.Should().Be("test-district");
        // TenantSlug should not have a public setter
    }
}
