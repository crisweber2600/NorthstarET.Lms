using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Tests.Entities;

public class DistrictTenantTests
{
    [Fact]
    public void CreateDistrict_WithValidData_ShouldSucceed()
    {
        // Arrange
        var slug = "oakland-unified";
        var displayName = "Oakland Unified School District";
        var quotas = new DistrictQuotas { MaxStudents = 50000, MaxStaff = 5000, MaxAdmins = 100 };
        var createdBy = "platform-admin-123";

        // Act
        var district = new DistrictTenant(slug, displayName, quotas, createdBy);

        // Assert
        district.Slug.Should().Be(slug);
        district.DisplayName.Should().Be(displayName);
        district.Status.Should().Be(DistrictStatus.Active);
        district.Quotas.Should().Be(quotas);
        district.CreatedByUserId.Should().Be(createdBy);
        district.Id.Should().NotBeEmpty();
        district.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("INVALID-UPPER")]
    [InlineData("invalid_underscore")]
    [InlineData("invalid spaces")]
    public void CreateDistrict_WithInvalidSlug_ShouldThrowArgumentException(string? invalidSlug)
    {
        // Arrange
        var displayName = "Test District";
        var quotas = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };

        // Act & Assert
        var act = () => new DistrictTenant(invalidSlug, displayName, quotas, "admin");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateDistrict_WithNullDisplayName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var slug = "valid-district";
        string displayName = null!;
        var quotas = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };

        // Act & Assert
        var act = () => new DistrictTenant(slug, displayName, quotas, "admin");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateDistrict_WithNullQuotas_ShouldThrowArgumentNullException()
    {
        // Arrange
        var slug = "valid-district";
        var displayName = "Valid District";
        DistrictQuotas quotas = null!;

        // Act & Assert
        var act = () => new DistrictTenant(slug, displayName, quotas, "admin");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateDistrict_ShouldGenerateDomainEvent()
    {
        // Arrange
        var slug = "oakland-unified";
        var displayName = "Oakland Unified School District";
        var quotas = new DistrictQuotas { MaxStudents = 50000, MaxStaff = 5000, MaxAdmins = 100 };
        var createdBy = "platform-admin-123";

        // Act
        var district = new DistrictTenant(slug, displayName, quotas, createdBy);

        // Assert
        district.DomainEvents.Should().ContainSingle();
        var domainEvent = district.DomainEvents.First();
        domainEvent.Should().BeOfType<DistrictProvisionedEvent>();
    }

    [Fact]
    public void Suspend_WhenActive_ShouldChangStatusAndGenerateEvent()
    {
        // Arrange
        var district = CreateValidDistrict();
        var reason = "Policy violation";
        var suspendedBy = "platform-admin-456";

        // Act
        district.Suspend(reason, suspendedBy);

        // Assert
        district.Status.Should().Be(DistrictStatus.Suspended);
        district.DomainEvents.Should().HaveCount(2); // Creation + Suspension
        var suspensionEvent = district.DomainEvents.Last();
        suspensionEvent.Should().BeOfType<DistrictSuspendedEvent>();
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var district = CreateValidDistrict();
        district.Suspend("First suspension", "admin-1");

        // Act & Assert
        var act = () => district.Suspend("Second suspension", "admin-2");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("District is already suspended");
    }

    private static DistrictTenant CreateValidDistrict()
    {
        return new DistrictTenant(
            "test-district",
            "Test District",
            new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 },
            "test-admin");
    }
}