using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantSlug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.StudentId)
            .IsRequired();

        builder.Property(e => e.ClassId)
            .IsRequired();

        builder.Property(e => e.SchoolYearId)
            .IsRequired();

        builder.Property(e => e.EnrollmentStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EntryDate)
            .IsRequired();

        builder.Property(e => e.ExitDate);

        builder.Property(e => e.ExitReason)
            .HasMaxLength(500);

        builder.HasIndex(e => new { e.TenantSlug, e.StudentId, e.ClassId });

        // Audit fields
        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedAt);
    }
}
