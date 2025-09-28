using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data.Configurations;
using NorthstarET.Lms.Infrastructure.Security;
using System.Reflection;

namespace NorthstarET.Lms.Infrastructure.Data;

public class LmsDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public LmsDbContext(
        DbContextOptions<LmsDbContext> options,
        ITenantContextAccessor tenantContextAccessor) : base(options)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    // Core entities
    public DbSet<DistrictTenant> Districts { get; set; } = null!;
    public DbSet<SchoolYear> SchoolYears { get; set; } = null!;
    public DbSet<AcademicCalendar> AcademicCalendars { get; set; } = null!;
    public DbSet<School> Schools { get; set; } = null!;
    public DbSet<Class> Classes { get; set; } = null!;
    public DbSet<Staff> Staff { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Guardian> Guardians { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;

    // RBAC entities
    public DbSet<RoleDefinition> RoleDefinitions { get; set; } = null!;
    public DbSet<RoleAssignment> RoleAssignments { get; set; } = null!;

    // Compliance entities
    public DbSet<AuditRecord> AuditRecords { get; set; } = null!;
    public DbSet<PlatformAuditRecord> PlatformAuditRecords { get; set; } = null!;
    public DbSet<RetentionPolicy> RetentionPolicies { get; set; } = null!;
    public DbSet<LegalHold> LegalHolds { get; set; } = null!;

    // Assessment entities
    public DbSet<AssessmentDefinition> AssessmentDefinitions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure tenant-specific schema if available
        var tenant = _tenantContextAccessor.GetTenant();
        if (tenant != null)
        {
            modelBuilder.HasDefaultSchema(tenant.SchemaName);
        }

        // Global query filters for tenant isolation
        ApplyTenantFilters(modelBuilder);
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        // Apply tenant filters to all TenantScopedEntity types
        var tenantScopedEntityTypes = modelBuilder.Model
            .GetEntityTypes()
            .Where(e => typeof(TenantScopedEntity).IsAssignableFrom(e.ClrType));

        foreach (var entityType in tenantScopedEntityTypes)
        {
            var method = typeof(LmsDbContext)
                .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);
            method.Invoke(this, new object[] { modelBuilder });
        }
    }

    private void ApplyTenantFilter<T>(ModelBuilder modelBuilder) where T : class, ITenantScoped
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => 
            _tenantContextAccessor.GetTenant() == null || 
            e.TenantId == _tenantContextAccessor.GetTenant()!.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set tenant ID on new entities
        var tenant = _tenantContextAccessor.GetTenant();
        if (tenant != null)
        {
            var tenantScopedEntities = ChangeTracker.Entries()
                .Where(e => e.Entity is ITenantScoped && e.State == EntityState.Added)
                .Select(e => e.Entity as ITenantScoped);

            foreach (var entity in tenantScopedEntities)
            {
                if (entity != null && string.IsNullOrEmpty(entity.TenantId))
                {
                    entity.TenantId = tenant.TenantId;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    // Explicit interface implementation for IUnitOfWork
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
    }
}