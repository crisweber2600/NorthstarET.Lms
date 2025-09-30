using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class GuardianTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateGuardian()
    {
        // Arrange
        var tenantSlug = "test-district";
        var firstName = "Jane";
        var lastName = "Smith";
        var email = "jane.smith@test.com";
        var createdBy = "admin@test.com";

        // Act
        var guardian = new Guardian(tenantSlug, firstName, lastName, createdBy, email);

        // Assert
        guardian.Should().NotBeNull();
        guardian.FirstName.Should().Be(firstName);
        guardian.LastName.Should().Be(lastName);
        guardian.Email.Should().Be(email);
        guardian.FullName.Should().Be("Jane Smith");
        guardian.CreatedBy.Should().Be(createdBy);
        guardian.DomainEvents.Should().ContainSingle(e => e is GuardianCreatedEvent);
    }

    [Fact]
    public void Constructor_WithEmptyFirstName_ShouldThrowException()
    {
        // Arrange
        var tenantSlug = "test-district";
        var createdBy = "admin@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Guardian(tenantSlug, "", "Smith", createdBy));
    }

    [Fact]
    public void UpdateInfo_WithValidData_ShouldUpdateInformation()
    {
        // Arrange
        var guardian = new Guardian("test-district", "Jane", "Smith", "admin@test.com");
        var newEmail = "jane.new@test.com";
        var newPhone = "555-1234";
        var updatedBy = "admin@test.com";

        // Act
        guardian.UpdateInfo(null, null, newEmail, newPhone, null, updatedBy);

        // Assert
        guardian.Email.Should().Be(newEmail);
        guardian.Phone.Should().Be(newPhone);
        guardian.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void FullName_ShouldCombineFirstAndLastName()
    {
        // Arrange & Act
        var guardian = new Guardian("test-district", "Jane", "Smith", "admin@test.com");

        // Assert
        guardian.FullName.Should().Be("Jane Smith");
    }
}
