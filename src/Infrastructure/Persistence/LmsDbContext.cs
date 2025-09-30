using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Shared;

namespace NorthstarET.Lms.Infrastructure.Persistence;

public class LmsDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public LmsDbContext(DbContextOptions<LmsDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // District and School Management
    public DbSet<DistrictTenant> Districts => Set<DistrictTenant>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();
    public DbSet<AcademicCalendar> AcademicCalendars => Set<AcademicCalendar>();
    public DbSet<Class> Classes => Set<Class>();

    // People
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    // Identity and Authorization
    public DbSet<IdentityMapping> IdentityMappings => Set<IdentityMapping>();
    public DbSet<RoleDefinition> RoleDefinitions => Set<RoleDefinition>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    // Assessments
    public DbSet<AssessmentDefinition> AssessmentDefinitions => Set<AssessmentDefinition>();

    // Compliance
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<LegalHold> LegalHolds => Set<LegalHold>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    // Bulk Operations
    public DbSet<BulkJob> BulkJobs => Set<BulkJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LmsDbContext).Assembly);

        // Set default schema based on tenant context
        if (!string.IsNullOrEmpty(_tenantContext.TenantSlug))
        {
            modelBuilder.HasDefaultSchema(_tenantContext.TenantSlug);
        }

        // Global query filters for tenant isolation
        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // Apply tenant filter to all tenant-scoped entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(TenantScopedEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(TenantScopedEntity.TenantSlug));
                var tenantSlug = System.Linq.Expressions.Expression.Constant(_tenantContext.TenantSlug);
                var equals = System.Linq.Expressions.Expression.Equal(property, tenantSlug);
                var lambda = System.Linq.Expressions.Expression.Lambda(equals, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Note: Tenant scoping is done in entity constructors via InitializeTenant()
        // which is called by repositories or services during entity creation
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}
