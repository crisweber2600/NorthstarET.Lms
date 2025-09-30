using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Infrastructure.Persistence.Configurations;

public class DistrictTenantConfiguration : IEntityTypeConfiguration<DistrictTenant>
{
    public void Configure(EntityTypeBuilder<DistrictTenant> builder)
    {
        builder.ToTable("Districts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantSlug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.DistrictName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(d => d.MaxStudents)
            .IsRequired();

        builder.Property(d => d.MaxStaff)
            .IsRequired();

        builder.Property(d => d.MaxAdmins)
            .IsRequired();

        builder.Property(d => d.StorageQuotaBytes)
            .IsRequired();

        builder.HasIndex(d => d.TenantSlug)
            .IsUnique();

        // Audit fields from base Entity class
        builder.Property(d => d.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(d => d.UpdatedAt);
    }
}
