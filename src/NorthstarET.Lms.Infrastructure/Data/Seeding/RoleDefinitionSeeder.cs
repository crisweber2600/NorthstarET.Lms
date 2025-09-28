using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Infrastructure.Data;
using System.Text.Json;

namespace NorthstarET.Lms.Infrastructure.Data.Seeding;

/// <summary>
/// Seeds system role definitions for hierarchical RBAC in the educational domain.
/// Defines roles at Platform, District, School, and Class levels with appropriate permissions.
/// </summary>
public static class RoleDefinitionSeeder
{
    public static async Task SeedAsync(LmsDbContext context)
    {
        // Check if role definitions already exist
        if (await context.RoleDefinitions.AnyAsync())
        {
            return;
        }

        var roleDefinitions = new List<RoleDefinition>
        {
            // Platform-level roles (system administration) - using proper constructor
            new RoleDefinition(
                name: "PlatformAdmin", 
                description: "Platform-wide administrator with full system access across all districts",
                scope: RoleScope.Platform,
                isSystemRole: true
            ),

            new RoleDefinition(
                name: "PlatformSupport", 
                description: "Platform support role with read-only access for troubleshooting",
                scope: RoleScope.Platform,
                isSystemRole: true
            ),

            // District-level roles
            new RoleDefinition(
                name: "DistrictAdmin", 
                description: "District administrator with full access within their district",
                scope: RoleScope.District,
                isSystemRole: false
            ),

            new RoleDefinition(
                name: "DistrictIT", 
                description: "District IT specialist with technical system access",
                scope: RoleScope.District,
                isSystemRole: false
            ),

            // School-level roles
            new RoleDefinition(
                name: "SchoolAdmin", 
                description: "School administrator with full access to their school",
                scope: RoleScope.School,
                isSystemRole: false
            ),

            new RoleDefinition(
                name: "SchoolSecretary", 
                description: "School administrative staff with student data access",
                scope: RoleScope.School,
                isSystemRole: false
            ),

            // Class-level roles  
            new RoleDefinition(
                name: "Teacher", 
                description: "Classroom teacher with access to their assigned classes",
                scope: RoleScope.Class,
                isSystemRole: false
            ),

            new RoleDefinition(
                name: "SubstituteTeacher", 
                description: "Substitute teacher with temporary class access",
                scope: RoleScope.Class,
                isSystemRole: false
            )
        };

        // Add permissions to roles after creation
        var platformAdmin = roleDefinitions.First(r => r.Name == "PlatformAdmin");
        platformAdmin.AddPermission("platform.districts.create", "system");
        platformAdmin.AddPermission("platform.districts.read", "system");
        platformAdmin.AddPermission("platform.districts.update", "system");
        platformAdmin.AddPermission("platform.districts.delete", "system");
        platformAdmin.AddPermission("platform.audit.access", "system");
        platformAdmin.AddPermission("platform.compliance.manage", "system");

        var districtAdmin = roleDefinitions.First(r => r.Name == "DistrictAdmin");
        districtAdmin.AddPermission("district.schools.manage", "system");
        districtAdmin.AddPermission("district.users.manage", "system");
        districtAdmin.AddPermission("district.data.access", "system");
        districtAdmin.AddPermission("district.reports.view", "system");

        var teacher = roleDefinitions.First(r => r.Name == "Teacher");
        teacher.AddPermission("class.students.view", "system");
        teacher.AddPermission("class.grades.manage", "system");
        teacher.AddPermission("class.attendance.record", "system");

        // Save all role definitions
        await context.RoleDefinitions.AddRangeAsync(roleDefinitions);
        await context.SaveChangesAsync();
    }
}