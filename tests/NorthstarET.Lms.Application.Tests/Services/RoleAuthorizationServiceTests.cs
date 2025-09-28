using FluentAssertions;
using NorthstarET.Lms.Application.Services;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Services;
using Moq;

namespace NorthstarET.Lms.Application.Tests.Services;

public class RoleAuthorizationServiceTests
{
    private readonly Mock<IRoleDefinitionRepository> _mockRoleDefinitionRepository;
    private readonly Mock<IRoleAssignmentRepository> _mockRoleAssignmentRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantContextAccessor> _mockTenantContext;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly RoleAuthorizationService _roleAuthorizationService;

    public RoleAuthorizationServiceTests()
    {
        _mockRoleDefinitionRepository = new Mock<IRoleDefinitionRepository>();
        _mockRoleAssignmentRepository = new Mock<IRoleAssignmentRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTenantContext = new Mock<ITenantContextAccessor>();
        _mockAuditService = new Mock<IAuditService>();
        
        // This will fail until RoleAuthorizationService is implemented
        _roleAuthorizationService = new RoleAuthorizationService(
            _mockRoleDefinitionRepository.Object,
            _mockRoleAssignmentRepository.Object,
            _mockUnitOfWork.Object,
            _mockTenantContext.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task HasPermissionAsync_WithValidUserAndPermission_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "students.read";
        var roleDefinitionId = Guid.NewGuid();

        var roleDefinition = new RoleDefinition("Teacher", "Classroom teacher role", RoleScope.Class);
        roleDefinition.AddPermission(permission, "system");

        var roleAssignment = new RoleAssignment(userId, roleDefinitionId, Guid.NewGuid(), "admin-1");

        _mockRoleAssignmentRepository.Setup(x => x.GetActiveRolesByUserIdAsync(userId))
            .ReturnsAsync(new List<RoleAssignment> { roleAssignment });
        
        _mockRoleDefinitionRepository.Setup(x => x.GetByIdAsync(roleDefinitionId))
            .ReturnsAsync(roleDefinition);

        // Act & Assert - This will fail until RoleAuthorizationService.HasPermissionAsync is implemented
        var result = await _roleAuthorizationService.HasPermissionAsync(userId, permission);
        
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WithInvalidPermission_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "admin.delete";
        var roleDefinitionId = Guid.NewGuid();

        var roleDefinition = new RoleDefinition("Student", "Student role", RoleScope.Class);
        roleDefinition.AddPermission("students.read", "system");

        var roleAssignment = new RoleAssignment(userId, roleDefinitionId, Guid.NewGuid(), "admin-1");

        _mockRoleAssignmentRepository.Setup(x => x.GetActiveRolesByUserIdAsync(userId))
            .ReturnsAsync(new List<RoleAssignment> { roleAssignment });
        
        _mockRoleDefinitionRepository.Setup(x => x.GetByIdAsync(roleDefinitionId))
            .ReturnsAsync(roleDefinition);

        // Act & Assert - This will fail until RoleAuthorizationService is implemented
        var result = await _roleAuthorizationService.HasPermissionAsync(userId, permission);
        
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_WithMultipleRoles_ShouldReturnAllPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var teacherRoleId = Guid.NewGuid();
        var advisorRoleId = Guid.NewGuid();

        var teacherRole = new RoleDefinition("Teacher", "Classroom teacher", RoleScope.Class);
        teacherRole.AddPermission("students.read", "system");
        teacherRole.AddPermission("assignments.create", "system");

        var advisorRole = new RoleDefinition("Advisor", "Student advisor", RoleScope.School);
        advisorRole.AddPermission("students.read", "system");
        advisorRole.AddPermission("counseling.access", "system");

        var roleAssignments = new List<RoleAssignment>
        {
            new(userId, teacherRoleId, Guid.NewGuid(), "admin-1"),
            new(userId, advisorRoleId, Guid.NewGuid(), "admin-1")
        };

        _mockRoleAssignmentRepository.Setup(x => x.GetActiveRolesByUserIdAsync(userId))
            .ReturnsAsync(roleAssignments);
        
        _mockRoleDefinitionRepository.Setup(x => x.GetByIdAsync(teacherRoleId))
            .ReturnsAsync(teacherRole);
        _mockRoleDefinitionRepository.Setup(x => x.GetByIdAsync(advisorRoleId))
            .ReturnsAsync(advisorRole);

        // Act & Assert - This will fail until RoleAuthorizationService is implemented
        var result = await _roleAuthorizationService.GetEffectivePermissionsAsync(userId);
        
        result.Should().NotBeNull();
        result.Should().Contain("students.read");
        result.Should().Contain("assignments.create");
        result.Should().Contain("counseling.access");
        result.Should().HaveCount(3); // Duplicates should be removed
    }

    [Fact]
    public async Task AssignRoleAsync_WithValidData_ShouldCreateRoleAssignment()
    {
        // Arrange
        var assignRoleDto = new AssignRoleDto
        {
            UserId = Guid.NewGuid(),
            RoleDefinitionId = Guid.NewGuid(),
            SchoolId = Guid.NewGuid(),
            ClassId = Guid.NewGuid(),
            ExpirationDate = DateTime.UtcNow.AddMonths(12)
        };

        var roleDefinition = new RoleDefinition("Teacher", "Classroom teacher", RoleScope.Class);
        
        _mockRoleDefinitionRepository.Setup(x => x.GetByIdAsync(assignRoleDto.RoleDefinitionId))
            .ReturnsAsync(roleDefinition);
        
        _mockRoleAssignmentRepository.Setup(x => x.GetActiveRoleAssignmentAsync(
            assignRoleDto.UserId, assignRoleDto.RoleDefinitionId, assignRoleDto.SchoolId, assignRoleDto.ClassId))
            .ReturnsAsync((RoleAssignment?)null);

        // Act & Assert - This will fail until RoleAuthorizationService.AssignRoleAsync is implemented
        var result = await _roleAuthorizationService.AssignRoleAsync(assignRoleDto, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(assignRoleDto.UserId);
        result.Value.RoleDefinitionId.Should().Be(assignRoleDto.RoleDefinitionId);
        
        _mockRoleAssignmentRepository.Verify(x => x.AddAsync(It.IsAny<RoleAssignment>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RevokeRoleAsync_WithValidAssignment_ShouldRevokeRole()
    {
        // Arrange
        var roleAssignmentId = Guid.NewGuid();
        var reason = "Role no longer needed";
        var roleAssignment = new RoleAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "admin-1");

        _mockRoleAssignmentRepository.Setup(x => x.GetByIdAsync(roleAssignmentId))
            .ReturnsAsync(roleAssignment);

        // Act & Assert - This will fail until RoleAuthorizationService.RevokeRoleAsync is implemented
        var result = await _roleAuthorizationService.RevokeRoleAsync(roleAssignmentId, reason, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ValidateRoleScopeAsync_WithValidScope_ShouldReturnTrue()
    {
        // Arrange
        var roleDefinition = new RoleDefinition("Teacher", "Class teacher", RoleScope.Class);
        var roleAssignment = new RoleAssignment(
            Guid.NewGuid(), 
            roleDefinition.Id, 
            Guid.NewGuid(), 
            "admin-1",
            Guid.NewGuid()); // ClassId provided for Class-scoped role

        _mockRoleDefinitionRepository.Setup(x => x.GetByIdAsync(roleDefinition.Id))
            .ReturnsAsync(roleDefinition);

        // Act & Assert - This will fail until RoleAuthorizationService.ValidateRoleScopeAsync is implemented
        var result = await _roleAuthorizationService.ValidateRoleScopeAsync(roleAssignment);
        
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRoleScopeAsync_WithInvalidScope_ShouldReturnFalse()
    {
        // Arrange
        var roleDefinition = new RoleDefinition("Teacher", "Class teacher", RoleScope.Class);
        var roleAssignment = new RoleAssignment(
            Guid.NewGuid(), 
            roleDefinition.Id, 
            Guid.NewGuid(), 
            "admin-1"); // No ClassId for Class-scoped role

        _mockRoleDefinitionRepository.Setup(x => x.GetByIdAsync(roleDefinition.Id))
            .ReturnsAsync(roleDefinition);

        // Act & Assert - This will fail until RoleAuthorizationService is implemented
        var result = await _roleAuthorizationService.ValidateRoleScopeAsync(roleAssignment);
        
        result.Should().BeFalse();
    }
}