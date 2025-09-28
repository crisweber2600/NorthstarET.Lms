using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Domain.Tests.Entities;

public class AuditRecordTests
{
    [Fact]
    public void CreateAuditRecord_WithValidData_ShouldSucceed()
    {
        // Arrange
        var eventType = AuditEventType.Update;
        var entityType = "Student";
        var entityId = Guid.NewGuid();
        var userId = "user-123";
        var changeDetails = new { OldValue = "Grade5", NewValue = "Grade6" };

        // Act
        var auditRecord = new AuditRecord(eventType, entityType, entityId, userId, changeDetails);

        // Assert
        auditRecord.EventType.Should().Be(eventType);
        auditRecord.EntityType.Should().Be(entityType);
        auditRecord.EntityId.Should().Be(entityId);
        auditRecord.UserId.Should().Be(userId);
        auditRecord.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        auditRecord.Id.Should().NotBeEmpty();
        auditRecord.ChangeDetails.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateAuditRecord_WithNullChangeDetails_ShouldAcceptNull()
    {
        // Arrange
        var eventType = AuditEventType.Delete;
        var entityType = "Student";
        var entityId = Guid.NewGuid();
        var userId = "user-123";

        // Act
        var auditRecord = new AuditRecord(eventType, entityType, entityId, userId, null);

        // Assert
        auditRecord.ChangeDetails.Should().BeNull();
        auditRecord.EventType.Should().Be(eventType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void CreateAuditRecord_WithInvalidEntityType_ShouldThrowArgumentException(string invalidEntityType)
    {
        // Arrange
        var eventType = AuditEventType.Create;
        var entityId = Guid.NewGuid();
        var userId = "user-123";

        // Act & Assert
        var act = () => new AuditRecord(eventType, invalidEntityType, entityId, userId, null);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Entity type is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void CreateAuditRecord_WithInvalidUserId_ShouldThrowArgumentException(string invalidUserId)
    {
        // Arrange
        var eventType = AuditEventType.Create;
        var entityType = "Student";
        var entityId = Guid.NewGuid();

        // Act & Assert
        var act = () => new AuditRecord(eventType, entityType, entityId, invalidUserId, null);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*User ID is required*");
    }

    [Fact]
    public void CreateAuditRecord_WithEmptyEntityId_ShouldThrowArgumentException()
    {
        // Arrange
        var eventType = AuditEventType.Create;
        var entityType = "Student";
        var entityId = Guid.Empty;
        var userId = "user-123";

        // Act & Assert
        var act = () => new AuditRecord(eventType, entityType, entityId, userId, null);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Entity ID cannot be empty*");
    }

    [Fact]
    public void SetHashChain_WithValidPreviousHash_ShouldSetHash()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();
        var previousHash = "previous-hash-123";

        // Act
        auditRecord.SetHashChain(previousHash);

        // Assert
        auditRecord.PreviousRecordHash.Should().Be(previousHash);
        auditRecord.RecordHash.Should().NotBeNullOrEmpty();
        auditRecord.RecordHash.Should().NotBe(previousHash);
    }

    [Fact]
    public void SetHashChain_ForFirstRecord_ShouldSetGenesisHash()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();

        // Act
        auditRecord.SetHashChain(null); // First record in chain

        // Assert
        auditRecord.PreviousRecordHash.Should().BeNull();
        auditRecord.RecordHash.Should().NotBeNullOrEmpty();
        auditRecord.RecordHash.Should().StartWith("genesis-");
    }

    [Fact]
    public void SetHashChain_WhenAlreadySet_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();
        auditRecord.SetHashChain("first-hash");

        // Act & Assert
        var act = () => auditRecord.SetHashChain("second-hash");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*hash chain already set*");
    }

    [Fact]
    public void VerifyIntegrity_WithValidHash_ShouldReturnTrue()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();
        var previousHash = "previous-hash-123";
        auditRecord.SetHashChain(previousHash);

        // Act
        var isValid = auditRecord.VerifyIntegrity(previousHash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyIntegrity_WithTamperedData_ShouldReturnFalse()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();
        auditRecord.SetHashChain("previous-hash-123");
        
        // Simulate tampering by changing entity type after hash is set
        var tamperedRecord = new AuditRecord(
            AuditEventType.Delete, // Different event type
            auditRecord.EntityType,
            auditRecord.EntityId,
            auditRecord.UserId,
            auditRecord.ChangeDetails);
        
        // Use reflection to set the hash from original record (simulating tampering)
        var hashProperty = typeof(AuditRecord).GetProperty("RecordHash");
        hashProperty?.SetValue(tamperedRecord, auditRecord.RecordHash);

        // Act
        var isValid = tamperedRecord.VerifyIntegrity("previous-hash-123");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void AddCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();
        var correlationId = Guid.NewGuid();

        // Act
        auditRecord.AddCorrelationId(correlationId);

        // Assert
        auditRecord.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void AddMetadata_ShouldSetAdditionalMetadata()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();
        var metadata = new { IpAddress = "192.168.1.1", UserAgent = "Test-Agent" };

        // Act
        auditRecord.AddMetadata(metadata);

        // Assert
        auditRecord.AdditionalMetadata.Should().NotBeNullOrEmpty();
        auditRecord.AdditionalMetadata.Should().Contain("192.168.1.1");
    }

    [Fact]
    public void IsPartOfBulkOperation_WhenHasCorrelationId_ShouldReturnTrue()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();
        auditRecord.AddCorrelationId(Guid.NewGuid());

        // Act & Assert
        auditRecord.IsPartOfBulkOperation.Should().BeTrue();
    }

    [Fact]
    public void IsPartOfBulkOperation_WhenNoCorrelationId_ShouldReturnFalse()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();

        // Act & Assert
        auditRecord.IsPartOfBulkOperation.Should().BeFalse();
    }

    [Fact]
    public void GetAuditSummary_ShouldReturnFormattedSummary()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();
        auditRecord.SetHashChain("previous-hash");

        // Act
        var summary = auditRecord.GetAuditSummary();

        // Assert
        summary.Should().Contain(auditRecord.EventType.ToString());
        summary.Should().Contain(auditRecord.EntityType);
        summary.Should().Contain(auditRecord.UserId);
        summary.Should().Contain(auditRecord.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public void CreateSecurityAuditRecord_WithSecurityEvent_ShouldSetSecurityFlags()
    {
        // Arrange
        var eventType = AuditEventType.SecurityViolation;
        var entityType = "User";
        var entityId = Guid.NewGuid();
        var userId = "security-system";
        var securityDetails = new { 
            Violation = "Unauthorized access attempt", 
            IpAddress = "192.168.1.100",
            Severity = "High"
        };

        // Act
        var auditRecord = new AuditRecord(eventType, entityType, entityId, userId, securityDetails);

        // Assert
        auditRecord.EventType.Should().Be(AuditEventType.SecurityViolation);
        auditRecord.IsSecurityEvent.Should().BeTrue();
        auditRecord.ChangeDetails.Should().Contain("Unauthorized access attempt");
    }

    [Fact]
    public void ComplianceReport_ShouldIncludeRequiredFields()
    {
        // Arrange
        var auditRecord = CreateValidAuditRecord();
        auditRecord.SetHashChain("previous-hash");
        auditRecord.AddCorrelationId(Guid.NewGuid());

        // Act
        var complianceData = auditRecord.ToComplianceReport();

        // Assert
        complianceData.Should().Contain(auditRecord.Id.ToString());
        complianceData.Should().Contain(auditRecord.EventType.ToString());
        complianceData.Should().Contain(auditRecord.EntityType);
        complianceData.Should().Contain(auditRecord.UserId);
        complianceData.Should().Contain(auditRecord.RecordHash);
        complianceData.Should().Contain("TamperEvident");
    }

    private static AuditRecord CreateValidAuditRecord()
    {
        return new AuditRecord(
            AuditEventType.Create,
            "Student",
            Guid.NewGuid(),
            "test-user-123",
            new { Name = "John Doe", Grade = "5th" });
    }
}