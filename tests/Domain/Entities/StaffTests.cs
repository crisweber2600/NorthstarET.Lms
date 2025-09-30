using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class StaffTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateStaff()
    {
        // Arrange
        var tenantSlug = "test-district";
        var userId = Guid.NewGuid();
        var firstName = "John";
        var lastName = "Doe";
        var email = "john.doe@test.com";
        var hireDate = DateTime.UtcNow;
        var createdBy = "admin@test.com";

        // Act
        var staff = new Staff(tenantSlug, userId, firstName, lastName, email, hireDate, createdBy);

        // Assert
        staff.Should().NotBeNull();
        staff.UserId.Should().Be(userId);
        staff.FirstName.Should().Be(firstName);
        staff.LastName.Should().Be(lastName);
        staff.Email.Should().Be(email);
        staff.HireDate.Should().Be(hireDate);
        staff.EmploymentStatus.Should().Be("Active");
        staff.FullName.Should().Be("John Doe");
        staff.CreatedBy.Should().Be(createdBy);
        staff.DomainEvents.Should().ContainSingle(e => e is StaffCreatedEvent);
    }

    [Fact]
    public void Constructor_WithEmptyEmail_ShouldThrowException()
    {
        // Arrange
        var tenantSlug = "test-district";
        var userId = Guid.NewGuid();
        var hireDate = DateTime.UtcNow;
        var createdBy = "admin@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Staff(tenantSlug, userId, "John", "Doe", "", hireDate, createdBy));
    }

    [Fact]
    public void UpdateEmploymentStatus_WithValidStatus_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var staff = new Staff("test-district", Guid.NewGuid(), "John", "Doe", 
            "john.doe@test.com", DateTime.UtcNow, "admin@test.com");
        var newStatus = "Terminated";
        var endDate = DateTime.UtcNow.AddDays(1);
        var updatedBy = "admin@test.com";

        // Act
        staff.UpdateEmploymentStatus(newStatus, updatedBy, endDate);

        // Assert
        staff.EmploymentStatus.Should().Be(newStatus);
        staff.EndDate.Should().Be(endDate);
        staff.UpdatedBy.Should().Be(updatedBy);
        staff.DomainEvents.Should().Contain(e => e is StaffEmploymentStatusChangedEvent);
    }

    [Fact]
    public void Suspend_WithValidReason_ShouldSuspendStaffAndRaiseEvents()
    {
        // Arrange
        var staff = new Staff("test-district", Guid.NewGuid(), "John", "Doe", 
            "john.doe@test.com", DateTime.UtcNow, "admin@test.com");
        var reason = "Performance issues";
        var updatedBy = "admin@test.com";

        // Act
        staff.Suspend(reason, updatedBy);

        // Assert
        staff.EmploymentStatus.Should().Be("Suspended");
        staff.UpdatedBy.Should().Be(updatedBy);
        staff.DomainEvents.Should().Contain(e => e is StaffSuspendedEvent);
        staff.DomainEvents.Should().Contain(e => e is StaffEmploymentStatusChangedEvent);
    }

    [Fact]
    public void UpdateInfo_WithValidData_ShouldUpdateInformation()
    {
        // Arrange
        var staff = new Staff("test-district", Guid.NewGuid(), "John", "Doe", 
            "john.doe@test.com", DateTime.UtcNow, "admin@test.com");
        var newFirstName = "Jane";
        var newEmail = "jane.doe@test.com";
        var updatedBy = "admin@test.com";

        // Act
        staff.UpdateInfo(newFirstName, null, newEmail, updatedBy);

        // Assert
        staff.FirstName.Should().Be(newFirstName);
        staff.Email.Should().Be(newEmail);
        staff.UpdatedBy.Should().Be(updatedBy);
        staff.FullName.Should().Be("Jane Doe");
    }

    [Fact]
    public void FullName_ShouldCombineFirstAndLastName()
    {
        // Arrange & Act
        var staff = new Staff("test-district", Guid.NewGuid(), "John", "Doe", 
            "john.doe@test.com", DateTime.UtcNow, "admin@test.com");

        // Assert
        staff.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void ExternalIdentity_ShouldSupportIdPMapping()
    {
        // Arrange
        var externalId = "ext123";
        var externalIssuer = "google";

        // Act
        var staff = new Staff("test-district", Guid.NewGuid(), "John", "Doe", 
            "john.doe@test.com", DateTime.UtcNow, "admin@test.com", externalId, externalIssuer);

        // Assert
        staff.ExternalId.Should().Be(externalId);
        staff.ExternalIssuer.Should().Be(externalIssuer);
    }
}
