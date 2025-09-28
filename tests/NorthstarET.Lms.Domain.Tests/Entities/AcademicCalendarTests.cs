using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Domain.Events;
using FluentAssertions;
using Xunit;

namespace NorthstarET.Lms.Domain.Tests.Entities;

public class AcademicCalendarTests
{
    [Fact]
    public void CreateAcademicCalendar_WithValidData_ShouldSucceed()
    {
        // Arrange
        var schoolYearId = Guid.NewGuid();

        // Act
        var calendar = new AcademicCalendar(schoolYearId);

        // Assert
        calendar.Id.Should().NotBe(Guid.Empty);
        calendar.SchoolYearId.Should().Be(schoolYearId);
        calendar.Terms.Should().BeEmpty();
        calendar.Closures.Should().BeEmpty();
        calendar.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AcademicCalendarCreatedEvent>();
    }

    [Fact]
    public void CreateAcademicCalendar_WithEmptySchoolYearId_ShouldThrowArgumentException()
    {
        // Arrange
        var schoolYearId = Guid.Empty;

        // Act & Assert
        var act = () => new AcademicCalendar(schoolYearId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("SchoolYearId cannot be empty*");
    }

    [Fact]
    public void AddTerm_WithValidTerm_ShouldSucceed()
    {
        // Arrange
        var calendar = CreateValidCalendar();
        var term = new Term("Fall Semester", new DateTime(2024, 8, 15), new DateTime(2024, 12, 20), 1);

        // Act
        calendar.AddTerm(term);

        // Assert
        calendar.Terms.Should().ContainSingle()
            .Which.Should().Be(term);
        calendar.DomainEvents.Should().Contain(e => e is TermAddedEvent);
    }

    [Fact]
    public void AddTerm_WithNullTerm_ShouldThrowArgumentNullException()
    {
        // Arrange
        var calendar = CreateValidCalendar();

        // Act & Assert
        var act = () => calendar.AddTerm(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddTerm_WithOverlappingDates_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var calendar = CreateValidCalendar();
        var term1 = new Term("Fall Semester", new DateTime(2024, 8, 15), new DateTime(2024, 12, 20), 1);
        var term2 = new Term("Spring Semester", new DateTime(2024, 12, 15), new DateTime(2025, 5, 15), 2);
        
        calendar.AddTerm(term1);

        // Act & Assert
        var act = () => calendar.AddTerm(term2);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Term dates overlap with existing term*");
    }

    [Fact]
    public void AddTerm_WithDuplicateSequenceNumber_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var calendar = CreateValidCalendar();
        var term1 = new Term("Fall Semester", new DateTime(2024, 8, 15), new DateTime(2024, 12, 20), 1);
        var term2 = new Term("Spring Semester", new DateTime(2025, 1, 15), new DateTime(2025, 5, 15), 1);
        
        calendar.AddTerm(term1);

        // Act & Assert
        var act = () => calendar.AddTerm(term2);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Term with sequence number * already exists");
    }

    [Fact]
    public void AddClosure_WithValidClosure_ShouldSucceed()
    {
        // Arrange
        var calendar = CreateValidCalendar();
        var closure = new Closure("Winter Break", new DateTime(2024, 12, 21), new DateTime(2024, 12, 31), false);

        // Act
        calendar.AddClosure(closure);

        // Assert
        calendar.Closures.Should().ContainSingle()
            .Which.Should().Be(closure);
        calendar.DomainEvents.Should().Contain(e => e is ClosureAddedEvent);
    }

    [Fact]
    public void AddClosure_WithNullClosure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var calendar = CreateValidCalendar();

        // Act & Assert
        var act = () => calendar.AddClosure(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateCompleteness_WithCompleteCalendar_ShouldReturnTrue()
    {
        // Arrange
        var calendar = CreateValidCalendar();
        var term1 = new Term("Fall Semester", new DateTime(2024, 8, 15), new DateTime(2024, 12, 20), 1);
        var term2 = new Term("Spring Semester", new DateTime(2025, 1, 15), new DateTime(2025, 5, 15), 2);
        
        calendar.AddTerm(term1);
        calendar.AddTerm(term2);

        // Act
        var isComplete = calendar.ValidateCompleteness();

        // Assert
        isComplete.Should().BeTrue();
    }

    [Fact]
    public void ValidateCompleteness_WithNoTerms_ShouldReturnFalse()
    {
        // Arrange
        var calendar = CreateValidCalendar();

        // Act
        var isComplete = calendar.ValidateCompleteness();

        // Assert
        isComplete.Should().BeFalse();
    }

    [Fact]
    public void GetInstructionalDays_WithTermsAndClosures_ShouldCalculateCorrectly()
    {
        // Arrange
        var calendar = CreateValidCalendar();
        var term = new Term("Fall Semester", new DateTime(2024, 8, 15), new DateTime(2024, 12, 20), 1);
        var closure = new Closure("Thanksgiving Break", new DateTime(2024, 11, 25), new DateTime(2024, 11, 29), false);
        
        calendar.AddTerm(term);
        calendar.AddClosure(closure);

        // Act
        var instructionalDays = calendar.GetInstructionalDays(term.Id);

        // Assert
        // Should calculate business days minus closure days
        instructionalDays.Should().BePositive();
    }

    [Fact]
    public void CopyToSchoolYear_WithValidTargetYear_ShouldCreateNewCalendar()
    {
        // Arrange
        var sourceCalendar = CreateValidCalendar();
        var term1 = new Term("Fall Semester", new DateTime(2024, 8, 15), new DateTime(2024, 12, 20), 1);
        var term2 = new Term("Spring Semester", new DateTime(2025, 1, 15), new DateTime(2025, 5, 15), 2);
        var closure = new Closure("Winter Break", new DateTime(2024, 12, 21), new DateTime(2024, 12, 31), false);
        
        sourceCalendar.AddTerm(term1);
        sourceCalendar.AddTerm(term2);
        sourceCalendar.AddClosure(closure);

        var targetSchoolYearId = Guid.NewGuid();
        var dayOffset = 365; // Move one year forward

        // Act
        var newCalendar = sourceCalendar.CopyToSchoolYear(targetSchoolYearId, dayOffset, "admin-user");

        // Assert
        newCalendar.SchoolYearId.Should().Be(targetSchoolYearId);
        newCalendar.Terms.Should().HaveCount(2);
        newCalendar.Closures.Should().HaveCount(1);
        
        // Verify dates are shifted
        var copiedTerm1 = newCalendar.Terms.First(t => t.SequenceNumber == 1);
        copiedTerm1.StartDate.Should().Be(term1.StartDate.AddDays(dayOffset));
        copiedTerm1.EndDate.Should().Be(term1.EndDate.AddDays(dayOffset));
        
        newCalendar.DomainEvents.Should().Contain(e => e is AcademicCalendarCopiedEvent);
    }

    [Fact]
    public void RemoveTerm_WithExistingTerm_ShouldRemoveSuccessfully()
    {
        // Arrange
        var calendar = CreateValidCalendar();
        var term = new Term("Fall Semester", new DateTime(2024, 8, 15), new DateTime(2024, 12, 20), 1);
        calendar.AddTerm(term);

        // Act
        calendar.RemoveTerm(term.Id);

        // Assert
        calendar.Terms.Should().BeEmpty();
        calendar.DomainEvents.Should().Contain(e => e is TermRemovedEvent);
    }

    [Fact]
    public void RemoveTerm_WithNonExistentTerm_ShouldThrowArgumentException()
    {
        // Arrange
        var calendar = CreateValidCalendar();
        var nonExistentTermId = Guid.NewGuid();

        // Act & Assert
        var act = () => calendar.RemoveTerm(nonExistentTermId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Term not found*");
    }

    private static AcademicCalendar CreateValidCalendar()
    {
        return new AcademicCalendar(Guid.NewGuid());
    }
}