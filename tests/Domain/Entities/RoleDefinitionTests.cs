using FluentAssertions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Tests.Domain.Entities;

public class RoleDefinitionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateRoleDefinition()
    {
        // Arrange
        var tenantSlug = "test-district";
        var name = "Teacher";
        var description = "Classroom teacher role";
        var allowedScopes = new List<RoleScopeType> { RoleScopeType.Class, RoleScopeType.School };
        var permissions = new List<Permission> 
        { 
            new Permission("student:read", "Read student information"),
            new Permission("grade:write", "Write grades")
        };
        var createdBy = "admin@test.com";

        // Act
        var roleDefinition = new RoleDefinition(tenantSlug, name, description, allowedScopes, permissions, createdBy);

        // Assert
        roleDefinition.Should().NotBeNull();
        roleDefinition.Name.Should().Be(name);
        roleDefinition.Description.Should().Be(description);
        roleDefinition.AllowedScopes.Should().HaveCount(2);
        roleDefinition.Permissions.Should().HaveCount(2);
        roleDefinition.IsActive.Should().BeTrue();
        roleDefinition.Version.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var tenantSlug = "test-district";
        var allowedScopes = new List<RoleScopeType> { RoleScopeType.Class };
        var permissions = new List<Permission> { new Permission("test:read", "Test") };
        var createdBy = "admin@test.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new RoleDefinition(tenantSlug, "", "Description", allowedScopes, permissions, createdBy));
    }

    [Fact]
    public void UpdatePermissions_WithNewPermissions_ShouldUpdatePermissionsAndIncrementVersion()
    {
        // Arrange
        var roleDefinition = new RoleDefinition("test-district", "Teacher", "Teacher role",
            new List<RoleScopeType> { RoleScopeType.Class },
            new List<Permission> { new Permission("student:read", "Read") },
            "admin@test.com");
        var newPermissions = new List<Permission> 
        { 
            new Permission("student:read", "Read"),
            new Permission("grade:write", "Write")
        };

        // Act
        roleDefinition.UpdatePermissions(newPermissions, "admin@test.com");

        // Assert
        roleDefinition.Permissions.Should().HaveCount(2);
        roleDefinition.Version.Should().Be(2);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var roleDefinition = new RoleDefinition("test-district", "Teacher", "Teacher role",
            new List<RoleScopeType> { RoleScopeType.Class },
            new List<Permission> { new Permission("student:read", "Read") },
            "admin@test.com");

        // Act
        roleDefinition.Deactivate("admin@test.com");

        // Assert
        roleDefinition.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AllowedScopes_ShouldControlWhereRoleCanBeAssigned()
    {
        // Arrange
        var allowedScopes = new List<RoleScopeType> { RoleScopeType.District, RoleScopeType.School };

        // Act
        var roleDefinition = new RoleDefinition("test-district", "Admin", "Admin role",
            allowedScopes,
            new List<Permission> { new Permission("all:admin", "Admin") },
            "admin@test.com");

        // Assert
        roleDefinition.AllowedScopes.Should().Contain(RoleScopeType.District);
        roleDefinition.AllowedScopes.Should().Contain(RoleScopeType.School);
        roleDefinition.AllowedScopes.Should().NotContain(RoleScopeType.Class);
    }

    [Fact]
    public void Version_ShouldTrackPermissionChanges()
    {
        // Arrange
        var roleDefinition = new RoleDefinition("test-district", "Teacher", "Teacher role",
            new List<RoleScopeType> { RoleScopeType.Class },
            new List<Permission> { new Permission("student:read", "Read") },
            "admin@test.com");

        // Act
        roleDefinition.UpdatePermissions(
            new List<Permission> { new Permission("student:write", "Write") },
            "admin@test.com");
        roleDefinition.UpdatePermissions(
            new List<Permission> { new Permission("grade:read", "Read") },
            "admin@test.com");

        // Assert
        roleDefinition.Version.Should().Be(3);
    }
}
