using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class LegalHoldTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateLegalHold()
    {
        // Arrange
        var entityType = "Student";
        var entityId = Guid.NewGuid();
        var reason = "Litigation hold for case #12345";
        var issuedBy = "legal@test.com";

        // Act
        var legalHold = new LegalHold(entityType, entityId, reason, issuedBy);

        // Assert
        legalHold.Should().NotBeNull();
        legalHold.EntityType.Should().Be(entityType);
        legalHold.EntityId.Should().Be(entityId);
        legalHold.Reason.Should().Be(reason);
        legalHold.IssuedBy.Should().Be(issuedBy);
        legalHold.Status.Should().Be("Active");
        legalHold.DomainEvents.Should().ContainSingle(e => e is LegalHoldAppliedEvent);
    }

    [Fact]
    public void Constructor_WithEmptyReason_ShouldThrowException()
    {
        // Arrange
        var entityType = "Student";
        var entityId = Guid.NewGuid();
        var issuedBy = "legal@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new LegalHold(entityType, entityId, "", issuedBy));
    }

    [Fact]
    public void Release_WithValidReason_ShouldReleaseLegalHoldAndRaiseEvent()
    {
        // Arrange
        var legalHold = new LegalHold("Student", Guid.NewGuid(), "Litigation hold", "legal@test.com");
        var releaseReason = "Case dismissed";
        var releasedBy = "legal@test.com";

        // Act
        legalHold.Release(releasedBy, releaseReason);

        // Assert
        legalHold.Status.Should().Be("Released");
        legalHold.ReleasedBy.Should().Be(releasedBy);
        legalHold.ReleasedAt.Should().NotBeNull();
        legalHold.DomainEvents.Should().Contain(e => e is LegalHoldReleasedEvent);
    }

    [Fact]
    public void Release_WhenNotActive_ShouldThrowException()
    {
        // Arrange
        var legalHold = new LegalHold("Student", Guid.NewGuid(), "Litigation hold", "legal@test.com");
        legalHold.Release("legal@test.com", "Case dismissed");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            legalHold.Release("legal@test.com", "Another reason"));
    }

    [Fact]
    public void ExpiresAt_ShouldSupportTemporaryHolds()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(30);

        // Act
        var legalHold = new LegalHold("Student", Guid.NewGuid(), "Temporary hold", "legal@test.com", expiresAt);

        // Assert
        legalHold.ExpiresAt.Should().Be(expiresAt);
    }
}
