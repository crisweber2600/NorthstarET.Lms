using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Infrastructure.Data.Configurations;

public class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("audit_records");
        
        builder.HasKey(ar => ar.Id);
        
        builder.Property(ar => ar.EventType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(ar => ar.EntityType)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(ar => ar.EntityId)
            .IsRequired(false);
            
        builder.Property(ar => ar.UserId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(ar => ar.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(ar => ar.IpAddress)
            .HasMaxLength(45); // IPv6 support
            
        builder.Property(ar => ar.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(ar => ar.ChangeDetails)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(ar => ar.CorrelationId)
            .HasMaxLength(100);
            
        builder.Property(ar => ar.PreviousRecordHash)
            .HasMaxLength(64); // SHA-256 hash length
            
        builder.Property(ar => ar.RecordHash)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.Property(ar => ar.SequenceNumber)
            .IsRequired()
            .ValueGeneratedOnAdd(); // Auto-increment

        // Unique constraint on sequence number within tenant
        builder.HasIndex(ar => ar.SequenceNumber)
            .IsUnique()
            .HasDatabaseName("uk_audit_records_sequence");

        // Indexes for audit queries
        builder.HasIndex(ar => ar.Timestamp)
            .HasDatabaseName("ix_audit_records_timestamp");
            
        builder.HasIndex(ar => new { ar.EntityType, ar.EntityId })
            .HasDatabaseName("ix_audit_records_entity");
            
        builder.HasIndex(ar => ar.UserId)
            .HasDatabaseName("ix_audit_records_user");
            
        builder.HasIndex(ar => ar.EventType)
            .HasDatabaseName("ix_audit_records_event_type");
            
        builder.HasIndex(ar => ar.CorrelationId)
            .HasDatabaseName("ix_audit_records_correlation");

        // Configure inheritance from TenantScopedEntity
        builder.Property<string>("TenantId")
            .HasColumnName("tenant_id")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex("TenantId")
            .HasDatabaseName("ix_audit_records_tenant_id");
    }
}

public class PlatformAuditRecordConfiguration : IEntityTypeConfiguration<PlatformAuditRecord>
{
    public void Configure(EntityTypeBuilder<PlatformAuditRecord> builder)
    {
        builder.ToTable("platform_audit_records", "platform");
        
        builder.HasKey(par => par.Id);
        
        builder.Property(par => par.EventType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(par => par.TenantId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(par => par.ActingUserId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(par => par.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(par => par.Details)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(par => par.RecordHash)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.Property(par => par.SequenceNumber)
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Unique constraint on sequence number (platform-wide)
        builder.HasIndex(par => par.SequenceNumber)
            .IsUnique()
            .HasDatabaseName("uk_platform_audit_records_sequence");

        // Indexes for platform audit queries
        builder.HasIndex(par => par.Timestamp)
            .HasDatabaseName("ix_platform_audit_records_timestamp");
            
        builder.HasIndex(par => par.TenantId)
            .HasDatabaseName("ix_platform_audit_records_tenant");
            
        builder.HasIndex(par => par.ActingUserId)
            .HasDatabaseName("ix_platform_audit_records_user");
            
        builder.HasIndex(par => par.EventType)
            .HasDatabaseName("ix_platform_audit_records_event_type");
    }
}

public class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("retention_policies");
        
        builder.HasKey(rp => rp.Id);
        
        builder.Property(rp => rp.EntityType)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(rp => rp.RetentionYears)
            .IsRequired();
            
        builder.Property(rp => rp.IsDefault)
            .HasDefaultValue(false);
            
        builder.Property(rp => rp.EffectiveDate)
            .IsRequired()
            .HasColumnType("date");
            
        builder.Property(rp => rp.SupersededDate)
            .HasColumnType("date");

        // Index for entity type lookups
        builder.HasIndex(rp => new { rp.EntityType, rp.IsDefault })
            .HasDatabaseName("ix_retention_policies_entity_default");

        // Configure inheritance from TenantScopedEntity
        builder.Property<string>("TenantId")
            .HasColumnName("tenant_id")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex("TenantId")
            .HasDatabaseName("ix_retention_policies_tenant_id");
    }
}

public class LegalHoldConfiguration : IEntityTypeConfiguration<LegalHold>
{
    public void Configure(EntityTypeBuilder<LegalHold> builder)
    {
        builder.ToTable("legal_holds");
        
        builder.HasKey(lh => lh.Id);
        
        builder.Property(lh => lh.EntityType)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(lh => lh.EntityId)
            .IsRequired();
            
        builder.Property(lh => lh.Reason)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(lh => lh.HoldDate)
            .IsRequired()
            .HasColumnType("date");
            
        builder.Property(lh => lh.ReleaseDate)
            .HasColumnType("date");
            
        builder.Property(lh => lh.IsActive)
            .HasDefaultValue(true);
            
        builder.Property(lh => lh.AuthorizingUser)
            .IsRequired()
            .HasMaxLength(100);

        // Index for entity lookups
        builder.HasIndex(lh => new { lh.EntityType, lh.EntityId, lh.IsActive })
            .HasDatabaseName("ix_legal_holds_entity_active");

        // Configure inheritance from TenantScopedEntity
        builder.Property<string>("TenantId")
            .HasColumnName("tenant_id")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex("TenantId")
            .HasDatabaseName("ix_legal_holds_tenant_id");
    }
}