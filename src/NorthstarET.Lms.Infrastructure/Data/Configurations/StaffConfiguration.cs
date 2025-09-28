using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Infrastructure.Data.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("staff");
        
        builder.HasKey(s => s.UserId);
        
        builder.Property(s => s.EmployeeNumber)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(s => s.FirstName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(s => s.LastName)
            .IsRequired()
            .HasMaxLength(100);
            
        // Comment out MiddleName since Staff entity doesn't have this property
        // TODO: Add MiddleName to Staff domain entity if needed
        // builder.Property(s => s.MiddleName)
        //     .HasMaxLength(100);
            
        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(s => s.HireDate)
            .IsRequired()
            .HasColumnType("date");
            
        builder.Property(s => s.TerminationDate)
            .HasColumnType("date");

        // Unique constraints
        builder.HasIndex(s => s.EmployeeNumber)
            .IsUnique()
            .HasDatabaseName("uk_staff_employee_number");
            
        builder.HasIndex(s => s.Email)
            .IsUnique()
            .HasDatabaseName("uk_staff_email");

        // Indexes for common queries
        builder.HasIndex(s => s.Status)
            .HasDatabaseName("ix_staff_status");
            
        builder.HasIndex(s => new { s.LastName, s.FirstName })
            .HasDatabaseName("ix_staff_name");

        // Relationships
        builder.HasMany(s => s.RoleAssignments)
            .WithOne()
            .HasForeignKey(ra => ra.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure inheritance from TenantScopedEntity
        builder.Property<string>("TenantId")
            .HasColumnName("tenant_id")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex("TenantId")
            .HasDatabaseName("ix_staff_tenant_id");
    }
}