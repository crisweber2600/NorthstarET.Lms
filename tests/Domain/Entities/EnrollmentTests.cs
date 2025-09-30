using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class EnrollmentTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateEnrollment()
    {
        // Arrange
        var tenantSlug = "test-district";
        var studentId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        var entryDate = DateTime.UtcNow;
        var createdBy = "admin@test.com";

        // Act
        var enrollment = new Enrollment(tenantSlug, studentId, classId, schoolYearId, entryDate, createdBy);

        // Assert
        enrollment.Should().NotBeNull();
        enrollment.StudentId.Should().Be(studentId);
        enrollment.ClassId.Should().Be(classId);
        enrollment.SchoolYearId.Should().Be(schoolYearId);
        enrollment.EntryDate.Should().Be(entryDate);
        enrollment.EnrollmentStatus.Should().Be("Active");
        enrollment.CreatedBy.Should().Be(createdBy);
        enrollment.DomainEvents.Should().ContainSingle(e => e is EnrollmentCreatedEvent);
    }

    [Fact]
    public void Withdraw_WithValidReason_ShouldWithdrawEnrollmentAndRaiseEvent()
    {
        // Arrange
        var enrollment = new Enrollment("test-district", Guid.NewGuid(), Guid.NewGuid(), 
            Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), "admin@test.com");
        var exitDate = DateTime.UtcNow;
        var reason = "Moved to different school";
        var updatedBy = "admin@test.com";

        // Act
        enrollment.Withdraw(exitDate, reason, updatedBy);

        // Assert
        enrollment.EnrollmentStatus.Should().Be("Withdrawn");
        enrollment.ExitDate.Should().Be(exitDate);
        enrollment.ExitReason.Should().Be(reason);
        enrollment.UpdatedBy.Should().Be(updatedBy);
        enrollment.DomainEvents.Should().Contain(e => e is EnrollmentWithdrawnEvent);
    }

    [Fact]
    public void Withdraw_WhenNotActive_ShouldThrowException()
    {
        // Arrange
        var enrollment = new Enrollment("test-district", Guid.NewGuid(), Guid.NewGuid(), 
            Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), "admin@test.com");
        enrollment.Withdraw(DateTime.UtcNow, "Test reason", "admin@test.com");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            enrollment.Withdraw(DateTime.UtcNow, "Another reason", "admin@test.com"));
    }

    [Fact]
    public void Transfer_WithValidReason_ShouldTransferEnrollmentAndRaiseEvent()
    {
        // Arrange
        var enrollment = new Enrollment("test-district", Guid.NewGuid(), Guid.NewGuid(), 
            Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), "admin@test.com");
        var exitDate = DateTime.UtcNow;
        var reason = "Transferred to advanced class";
        var updatedBy = "admin@test.com";

        // Act
        enrollment.Transfer(exitDate, reason, updatedBy);

        // Assert
        enrollment.EnrollmentStatus.Should().Be("Transferred");
        enrollment.ExitDate.Should().Be(exitDate);
        enrollment.ExitReason.Should().Be(reason);
        enrollment.UpdatedBy.Should().Be(updatedBy);
        enrollment.DomainEvents.Should().Contain(e => e is EnrollmentTransferredEvent);
    }

    [Fact]
    public void CanModify_WhenSchoolYearArchived_ShouldReturnFalse()
    {
        // Arrange
        var enrollment = new Enrollment("test-district", Guid.NewGuid(), Guid.NewGuid(), 
            Guid.NewGuid(), DateTime.UtcNow, "admin@test.com");

        // Act
        var canModify = enrollment.CanModify(true);

        // Assert
        canModify.Should().BeFalse();
    }

    [Fact]
    public void Withdraw_WithExitDateBeforeEntry_ShouldThrowException()
    {
        // Arrange
        var entryDate = DateTime.UtcNow;
        var enrollment = new Enrollment("test-district", Guid.NewGuid(), Guid.NewGuid(), 
            Guid.NewGuid(), entryDate, "admin@test.com");
        var exitDate = entryDate.AddDays(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            enrollment.Withdraw(exitDate, "Test reason", "admin@test.com"));
    }
}
