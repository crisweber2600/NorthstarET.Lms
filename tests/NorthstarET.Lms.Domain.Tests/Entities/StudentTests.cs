using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Tests.Entities;

public class StudentTests
{
    [Fact]
    public void CreateStudent_WithValidData_ShouldSucceed()
    {
        // Arrange
        var studentNumber = "STU-2024-001";
        var firstName = "Maria";
        var lastName = "Garcia";
        var dateOfBirth = new DateTime(2010, 6, 15);
        var enrollmentDate = new DateTime(2024, 8, 15);

        // Act
        var student = new Student(studentNumber, firstName, lastName, dateOfBirth, enrollmentDate);

        // Assert
        student.StudentNumber.Should().Be(studentNumber);
        student.FirstName.Should().Be(firstName);
        student.LastName.Should().Be(lastName);
        student.DateOfBirth.Should().Be(dateOfBirth);
        student.EnrollmentDate.Should().Be(enrollmentDate);
        student.Status.Should().Be(UserLifecycleStatus.Active);
        student.UserId.Should().NotBeEmpty();
        student.FullName.Should().Be("Maria Garcia");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void CreateStudent_WithInvalidStudentNumber_ShouldThrowArgumentException(string? invalidStudentNumber)
    {
        // Arrange
        var firstName = "John";
        var lastName = "Smith";
        var dateOfBirth = new DateTime(2010, 1, 1);
        var enrollmentDate = DateTime.Today;

        // Act & Assert
        var act = () => new Student(invalidStudentNumber, firstName, lastName, dateOfBirth, enrollmentDate);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Student number*");
    }

    [Fact]
    public void CreateStudent_WithFutureDateOfBirth_ShouldThrowArgumentException()
    {
        // Arrange
        var studentNumber = "STU-2024-001";
        var firstName = "Jane";
        var lastName = "Doe";
        var dateOfBirth = DateTime.Today.AddDays(1); // Future date
        var enrollmentDate = DateTime.Today;

        // Act & Assert
        var act = () => new Student(studentNumber, firstName, lastName, dateOfBirth, enrollmentDate);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*future*");
    }

    [Fact]
    public void CreateStudent_WithTooOldDateOfBirth_ShouldThrowArgumentException()
    {
        // Arrange
        var studentNumber = "STU-2024-001";
        var firstName = "Old";
        var lastName = "Student";
        var dateOfBirth = new DateTime(1900, 1, 1); // Too old
        var enrollmentDate = DateTime.Today;

        // Act & Assert
        var act = () => new Student(studentNumber, firstName, lastName, dateOfBirth, enrollmentDate);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*unreasonably old*");
    }

    [Fact]
    public void UpdateGradeLevel_ShouldGenerateDomainEvent()
    {
        // Arrange
        var student = CreateValidStudent();
        var newGradeLevel = GradeLevel.Grade7;
        var updatedBy = "teacher-123";

        // Act
        student.UpdateGradeLevel(newGradeLevel, updatedBy);

        // Assert
        student.CurrentGradeLevel.Should().Be(newGradeLevel);
        student.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StudentGradeUpdatedEvent>();
    }

    [Fact]
    public void SetProgramParticipation_ShouldUpdateFlags()
    {
        // Arrange
        var student = CreateValidStudent();

        // Act
        student.SetProgramParticipation(true, false, true);

        // Assert
        student.IsSpecialEducation.Should().BeTrue();
        student.IsGifted.Should().BeFalse();
        student.IsEnglishLanguageLearner.Should().BeTrue();
    }

    [Fact]
    public void AddAccommodationTag_ShouldAddToCollection()
    {
        // Arrange
        var student = CreateValidStudent();
        var accommodationTag = "extended-time";

        // Act
        student.AddAccommodationTag(accommodationTag);

        // Assert
        student.AccommodationTags.Should().Contain(accommodationTag);
    }

    [Fact]
    public void RemoveAccommodationTag_WhenExists_ShouldRemoveFromCollection()
    {
        // Arrange
        var student = CreateValidStudent();
        var accommodationTag = "large-print";
        student.AddAccommodationTag(accommodationTag);

        // Act
        student.RemoveAccommodationTag(accommodationTag);

        // Assert
        student.AccommodationTags.Should().NotContain(accommodationTag);
    }

    [Fact]
    public void Withdraw_ShouldUpdateStatusAndDate()
    {
        // Arrange
        var student = CreateValidStudent();
        var withdrawalDate = DateTime.Today;
        var reason = "Family moved";

        // Act
        student.Withdraw(withdrawalDate, reason);

        // Assert
        student.Status.Should().Be(UserLifecycleStatus.Withdrawn);
        student.WithdrawalDate.Should().Be(withdrawalDate);
        student.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StudentWithdrawnEvent>();
    }

    private static Student CreateValidStudent()
    {
        return new Student(
            "STU-2024-001",
            "Test",
            "Student", 
            new DateTime(2010, 1, 1),
            DateTime.Today);
    }
}