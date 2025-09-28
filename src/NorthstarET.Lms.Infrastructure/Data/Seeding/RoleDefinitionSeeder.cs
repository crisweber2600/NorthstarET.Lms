using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Domain.Entities;
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
            // Platform-level roles (system administration)
            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "PlatformAdmin",
                Description = "Platform-wide administrator with full system access across all districts",
                Scope = RoleScope.Platform,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "platform.districts.create",
                    "platform.districts.read",
                    "platform.districts.update", 
                    "platform.districts.delete",
                    "platform.districts.suspend",
                    "platform.districts.reactivate",
                    "platform.users.manage",
                    "platform.audit.access",
                    "platform.compliance.manage",
                    "platform.quotas.modify",
                    "platform.system.configure"
                }),
                IsSystemRole = true,
                AllowsDelegation = false, // Platform admin cannot be delegated
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "PlatformSupport",
                Description = "Platform support role with read-only access for troubleshooting",
                Scope = RoleScope.Platform,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "platform.districts.read",
                    "platform.users.read",
                    "platform.audit.read",
                    "platform.system.monitor"
                }),
                IsSystemRole = true,
                AllowsDelegation = false,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            // District-level roles (tenant administration)
            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DistrictAdmin",
                Description = "District administrator with full access to district data and users",
                Scope = RoleScope.District,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "district.schools.create",
                    "district.schools.read",
                    "district.schools.update",
                    "district.schools.delete",
                    "district.users.create",
                    "district.users.read", 
                    "district.users.update",
                    "district.users.delete",
                    "district.students.create",
                    "district.students.read",
                    "district.students.update",
                    "district.students.transfer",
                    "district.staff.create",
                    "district.staff.read",
                    "district.staff.update",
                    "district.staff.terminate",
                    "district.roles.assign",
                    "district.roles.revoke",
                    "district.classes.create",
                    "district.classes.read",
                    "district.classes.update",
                    "district.enrollments.manage",
                    "district.assessments.manage",
                    "district.audit.read",
                    "district.compliance.manage",
                    "district.reports.access",
                    "district.bulk.operations"
                }),
                IsSystemRole = true,
                AllowsDelegation = true,
                TenantId = "platform", // Default for all tenants
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DistrictDataManager",
                Description = "District data management specialist for imports, exports, and data integrity",
                Scope = RoleScope.District,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "district.students.read",
                    "district.students.update",
                    "district.staff.read",
                    "district.staff.update",
                    "district.bulk.import",
                    "district.bulk.export",
                    "district.reports.generate",
                    "district.data.validate"
                }),
                IsSystemRole = true,
                AllowsDelegation = true,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "ComplianceOfficer",
                Description = "District compliance and audit specialist with access to sensitive audit data",
                Scope = RoleScope.District,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "district.audit.read",
                    "district.audit.export",
                    "district.compliance.read",
                    "district.compliance.report",
                    "district.retention.manage",
                    "district.legal.holds",
                    "district.privacy.review"
                }),
                IsSystemRole = true,
                AllowsDelegation = false, // Compliance role should not be delegated
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            // School-level roles (school administration)
            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "SchoolPrincipal",
                Description = "School principal with full administrative access to school operations",
                Scope = RoleScope.School,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "school.students.read",
                    "school.students.update",
                    "school.students.transfer",
                    "school.staff.read",
                    "school.staff.manage",
                    "school.classes.create",
                    "school.classes.read",
                    "school.classes.update",
                    "school.classes.delete",
                    "school.enrollments.manage",
                    "school.assessments.read",
                    "school.assessments.assign",
                    "school.reports.access",
                    "school.calendar.manage",
                    "school.roles.assign.teacher",
                    "school.roles.assign.staff"
                }),
                IsSystemRole = true,
                AllowsDelegation = true,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "SchoolUser",
                Description = "General school administrative user with limited administrative access",
                Scope = RoleScope.School,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "school.students.read",
                    "school.students.update",
                    "school.classes.read",
                    "school.enrollments.read",
                    "school.enrollments.update",
                    "school.assessments.read",
                    "school.reports.basic"
                }),
                IsSystemRole = true,
                AllowsDelegation = true,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "SchoolSecretary",
                Description = "School secretary with enrollment and student information management access",
                Scope = RoleScope.School,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "school.students.create",
                    "school.students.read",
                    "school.students.update",
                    "school.enrollments.create",
                    "school.enrollments.read",
                    "school.enrollments.update",
                    "school.guardians.read",
                    "school.guardians.update",
                    "school.reports.enrollment"
                }),
                IsSystemRole = true,
                AllowsDelegation = true,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            // Class/Teacher-level roles (classroom instruction)
            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Teacher",
                Description = "Classroom teacher with access to assigned students and classes",
                Scope = RoleScope.Class,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "class.students.read",
                    "class.students.basic.update", // Limited student info updates
                    "class.enrollments.read",
                    "class.assessments.read",
                    "class.assessments.assign",
                    "class.grades.read",
                    "class.grades.update",
                    "class.attendance.read",
                    "class.attendance.update",
                    "class.reports.student.progress"
                }),
                IsSystemRole = true,
                AllowsDelegation = true,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "SubstituteTeacher",
                Description = "Substitute teacher with temporary, limited access to classroom data",
                Scope = RoleScope.Class,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "class.students.read",
                    "class.enrollments.read",
                    "class.assessments.read",
                    "class.attendance.read",
                    "class.attendance.update"
                }),
                IsSystemRole = true,
                AllowsDelegation = false, // Substitute access should be explicitly granted
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "TeacherAide",
                Description = "Teaching assistant with read-only access to support classroom instruction",
                Scope = RoleScope.Class,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "class.students.read",
                    "class.enrollments.read",
                    "class.assessments.read",
                    "class.attendance.read"
                }),
                IsSystemRole = true,
                AllowsDelegation = true,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            // Specialized roles
            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Counselor",
                Description = "School counselor with extended student access for academic and personal support",
                Scope = RoleScope.School,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "school.students.read",
                    "school.students.counseling.update",
                    "school.enrollments.read",
                    "school.enrollments.counseling.update",
                    "school.assessments.read",
                    "school.grades.read",
                    "school.attendance.read",
                    "school.guardians.read",
                    "school.reports.counseling",
                    "school.privacy.notes.manage" // Counseling notes
                }),
                IsSystemRole = true,
                AllowsDelegation = false, // Counseling role requires specific qualifications
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            },

            new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "SpecialEducationCoordinator",
                Description = "Special education coordinator with access to IEP and accommodation data",
                Scope = RoleScope.District,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "district.students.special.education.read",
                    "district.students.special.education.update",
                    "district.students.accommodations.manage",
                    "district.students.iep.manage",
                    "district.assessments.accommodations.read",
                    "district.reports.special.education",
                    "district.compliance.special.education"
                }),
                IsSystemRole = true,
                AllowsDelegation = false,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            }
        };

        await context.RoleDefinitions.AddRangeAsync(roleDefinitions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds custom district-specific roles if needed
    /// </summary>
    public static async Task SeedDistrictCustomRolesAsync(LmsDbContext context, string tenantId, string districtName)
    {
        // Districts can define custom roles beyond the system defaults
        var customRoles = new List<RoleDefinition>();

        // Example: Charter schools might need additional oversight roles
        if (districtName.Contains("Charter"))
        {
            customRoles.Add(new RoleDefinition
            {
                Id = Guid.NewGuid(),
                Name = "CharterBoardMember",
                Description = "Charter school board member with governance oversight access",
                Scope = RoleScope.District,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    "district.students.aggregate.read", // No PII, only aggregate data
                    "district.staff.aggregate.read",
                    "district.budget.read",
                    "district.compliance.read",
                    "district.reports.governance"
                }),
                IsSystemRole = false, // District-specific role
                AllowsDelegation = false,
                TenantId = tenantId,
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow
            });
        }

        if (customRoles.Any())
        {
            await context.RoleDefinitions.AddRangeAsync(customRoles);
            await context.SaveChangesAsync();
        }
    }
}