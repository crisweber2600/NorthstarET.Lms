using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Infrastructure.Data;

namespace NorthstarET.Lms.Infrastructure.Data.Seeding;

/// <summary>
/// Seeds default FERPA-compliant retention policies for all entity types.
/// These policies define how long data must be retained before it can be purged.
/// </summary>
public static class RetentionPolicySeeder
{
    public static async Task SeedAsync(LmsDbContext context)
    {
        // Check if retention policies already exist
        if (await context.RetentionPolicies.AnyAsync())
        {
            return;
        }

        var retentionPolicies = new List<RetentionPolicy>
        {
            // Student data retention (7 years per FERPA)
            new RetentionPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "Student",
                RetentionYears = 7,
                IsDefault = true,
                EffectiveDate = DateTime.UtcNow,
                TenantId = "platform", // Platform-level default
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow,
                Description = "FERPA-compliant retention policy for student educational records. " +
                            "Student records must be retained for a minimum of 7 years after graduation or withdrawal."
            },

            // Staff data retention (5 years)
            new RetentionPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "Staff",
                RetentionYears = 5,
                IsDefault = true,
                EffectiveDate = DateTime.UtcNow,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow,
                Description = "Employment records retention policy. Staff records retained for 5 years " +
                            "after termination to comply with employment law requirements."
            },

            // Assessment data retention (3 years)
            new RetentionPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "Assessment",
                RetentionYears = 3,
                IsDefault = true,
                EffectiveDate = DateTime.UtcNow,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow,
                Description = "Assessment materials retention policy. Test materials and results " +
                            "retained for 3 years for academic integrity and accreditation purposes."
            },

            // Enrollment data retention (10 years)
            new RetentionPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "Enrollment",
                RetentionYears = 10,
                IsDefault = true,
                EffectiveDate = DateTime.UtcNow,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow,
                Description = "Student enrollment and academic progress records. Extended retention " +
                            "for transcript and credential verification purposes."
            },

            // Audit record retention (10 years minimum for compliance)
            new RetentionPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "AuditRecord",
                RetentionYears = 10,
                IsDefault = true,
                EffectiveDate = DateTime.UtcNow,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow,
                Description = "Audit trail retention for compliance and security investigations. " +
                            "Extended retention period to support legal and regulatory requirements."
            },

            // Guardian relationship retention (5 years after student graduation)
            new RetentionPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "Guardian",
                RetentionYears = 5,
                IsDefault = true,
                EffectiveDate = DateTime.UtcNow,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow,
                Description = "Guardian and parent contact information retention policy. " +
                            "Retained for 5 years after student graduation or withdrawal."
            },

            // School and class data retention (indefinite with archival)
            new RetentionPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "School",
                RetentionYears = 50, // Long-term institutional memory
                IsDefault = true,
                EffectiveDate = DateTime.UtcNow,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow,
                Description = "School organizational data retention policy. Long-term retention " +
                            "for institutional history and administrative continuity."
            },

            // District tenant data (administrative)
            new RetentionPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "DistrictTenant",
                RetentionYears = 25,
                IsDefault = true,
                EffectiveDate = DateTime.UtcNow,
                TenantId = "platform",
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow,
                Description = "District administrative and configuration data retention policy. " +
                            "Extended retention for regulatory and historical purposes."
            }
        };

        await context.RetentionPolicies.AddRangeAsync(retentionPolicies);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds district-specific retention policy overrides if needed
    /// </summary>
    public static async Task SeedDistrictSpecificPoliciesAsync(LmsDbContext context, string tenantId, string districtName)
    {
        // Check if district already has custom retention policies
        var existingPolicies = await context.RetentionPolicies
            .Where(rp => rp.TenantId == tenantId)
            .ToListAsync();

        if (existingPolicies.Any())
        {
            return; // District already has custom policies
        }

        // For districts requiring special retention periods (e.g., research districts)
        var specialRetentionPolicies = new List<RetentionPolicy>();

        // Example: Research district might need longer student data retention
        if (districtName.Contains("Research") || districtName.Contains("University"))
        {
            specialRetentionPolicies.Add(new RetentionPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "Student",
                RetentionYears = 15, // Extended for research purposes
                IsDefault = false,
                EffectiveDate = DateTime.UtcNow,
                TenantId = tenantId,
                CreatedBy = "system",
                CreatedDate = DateTime.UtcNow,
                Description = $"Extended student data retention policy for {districtName}. " +
                            "Research institutions require longer retention for longitudinal studies."
            });
        }

        if (specialRetentionPolicies.Any())
        {
            await context.RetentionPolicies.AddRangeAsync(specialRetentionPolicies);
            await context.SaveChangesAsync();
        }
    }
}