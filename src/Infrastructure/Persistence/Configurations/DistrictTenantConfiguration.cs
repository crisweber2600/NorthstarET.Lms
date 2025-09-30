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

        // Use OwnsOne for complex type Slug
        builder.OwnsOne(d => d.Slug, slug =>
        {
            slug.Property(s => s.Value)
                .HasColumnName("TenantSlug")
                .IsRequired()
                .HasMaxLength(100);
        });

        builder.Property(d => d.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>();

        // Use OwnsOne for complex type Quotas
        builder.OwnsOne(d => d.Quotas, quotas =>
        {
            quotas.Property(q => q.Students)
                .HasColumnName("MaxStudents")
                .IsRequired();

            quotas.Property(q => q.Staff)
                .HasColumnName("MaxStaff")
                .IsRequired();

            quotas.Property(q => q.Admins)
                .HasColumnName("MaxAdmins")
                .IsRequired();
        });

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
