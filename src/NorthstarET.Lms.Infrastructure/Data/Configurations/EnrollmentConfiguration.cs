using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Infrastructure.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.StudentId)
            .IsRequired();
            
        builder.Property(e => e.ClassId)
            .IsRequired();
            
        builder.Property(e => e.SchoolYearId)
            .IsRequired();
            
        builder.Property(e => e.GradeLevel)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(e => e.EnrollmentDate)
            .IsRequired()
            .HasColumnType("date");
            
        builder.Property(e => e.WithdrawalDate)
            .HasColumnType("date");
            
        builder.Property(e => e.WithdrawalReason)
            .HasMaxLength(500);

        // Composite unique constraint to prevent duplicate enrollments
        builder.HasIndex(e => new { e.StudentId, e.ClassId, e.SchoolYearId })
            .IsUnique()
            .HasDatabaseName("uk_enrollments_student_class_year");

        // Indexes for common queries
        builder.HasIndex(e => new { e.StudentId, e.SchoolYearId })
            .HasDatabaseName("ix_enrollments_student_year");
            
        builder.HasIndex(e => new { e.ClassId, e.Status })
            .HasDatabaseName("ix_enrollments_class_status");
            
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_enrollments_status");

        // Relationships
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.Class)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.SchoolYear)
            .WithMany(sy => sy.Enrollments)
            .HasForeignKey(e => e.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure inheritance from TenantScopedEntity
        builder.Property<string>("TenantId")
            .HasColumnName("tenant_id")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex("TenantId")
            .HasDatabaseName("ix_enrollments_tenant_id");
    }
}