using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Tests.Entities;

public class RoleAssignmentTests
{
    [Fact]
    public void CreateRoleAssignment_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleDefinitionId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var assignedBy = "district-admin-123";

        // Act
        var assignment = new RoleAssignment(userId, roleDefinitionId, schoolId, assignedBy);

        // Assert
        assignment.UserId.Should().Be(userId);
        assignment.RoleDefinitionId.Should().Be(roleDefinitionId);
        assignment.SchoolId.Should().Be(schoolId);
        assignment.AssignedByUserId.Should().Be(assignedBy);
        assignment.Status.Should().Be(RoleAssignmentStatus.Active);
        assignment.EffectiveDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        assignment.ClassId.Should().BeNull();
        assignment.SchoolYearId.Should().BeNull();
    }

    [Fact]
    public void CreateRoleAssignment_WithClassScope_ShouldSetClassId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleDefinitionId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var assignedBy = "district-admin-123";

        // Act
        var assignment = new RoleAssignment(userId, roleDefinitionId, schoolId, assignedBy, classId);

        // Assert
        assignment.ClassId.Should().Be(classId);
        assignment.SchoolId.Should().Be(schoolId);
    }

    [Fact]
    public void CreateRoleAssignment_WithSchoolYearScope_ShouldSetSchoolYearId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleDefinitionId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        var assignedBy = "district-admin-123";

        // Act
        var assignment = new RoleAssignment(userId, roleDefinitionId, schoolId, assignedBy, null, schoolYearId);

        // Assert
        assignment.SchoolYearId.Should().Be(schoolYearId);
        assignment.SchoolId.Should().Be(schoolId);
    }

    [Fact]
    public void CreateRoleAssignment_WithExpirationDate_ShouldSetExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleDefinitionId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var assignedBy = "district-admin-123";
        var expirationDate = DateTime.UtcNow.AddMonths(6);

        // Act
        var assignment = new RoleAssignment(userId, roleDefinitionId, schoolId, assignedBy, 
            expirationDate: expirationDate);

        // Assert
        assignment.ExpirationDate.Should().Be(expirationDate);
        assignment.IsTemporary.Should().BeTrue();
    }

    [Fact]
    public void CreateRoleAssignment_WithPastExpirationDate_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleDefinitionId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var assignedBy = "district-admin-123";
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        var act = () => new RoleAssignment(userId, roleDefinitionId, schoolId, assignedBy, 
            expirationDate: pastDate);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Expiration date cannot be in the past*");
    }

    [Fact]
    public void IsExpired_WhenExpirationDatePassed_ShouldReturnTrue()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.SetExpiration(DateTime.UtcNow.AddHours(-1), "admin-123"); // 1 hour ago

        // Act & Assert
        assignment.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenNoExpirationDate_ShouldReturnFalse()
    {
        // Arrange
        var assignment = CreateValidAssignment();

        // Act & Assert
        assignment.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenActive_ShouldChangeStatusAndGenerateEvent()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        var reason = "Policy violation";
        var revokedBy = "district-admin-456";

        // Act
        assignment.Revoke(reason, revokedBy);

        // Assert
        assignment.Status.Should().Be(RoleAssignmentStatus.Revoked);
        assignment.RevokedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        assignment.RevokedByUserId.Should().Be(revokedBy);
        assignment.RevocationReason.Should().Be(reason);
        assignment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RoleAssignmentRevokedEvent>();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Revoke("First revocation", "admin-1");

        // Act & Assert
        var act = () => assignment.Revoke("Second revocation", "admin-2");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already revoked*");
    }

    [Fact]
    public void ExtendExpiration_WhenTemporary_ShouldUpdateExpirationDate()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.SetExpiration(DateTime.UtcNow.AddMonths(1), "admin-1");
        var newExpirationDate = DateTime.UtcNow.AddMonths(3);
        var extendedBy = "admin-2";

        // Act
        assignment.ExtendExpiration(newExpirationDate, extendedBy);

        // Assert
        assignment.ExpirationDate.Should().Be(newExpirationDate);
        assignment.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ExtendExpiration_WhenNotTemporary_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var assignment = CreateValidAssignment(); // No expiration date set

        // Act & Assert
        var act = () => assignment.ExtendExpiration(DateTime.UtcNow.AddMonths(1), "admin-123");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*not a temporary assignment*");
    }

    [Fact]
    public void SetExpiration_WhenActive_ShouldConvertToPermanent()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.SetExpiration(DateTime.UtcNow.AddMonths(1), "admin-1");

        // Act
        assignment.RemoveExpiration("admin-2");

        // Assert
        assignment.ExpirationDate.Should().BeNull();
        assignment.IsTemporary.Should().BeFalse();
        assignment.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsEffective_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var assignment = CreateValidAssignment();

        // Act & Assert
        assignment.IsEffective.Should().BeTrue();
    }

    [Fact]
    public void IsEffective_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Revoke("Test revocation", "admin-123");

        // Act & Assert
        assignment.IsEffective.Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.SetExpiration(DateTime.UtcNow.AddHours(-1), "admin-123"); // 1 hour ago

        // Act & Assert
        assignment.IsEffective.Should().BeFalse();
    }

    [Fact]
    public void HasScope_WhenSchoolIdMatches_ShouldReturnTrue()
    {
        // Arrange
        var schoolId = Guid.NewGuid();
        var assignment = new RoleAssignment(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            schoolId, 
            "admin-123");

        // Act & Assert
        assignment.HasScope(schoolId: schoolId).Should().BeTrue();
    }

    [Fact]
    public void HasScope_WhenClassIdMatches_ShouldReturnTrue()
    {
        // Arrange
        var schoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var assignment = new RoleAssignment(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            schoolId, 
            "admin-123", 
            classId);

        // Act & Assert
        assignment.HasScope(classId: classId).Should().BeTrue();
    }

    private static RoleAssignment CreateValidAssignment()
    {
        return new RoleAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test-admin");
    }
}