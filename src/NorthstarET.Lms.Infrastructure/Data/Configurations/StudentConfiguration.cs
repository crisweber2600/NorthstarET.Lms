using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        
        builder.HasKey(s => s.UserId);
        
        builder.Property(s => s.StudentNumber)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(s => s.FirstName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(s => s.LastName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(s => s.MiddleName)
            .HasMaxLength(100);
            
        builder.Property(s => s.DateOfBirth)
            .IsRequired()
            .HasColumnType("date");
            
        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(s => s.EnrollmentDate)
            .IsRequired()
            .HasColumnType("date");
            
        builder.Property(s => s.WithdrawalDate)
            .HasColumnType("date");
            
        builder.Property(s => s.IsSpecialEducation)
            .HasDefaultValue(false);
            
        builder.Property(s => s.IsGifted)
            .HasDefaultValue(false);
            
        builder.Property(s => s.IsEnglishLanguageLearner)
            .HasDefaultValue(false);

        // Configure AccommodationTags as JSON
        builder.Property(s => s.AccommodationTags)
            .HasConversion(
                tags => System.Text.Json.JsonSerializer.Serialize(tags, (System.Text.Json.JsonSerializerOptions?)null),
                json => System.Text.Json.JsonSerializer.Deserialize<string[]>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? Array.Empty<string>()
            )
            .HasColumnType("nvarchar(max)");

        // Unique constraint on student number within tenant
        builder.HasIndex(s => s.StudentNumber)
            .IsUnique()
            .HasDatabaseName("uk_students_number");

        // Indexes for common queries
        builder.HasIndex(s => s.Status)
            .HasDatabaseName("ix_students_status");
            
        builder.HasIndex(s => new { s.LastName, s.FirstName })
            .HasDatabaseName("ix_students_name");

        // Relationships
        builder.HasMany(s => s.Enrollments)
            .WithOne(e => e.Student)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(s => s.GuardianRelationships)
            .WithOne(gr => gr.Student)
            .HasForeignKey(gr => gr.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure inheritance from TenantScopedEntity
        builder.Property<string>("TenantId")
            .HasColumnName("tenant_id")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex("TenantId")
            .HasDatabaseName("ix_students_tenant_id");
    }
}