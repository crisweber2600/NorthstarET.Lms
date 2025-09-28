using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Infrastructure.Data.Configurations;

public class DistrictTenantConfiguration : IEntityTypeConfiguration<DistrictTenant>
{
    public void Configure(EntityTypeBuilder<DistrictTenant> builder)
    {
        builder.ToTable("districts");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Slug)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(d => d.DisplayName)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(d => d.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(d => d.CreatedDate)
            .IsRequired();

        // Unique constraint on slug
        builder.HasIndex(d => d.Slug)
            .IsUnique()
            .HasDatabaseName("uk_districts_slug");

        // Configure the DistrictQuotas value object as owned entity
        builder.OwnsOne(d => d.Quotas, quotas =>
        {
            quotas.Property(q => q.MaxStudents)
                .HasColumnName("max_students")
                .HasDefaultValue(50000);
                
            quotas.Property(q => q.MaxStaff)
                .HasColumnName("max_staff")
                .HasDefaultValue(5000);
                
            quotas.Property(q => q.MaxAdmins)
                .HasColumnName("max_admins")
                .HasDefaultValue(100);
        });

        // Relationships
        builder.HasMany(d => d.Schools)
            .WithOne(s => s.District)
            .HasForeignKey(s => s.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(d => d.SchoolYears)
            .WithOne(sy => sy.District)
            .HasForeignKey(sy => sy.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(d => d.RetentionPolicy)
            .WithMany()
            .HasForeignKey("RetentionPolicyId")
            .OnDelete(DeleteBehavior.Restrict);

        // Configure inheritance from TenantScopedEntity
        builder.Property<string>("TenantId")
            .HasColumnName("tenant_id")
            .IsRequired()
            .HasMaxLength(100);

        // Index for tenant isolation
        builder.HasIndex("TenantId")
            .HasDatabaseName("ix_districts_tenant_id");
    }
}