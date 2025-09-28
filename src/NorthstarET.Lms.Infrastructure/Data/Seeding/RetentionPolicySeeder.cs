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
            new RetentionPolicy(
                entityType: "Student",
                retentionYears: 7,
                isDefault: true,
                effectiveDate: DateTime.UtcNow),

            // Staff data retention (5 years)
            new RetentionPolicy(
                entityType: "Staff", 
                retentionYears: 5,
                isDefault: true,
                effectiveDate: DateTime.UtcNow),

            // Assessment data retention (3 years)
            new RetentionPolicy(
                entityType: "Assessment",
                retentionYears: 3, 
                isDefault: true,
                effectiveDate: DateTime.UtcNow),

            // Audit data retention (10 years for compliance)
            new RetentionPolicy(
                entityType: "AuditRecord",
                retentionYears: 10,
                isDefault: true,
                effectiveDate: DateTime.UtcNow)
        };

        await context.RetentionPolicies.AddRangeAsync(retentionPolicies);
        await context.SaveChangesAsync();
    }
}