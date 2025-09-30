using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class SchoolYearTests
{
    [Fact] public void Constructor_WithValidDates_ShouldCreateSchoolYear() => Assert.Fail("SchoolYear entity not implemented - expected as per BDD-first requirement");
    [Fact] public void Constructor_WithOverlappingDates_ShouldThrowException() => Assert.Fail("Date overlap validation not implemented - expected as per BDD-first requirement");
    [Fact] public void Archive_WhenActive_ShouldUpdateStatusAndLockChildren() => Assert.Fail("SchoolYear archiving not implemented - expected as per BDD-first requirement");
    [Fact] public void DateRange_ShouldNotOverlapExistingSchoolYears() => Assert.Fail("Date range validation not implemented - expected as per BDD-first requirement");
    [Fact] public void Status_ShouldControlChildEntityMutability() => Assert.Fail("Child entity lock control not implemented - expected as per BDD-first requirement");
}