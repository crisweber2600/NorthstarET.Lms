using FluentAssertions;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Domain.Tests.ValueObjects;

public class DistrictQuotasTests
{
    [Fact]
    public void CreateDistrictQuotas_WithValidValues_ShouldSucceed()
    {
        // Arrange
        var maxStudents = 50000;
        var maxStaff = 5000;
        var maxAdmins = 100;

        // Act
        var quotas = new DistrictQuotas 
        { 
            MaxStudents = maxStudents, 
            MaxStaff = maxStaff, 
            MaxAdmins = maxAdmins 
        };

        // Assert
        quotas.MaxStudents.Should().Be(maxStudents);
        quotas.MaxStaff.Should().Be(maxStaff);
        quotas.MaxAdmins.Should().Be(maxAdmins);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CreateDistrictQuotas_WithInvalidMaxStudents_ShouldThrowArgumentException(int invalidMaxStudents)
    {
        // Act & Assert
        var act = () => new DistrictQuotas 
        { 
            MaxStudents = invalidMaxStudents, 
            MaxStaff = 1000, 
            MaxAdmins = 10 
        };
        act.Should().Throw<ArgumentException>()
           .WithMessage("*MaxStudents*positive*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateDistrictQuotas_WithInvalidMaxStaff_ShouldThrowArgumentException(int invalidMaxStaff)
    {
        // Act & Assert
        var act = () => new DistrictQuotas 
        { 
            MaxStudents = 1000, 
            MaxStaff = invalidMaxStaff, 
            MaxAdmins = 10 
        };
        act.Should().Throw<ArgumentException>()
           .WithMessage("*MaxStaff*positive*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateDistrictQuotas_WithInvalidMaxAdmins_ShouldThrowArgumentException(int invalidMaxAdmins)
    {
        // Act & Assert
        var act = () => new DistrictQuotas 
        { 
            MaxStudents = 1000, 
            MaxStaff = 100, 
            MaxAdmins = invalidMaxAdmins 
        };
        act.Should().Throw<ArgumentException>()
           .WithMessage("*MaxAdmins*positive*");
    }

    [Fact]
    public void TwoDistrictQuotas_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var quotas1 = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };
        var quotas2 = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };

        // Act & Assert
        quotas1.Should().Be(quotas2);
        quotas1.GetHashCode().Should().Be(quotas2.GetHashCode());
    }

    [Fact]
    public void TwoDistrictQuotas_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var quotas1 = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };
        var quotas2 = new DistrictQuotas { MaxStudents = 2000, MaxStaff = 100, MaxAdmins = 10 };

        // Act & Assert
        quotas1.Should().NotBe(quotas2);
    }

    [Fact]
    public void IsWithinLimits_WhenAllValuesUnderLimits_ShouldReturnTrue()
    {
        // Arrange
        var quotas = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };
        
        // Act & Assert
        quotas.IsWithinLimits(500, 50, 5).Should().BeTrue();
    }

    [Fact]
    public void IsWithinLimits_WhenStudentsExceedLimit_ShouldReturnFalse()
    {
        // Arrange
        var quotas = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };
        
        // Act & Assert
        quotas.IsWithinLimits(1500, 50, 5).Should().BeFalse();
    }

    [Fact]
    public void IsWithinLimits_WhenStaffExceedLimit_ShouldReturnFalse()
    {
        // Arrange
        var quotas = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };
        
        // Act & Assert
        quotas.IsWithinLimits(500, 150, 5).Should().BeFalse();
    }

    [Fact]
    public void IsWithinLimits_WhenAdminsExceedLimit_ShouldReturnFalse()
    {
        // Arrange
        var quotas = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };
        
        // Act & Assert
        quotas.IsWithinLimits(500, 50, 15).Should().BeFalse();
    }

    [Fact]
    public void CalculateUtilization_ShouldReturnCorrectPercentages()
    {
        // Arrange
        var quotas = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };
        
        // Act
        var utilization = quotas.CalculateUtilization(500, 25, 2);

        // Assert
        utilization.StudentUtilization.Should().Be(50.0);
        utilization.StaffUtilization.Should().Be(25.0);
        utilization.AdminUtilization.Should().Be(20.0);
    }
}