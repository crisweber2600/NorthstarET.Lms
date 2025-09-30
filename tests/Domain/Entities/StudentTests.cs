using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class StudentTests
{
    [Fact] public void Constructor_WithValidData_ShouldCreateStudent() => Assert.Fail("Student entity not implemented - expected as per BDD-first requirement");
    [Fact] public void StudentNumber_ShouldBeUniquePerDistrict() => Assert.Fail("Student number uniqueness not implemented - expected as per BDD-first requirement");
    [Fact] public void PromoteGrade_WithValidGrade_ShouldUpdateGradeLevelAndRaiseEvent() => Assert.Fail("Grade promotion not implemented - expected as per BDD-first requirement");
    [Fact] public void AddAccommodation_WithValidTag_ShouldAddToAccommodationTags() => Assert.Fail("Accommodation management not implemented - expected as per BDD-first requirement");
    [Fact] public void EnrollInClass_ShouldCreateEnrollment() => Assert.Fail("Class enrollment not implemented - expected as per BDD-first requirement");
    [Fact] public void GradeProgression_ShouldBeTrackedPerSchoolYear() => Assert.Fail("Grade progression tracking not implemented - expected as per BDD-first requirement");
    [Fact] public void Status_ShouldControlEnrollmentEligibility() => Assert.Fail("Enrollment eligibility control not implemented - expected as per BDD-first requirement");
}