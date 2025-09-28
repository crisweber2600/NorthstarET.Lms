using FluentAssertions;
using NorthstarET.Lms.Application.Services;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;
using Moq;

namespace NorthstarET.Lms.Application.Tests.Services;

public class DistrictServiceTests
{
    private readonly Mock<IDistrictRepository> _mockDistrictRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly DistrictService _districtService;

    public DistrictServiceTests()
    {
        _mockDistrictRepository = new Mock<IDistrictRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockAuditService = new Mock<IAuditService>();
        
        // This will fail until DistrictService is implemented
        _districtService = new DistrictService(
            _mockDistrictRepository.Object,
            _mockUnitOfWork.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task CreateDistrictAsync_WithValidData_ShouldCreateDistrict()
    {
        // Arrange
        var createDistrictDto = new CreateDistrictDto
        {
            Slug = "oakland-unified",
            DisplayName = "Oakland Unified School District",
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = 50000,
                MaxStaff = 5000,
                MaxAdmins = 100
            }
        };

        var quotas = new DistrictQuotas { MaxStudents = 10000, MaxStaff = 1000, MaxAdmins = 100 };
        var expectedDistrict = new DistrictTenant("oakland-unified", "Oakland Unified School District", quotas, "system-admin");
        
        _mockDistrictRepository.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync((DistrictTenant?)null);
        
        _mockDistrictRepository.Setup(x => x.AddAsync(It.IsAny<DistrictTenant>()))
            .Returns(Task.CompletedTask);

        // Act & Assert - This will fail until DistrictService.CreateDistrictAsync is implemented
        var result = await _districtService.CreateDistrictAsync(createDistrictDto, "platform-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("oakland-unified");
        result.Value.DisplayName.Should().Be("Oakland Unified School District");
        
        _mockDistrictRepository.Verify(x => x.AddAsync(It.IsAny<DistrictTenant>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateDistrictAsync_WithDuplicateSlug_ShouldReturnFailure()
    {
        // Arrange
        var createDistrictDto = new CreateDistrictDto
        {
            Slug = "existing-district",
            DisplayName = "Existing District"
        };

        var existingQuotas = new DistrictQuotas { MaxStudents = 5000, MaxStaff = 500, MaxAdmins = 50 };
        var existingDistrict = new DistrictTenant("existing-district", "Existing District", existingQuotas, "system-admin");
        _mockDistrictRepository.Setup(x => x.GetBySlugAsync("existing-district"))
            .ReturnsAsync(existingDistrict);

        // Act & Assert - This will fail until DistrictService is implemented
        var result = await _districtService.CreateDistrictAsync(createDistrictDto, "platform-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task GetDistrictAsync_WithValidId_ShouldReturnDistrict()
    {
        // Arrange
        var districtId = Guid.NewGuid();
        var quotas = new DistrictQuotas { MaxStudents = 10000, MaxStaff = 1000, MaxAdmins = 100 };
        var expectedDistrict = new DistrictTenant("test-district", "Test District", quotas, "system-admin");
        
        _mockDistrictRepository.Setup(x => x.GetByIdAsync(districtId))
            .ReturnsAsync(expectedDistrict);

        // Act & Assert - This will fail until DistrictService is implemented
        var result = await _districtService.GetDistrictAsync(districtId);
        
        result.Should().NotBeNull();
        result.Slug.Should().Be("test-district");
        result.DisplayName.Should().Be("Test District");
    }

    [Fact]
    public async Task UpdateDistrictQuotasAsync_WithValidData_ShouldUpdateQuotas()
    {
        // Arrange
        var districtId = Guid.NewGuid();
        var districtQuotas = new DistrictQuotas { MaxStudents = 10000, MaxStaff = 1000, MaxAdmins = 100 };
        var district = new DistrictTenant("test-district", "Test District", districtQuotas, "system-admin");
        var newQuotas = new DistrictQuotas { MaxStudents = 60000, MaxStaff = 6000, MaxAdmins = 150 };

        _mockDistrictRepository.Setup(x => x.GetByIdAsync(districtId))
            .ReturnsAsync(district);

        // Act & Assert - This will fail until DistrictService is implemented
        var result = await _districtService.UpdateDistrictQuotasAsync(districtId, newQuotas, "platform-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SuspendDistrictAsync_WithValidId_ShouldSuspendDistrict()
    {
        // Arrange
        var districtId = Guid.NewGuid();
        var quotas = new DistrictQuotas { MaxStudents = 10000, MaxStaff = 1000, MaxAdmins = 100 };
        var district = new DistrictTenant("test-district", "Test District", quotas, "system-admin");
        var reason = "Policy violation";

        _mockDistrictRepository.Setup(x => x.GetByIdAsync(districtId))
            .ReturnsAsync(district);

        // Act & Assert - This will fail until DistrictService is implemented
        var result = await _districtService.SuspendDistrictAsync(districtId, reason, "platform-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteDistrictAsync_WithActiveRetention_ShouldReturnFailure()
    {
        // Arrange
        var districtId = Guid.NewGuid();
        var quotas = new DistrictQuotas { MaxStudents = 10000, MaxStaff = 1000, MaxAdmins = 100 };
        var district = new DistrictTenant("test-district", "Test District", quotas, "system-admin");

        _mockDistrictRepository.Setup(x => x.GetByIdAsync(districtId))
            .ReturnsAsync(district);
        
        _mockDistrictRepository.Setup(x => x.HasActiveRetentionPoliciesAsync(districtId))
            .ReturnsAsync(true);

        // Act & Assert - This will fail until DistrictService is implemented
        var result = await _districtService.DeleteDistrictAsync(districtId, "platform-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("retention policies");
    }
}