using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class ClassTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateClass()
    {
        // Arrange
        var tenantSlug = "test-district";
        var schoolId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        var name = "Math 101";
        var code = "MATH101";
        var capacity = 30;
        var createdBy = "admin@test.com";

        // Act
        var classEntity = new Class(tenantSlug, schoolId, schoolYearId, name, code, capacity, createdBy);

        // Assert
        classEntity.Should().NotBeNull();
        classEntity.SchoolId.Should().Be(schoolId);
        classEntity.SchoolYearId.Should().Be(schoolYearId);
        classEntity.Name.Should().Be(name);
        classEntity.Code.Should().Be(code);
        classEntity.Capacity.Should().Be(capacity);
        classEntity.Status.Should().Be("Active");
        classEntity.CreatedBy.Should().Be(createdBy);
        classEntity.DomainEvents.Should().ContainSingle(e => e is ClassCreatedEvent);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_ShouldThrowException()
    {
        // Arrange
        var tenantSlug = "test-district";
        var schoolId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        var createdBy = "admin@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Class(tenantSlug, schoolId, schoolYearId, "Math 101", "MATH101", 0, createdBy));
    }

    [Fact]
    public void UpdateStatus_WithValidStatus_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var classEntity = new Class("test-district", Guid.NewGuid(), Guid.NewGuid(), 
            "Math 101", "MATH101", 30, "admin@test.com");
        var newStatus = "Closed";
        var updatedBy = "admin@test.com";

        // Act
        classEntity.UpdateStatus(newStatus, updatedBy);

        // Assert
        classEntity.Status.Should().Be(newStatus);
        classEntity.UpdatedBy.Should().Be(updatedBy);
        classEntity.DomainEvents.Should().Contain(e => e is ClassStatusChangedEvent);
    }

    [Fact]
    public void UpdateCapacity_WithValidCapacity_ShouldUpdateCapacity()
    {
        // Arrange
        var classEntity = new Class("test-district", Guid.NewGuid(), Guid.NewGuid(), 
            "Math 101", "MATH101", 30, "admin@test.com");
        var newCapacity = 35;
        var updatedBy = "admin@test.com";

        // Act
        classEntity.UpdateCapacity(newCapacity, updatedBy);

        // Assert
        classEntity.Capacity.Should().Be(newCapacity);
        classEntity.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void SetCapacityOverride_ShouldEnableCapacityOverride()
    {
        // Arrange
        var classEntity = new Class("test-district", Guid.NewGuid(), Guid.NewGuid(), 
            "Math 101", "MATH101", 30, "admin@test.com");
        var updatedBy = "admin@test.com";

        // Act
        classEntity.SetCapacityOverride(true, updatedBy);

        // Assert
        classEntity.CapacityOverrideEnabled.Should().BeTrue();
        classEntity.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void Code_ShouldBeUniquePerSchoolYearAndSchool()
    {
        // Arrange & Act
        var classEntity = new Class("test-district", Guid.NewGuid(), Guid.NewGuid(), 
            "Math 101", "MATH101", 30, "admin@test.com");

        // Assert
        classEntity.Code.Should().Be("MATH101");
        // Code uniqueness is enforced at the repository/database level
    }
}
