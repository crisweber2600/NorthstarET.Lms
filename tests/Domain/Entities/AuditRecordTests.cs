using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class AuditRecordTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateAuditRecord()
    {
        // Arrange
        var tenantSlug = "test-district";
        var actorId = Guid.NewGuid();
        var actorRole = "DistrictAdmin";
        var action = "CreateStudent";
        var entityType = "Student";
        var entityId = Guid.NewGuid();
        var payload = "{\"studentNumber\":\"STU-001\"}";
        var previousHash = "sha256:000";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var auditRecord = new AuditRecord(tenantSlug, actorId, actorRole, action, 
            entityType, entityId, payload, previousHash, correlationId);

        // Assert
        auditRecord.Should().NotBeNull();
        auditRecord.ActorId.Should().Be(actorId);
        auditRecord.ActorRole.Should().Be(actorRole);
        auditRecord.Action.Should().Be(action);
        auditRecord.EntityType.Should().Be(entityType);
        auditRecord.EntityId.Should().Be(entityId);
        auditRecord.Payload.Should().Be(payload);
        auditRecord.PreviousHash.Should().Be(previousHash);
        auditRecord.CorrelationId.Should().Be(correlationId);
        auditRecord.CurrentHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyAction_ShouldThrowException()
    {
        // Arrange
        var tenantSlug = "test-district";
        var actorId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new AuditRecord(tenantSlug, actorId, "Admin", "", "Student", entityId, "{}", "hash", "corr"));
    }

    [Fact]
    public void CurrentHash_ShouldBeComputedFromPreviousHashAndPayload()
    {
        // Arrange
        var previousHash = "sha256:abc";
        var payload = "{\"test\":\"data\"}";

        // Act
        var auditRecord = new AuditRecord("test-district", Guid.NewGuid(), "Admin", 
            "TestAction", "Student", Guid.NewGuid(), payload, previousHash, "corr1");

        // Assert
        auditRecord.CurrentHash.Should().NotBeNullOrEmpty();
        auditRecord.CurrentHash.Should().NotBe(previousHash);
    }

    [Fact]
    public void TamperEvident_ShouldLinkRecordsTogether()
    {
        // Arrange
        var auditRecord1 = new AuditRecord("test-district", Guid.NewGuid(), "Admin", 
            "Action1", "Student", Guid.NewGuid(), "{}", "sha256:000", "corr1");

        // Act
        var auditRecord2 = new AuditRecord("test-district", Guid.NewGuid(), "Admin", 
            "Action2", "Student", Guid.NewGuid(), "{}", auditRecord1.CurrentHash, "corr2");

        // Assert
        auditRecord2.PreviousHash.Should().Be(auditRecord1.CurrentHash);
        auditRecord2.CurrentHash.Should().NotBe(auditRecord1.CurrentHash);
    }

    [Fact]
    public void Timestamp_ShouldBeSetOnCreation()
    {
        // Arrange & Act
        var auditRecord = new AuditRecord("test-district", Guid.NewGuid(), "Admin", 
            "TestAction", "Student", Guid.NewGuid(), "{}", "sha256:000", "corr");

        // Assert
        auditRecord.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
