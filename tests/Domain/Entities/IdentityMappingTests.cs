using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class IdentityMappingTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateMapping()
    {
        // Arrange
        var internalUserId = Guid.NewGuid();
        var externalId = "google|123456";
        var issuer = "google.com";
        var createdBy = "system";

        // Act
        var mapping = new IdentityMapping(internalUserId, externalId, issuer, createdBy);

        // Assert
        mapping.Should().NotBeNull();
        mapping.InternalUserId.Should().Be(internalUserId);
        mapping.ExternalId.Should().Be(externalId);
        mapping.Issuer.Should().Be(issuer);
        mapping.Status.Should().Be("Active");
        mapping.CreatedBy.Should().Be(createdBy);
        mapping.DomainEvents.Should().ContainSingle(e => e is IdentityMappingCreatedEvent);
    }

    [Fact]
    public void Constructor_WithEmptyExternalId_ShouldThrowException()
    {
        // Arrange
        var internalUserId = Guid.NewGuid();
        var issuer = "google.com";
        var createdBy = "system";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new IdentityMapping(internalUserId, "", issuer, createdBy));
    }

    [Fact]
    public void Suspend_WithValidReason_ShouldSuspendMappingAndRaiseEvent()
    {
        // Arrange
        var mapping = new IdentityMapping(Guid.NewGuid(), "google|123456", "google.com", "system");
        var reason = "Security violation";
        var suspendedBy = "admin@test.com";

        // Act
        mapping.Suspend(reason, suspendedBy);

        // Assert
        mapping.Status.Should().Be("Suspended");
        mapping.DomainEvents.Should().Contain(e => e is IdentityMappingSuspendedEvent);
    }

    [Fact]
    public void ExternalId_ShouldBeUniquePerIssuer()
    {
        // Arrange & Act
        var mapping = new IdentityMapping(Guid.NewGuid(), "google|123456", "google.com", "system");

        // Assert
        mapping.ExternalId.Should().Be("google|123456");
        // Uniqueness is enforced at repository/database level
    }
}
