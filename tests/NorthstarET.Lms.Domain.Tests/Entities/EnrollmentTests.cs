using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;
using FluentAssertions;
using Xunit;

namespace NorthstarET.Lms.Domain.Tests.Entities;

public class EnrollmentTests
{
    [Fact]
    public void CreateEnrollment_WithValidData_ShouldSucceed()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        var gradeLevel = GradeLevel.Grade6;
        var enrollmentDate = DateTime.UtcNow.Date;

        // Act
        var enrollment = new Enrollment(studentId, classId, schoolYearId, gradeLevel, enrollmentDate);

        // Assert
        enrollment.Id.Should().NotBe(Guid.Empty);
        enrollment.StudentId.Should().Be(studentId);
        enrollment.ClassId.Should().Be(classId);
        enrollment.SchoolYearId.Should().Be(schoolYearId);
        enrollment.GradeLevel.Should().Be(gradeLevel);
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.EnrollmentDate.Should().Be(enrollmentDate);
        enrollment.WithdrawalDate.Should().BeNull();
        enrollment.WithdrawalReason.Should().BeNullOrEmpty();
        enrollment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StudentEnrolledEvent>();
    }

    [Fact]
    public void CreateEnrollment_WithEmptyStudentId_ShouldThrowArgumentException()
    {
        // Arrange
        var studentId = Guid.Empty;
        var classId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        var gradeLevel = GradeLevel.Grade6;
        var enrollmentDate = DateTime.UtcNow.Date;

        // Act & Assert
        var act = () => new Enrollment(studentId, classId, schoolYearId, gradeLevel, enrollmentDate);
        act.Should().Throw<ArgumentException>()
            .WithMessage("StudentId cannot be empty*");
    }

    [Fact]
    public void CreateEnrollment_WithEmptyClassId_ShouldThrowArgumentException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var classId = Guid.Empty;
        var schoolYearId = Guid.NewGuid();
        var gradeLevel = GradeLevel.Grade6;
        var enrollmentDate = DateTime.UtcNow.Date;

        // Act & Assert
        var act = () => new Enrollment(studentId, classId, schoolYearId, gradeLevel, enrollmentDate);
        act.Should().Throw<ArgumentException>()
            .WithMessage("ClassId cannot be empty*");
    }

    [Fact]
    public void CreateEnrollment_WithEmptySchoolYearId_ShouldThrowArgumentException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var schoolYearId = Guid.Empty;
        var gradeLevel = GradeLevel.Grade6;
        var enrollmentDate = DateTime.UtcNow.Date;

        // Act & Assert
        var act = () => new Enrollment(studentId, classId, schoolYearId, gradeLevel, enrollmentDate);
        act.Should().Throw<ArgumentException>()
            .WithMessage("SchoolYearId cannot be empty*");
    }

    [Fact]
    public void CreateEnrollment_WithFutureEnrollmentDate_ShouldThrowArgumentException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        var gradeLevel = GradeLevel.Grade6;
        var enrollmentDate = DateTime.UtcNow.Date.AddDays(1);

        // Act & Assert
        var act = () => new Enrollment(studentId, classId, schoolYearId, gradeLevel, enrollmentDate);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Enrollment date cannot be in the future*");
    }

    [Fact]
    public void Withdraw_WithValidData_ShouldUpdateStatusAndDates()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        var withdrawalDate = DateTime.UtcNow.Date;
        var withdrawalReason = "Family relocation";
        var withdrawnByUserId = "admin-user-123";

        // Act
        enrollment.Withdraw(withdrawalDate, withdrawalReason, withdrawnByUserId);

        // Assert
        enrollment.Status.Should().Be(EnrollmentStatus.Withdrawn);
        enrollment.WithdrawalDate.Should().Be(withdrawalDate);
        enrollment.WithdrawalReason.Should().Be(withdrawalReason);
        enrollment.DomainEvents.Should().Contain(e => e is StudentWithdrawnEvent);
    }

    [Fact]
    public void Withdraw_WhenAlreadyWithdrawn_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        enrollment.Withdraw(DateTime.UtcNow.Date, "First withdrawal", "admin-user-123");

        // Act & Assert
        var act = () => enrollment.Withdraw(DateTime.UtcNow.Date, "Second withdrawal", "admin-user-456");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Enrollment is already withdrawn");
    }

    [Fact]
    public void Withdraw_WithEmptyReason_ShouldThrowArgumentException()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        var withdrawalDate = DateTime.UtcNow.Date;

        // Act & Assert
        var act = () => enrollment.Withdraw(withdrawalDate, "", "admin-user-123");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Withdrawal reason is required*");
    }

    [Fact]
    public void Withdraw_WithFutureWithdrawalDate_ShouldThrowArgumentException()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        var futureWithdrawalDate = DateTime.UtcNow.Date.AddDays(1);
        var withdrawalReason = "Future withdrawal";
        var withdrawnByUserId = "admin-user-123";

        // Act & Assert
        var act = () => enrollment.Withdraw(futureWithdrawalDate, withdrawalReason, withdrawnByUserId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Withdrawal date cannot be in the future*");
    }

    [Fact]
    public void Withdraw_WithDateBeforeEnrollment_ShouldThrowArgumentException()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        var earlyWithdrawalDate = enrollment.EnrollmentDate.AddDays(-1);
        var withdrawalReason = "Early withdrawal";
        var withdrawnByUserId = "admin-user-123";

        // Act & Assert
        var act = () => enrollment.Withdraw(earlyWithdrawalDate, withdrawalReason, withdrawnByUserId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Withdrawal date cannot be before enrollment date*");
    }

    [Fact]
    public void Transfer_WithValidData_ShouldUpdateStatusAndClass()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        var newClassId = Guid.NewGuid();
        var transferDate = DateTime.UtcNow.Date;
        var transferReason = "Schedule change";
        var transferredByUserId = "admin-user-123";

        // Act
        enrollment.Transfer(newClassId, transferDate, transferReason, transferredByUserId);

        // Assert
        enrollment.Status.Should().Be(EnrollmentStatus.Transferred);
        enrollment.ClassId.Should().Be(newClassId);
        enrollment.DomainEvents.Should().Contain(e => e is StudentTransferredEvent);
    }

    [Fact]
    public void Transfer_WhenAlreadyWithdrawn_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        enrollment.Withdraw(DateTime.UtcNow.Date, "Withdrawal reason", "admin-user-123");
        var newClassId = Guid.NewGuid();

        // Act & Assert
        var act = () => enrollment.Transfer(newClassId, DateTime.UtcNow.Date, "Transfer reason", "admin-user-456");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot transfer withdrawn enrollment");
    }

    [Fact]
    public void Transfer_ToSameClass_ShouldThrowArgumentException()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        var transferDate = DateTime.UtcNow.Date;
        var transferReason = "Same class transfer";
        var transferredByUserId = "admin-user-123";

        // Act & Assert
        var act = () => enrollment.Transfer(enrollment.ClassId, transferDate, transferReason, transferredByUserId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Cannot transfer to the same class*");
    }

    [Fact]
    public void Graduate_WithValidData_ShouldUpdateStatusAndGraduationDate()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        var graduationDate = DateTime.UtcNow.Date;
        var graduatedByUserId = "admin-user-123";

        // Act
        enrollment.Graduate(graduationDate, graduatedByUserId);

        // Assert
        enrollment.Status.Should().Be(EnrollmentStatus.Graduated);
        enrollment.WithdrawalDate.Should().Be(graduationDate);
        enrollment.WithdrawalReason.Should().Be("Graduated");
        enrollment.DomainEvents.Should().Contain(e => e is StudentGraduatedEvent);
    }

    [Fact]
    public void Graduate_WhenAlreadyWithdrawn_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        enrollment.Withdraw(DateTime.UtcNow.Date, "Withdrawal reason", "admin-user-123");

        // Act & Assert
        var act = () => enrollment.Graduate(DateTime.UtcNow.Date, "admin-user-456");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot graduate withdrawn enrollment");
    }

    [Fact]
    public void Reinstate_WithdrawnEnrollment_ShouldReactivateEnrollment()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        enrollment.Withdraw(DateTime.UtcNow.Date, "Temporary withdrawal", "admin-user-123");
        var reinstateDate = DateTime.UtcNow.Date;
        var reinstateReason = "Return from medical leave";
        var reinstatedByUserId = "admin-user-456";

        // Act
        enrollment.Reinstate(reinstateDate, reinstateReason, reinstatedByUserId);

        // Assert
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.WithdrawalDate.Should().BeNull();
        enrollment.WithdrawalReason.Should().BeNullOrEmpty();
        enrollment.DomainEvents.Should().Contain(e => e is StudentReinstatedEvent);
    }

    [Fact]
    public void Reinstate_ActiveEnrollment_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();

        // Act & Assert
        var act = () => enrollment.Reinstate(DateTime.UtcNow.Date, "Reinstate reason", "admin-user-123");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reinstate active enrollment");
    }

    [Fact]
    public void Reinstate_GraduatedEnrollment_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        enrollment.Graduate(DateTime.UtcNow.Date, "admin-user-123");

        // Act & Assert
        var act = () => enrollment.Reinstate(DateTime.UtcNow.Date, "Reinstate reason", "admin-user-456");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reinstate graduated enrollment");
    }

    [Fact]
    public void UpdateGradeLevel_WithValidGradeLevel_ShouldUpdateSuccessfully()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        var newGradeLevel = GradeLevel.Grade7;
        var updatedByUserId = "admin-user-123";

        // Act
        enrollment.UpdateGradeLevel(newGradeLevel, updatedByUserId);

        // Assert
        enrollment.GradeLevel.Should().Be(newGradeLevel);
        enrollment.DomainEvents.Should().Contain(e => e is StudentGradeLevelUpdatedEvent);
    }

    [Fact]
    public void UpdateGradeLevel_WithSameGradeLevel_ShouldNotAddDomainEvent()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        var originalEventCount = enrollment.DomainEvents.Count;
        var updatedByUserId = "admin-user-123";

        // Act
        enrollment.UpdateGradeLevel(enrollment.GradeLevel, updatedByUserId);

        // Assert
        enrollment.DomainEvents.Should().HaveCount(originalEventCount);
    }

    [Fact]
    public void IsActive_ForActiveEnrollment_ShouldReturnTrue()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();

        // Act
        var isActive = enrollment.IsActive;

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ForWithdrawnEnrollment_ShouldReturnFalse()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        enrollment.Withdraw(DateTime.UtcNow.Date, "Withdrawal reason", "admin-user-123");

        // Act
        var isActive = enrollment.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ForTransferredEnrollment_ShouldReturnFalse()
    {
        // Arrange
        var enrollment = CreateValidEnrollment();
        enrollment.Transfer(Guid.NewGuid(), DateTime.UtcNow.Date, "Transfer reason", "admin-user-123");

        // Act
        var isActive = enrollment.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    private static Enrollment CreateValidEnrollment()
    {
        return new Enrollment(
            studentId: Guid.NewGuid(),
            classId: Guid.NewGuid(),
            schoolYearId: Guid.NewGuid(),
            gradeLevel: GradeLevel.Grade6,
            enrollmentDate: DateTime.UtcNow.Date.AddDays(-30)
        );
    }
}