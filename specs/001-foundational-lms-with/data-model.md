# Data Model: Foundational LMS with Tenant Isolation

**Feature**: 001-foundational-lms-with  
**Date**: December 19, 2024  
**Status**: Phase 1 - Data Design

## Multi-Tenant Architecture

### Tenant Isolation Strategy
```csharp
// Schema-per-tenant with dynamic connection strings
public class TenantContext
{
    public string TenantId { get; set; }      // District slug (e.g., "oakland-unified")
    public string SchemaName { get; set; }   // Database schema (e.g., "oakland_unified")
    public string ConnectionString { get; set; }
}

// All entities include implicit tenant scoping through EF Core configurations
public abstract class TenantScopedEntity
{
    public string TenantId { get; protected set; }
    // Tenant ID is never exposed in API - handled by middleware
}
```

## Core Domain Entities

### District & Tenant Management
```csharp
public class DistrictTenant : TenantScopedEntity
{
    public string Slug { get; set; }                    // Unique identifier (e.g., "oakland-unified")
    public string DisplayName { get; set; }             // Human-readable name
    public DistrictStatus Status { get; set; }          // Active, Suspended, Deleted
    public DistrictQuotas Quotas { get; set; }          // User count limits
    public DateTime CreatedDate { get; set; }
    public string CreatedByUserId { get; set; }         // PlatformAdmin who created
    
    // Navigation properties
    public ICollection<School> Schools { get; set; }
    public ICollection<SchoolYear> SchoolYears { get; set; }
    public RetentionPolicy RetentionPolicy { get; set; }
}

public record DistrictQuotas
{
    public int MaxStudents { get; init; }               // Default: 50,000
    public int MaxStaff { get; init; }                  // Default: 5,000  
    public int MaxAdmins { get; init; }                 // Default: 100
}

public enum DistrictStatus { Active, Suspended, PendingDeletion, Deleted }
```

### Academic Calendar & Temporal Scoping
```csharp
public class SchoolYear : TenantScopedEntity
{
    public int Year { get; set; }                       // e.g., 2024 for 2024-2025 school year
    public string Name { get; set; }                    // e.g., "2024-2025"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SchoolYearStatus Status { get; set; }        // Planning, Active, Completed, Archived
    public bool IsArchived { get; set; }                // Immutable when true
    
    // Navigation properties
    public AcademicCalendar Calendar { get; set; }
    public ICollection<Class> Classes { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; }
    public ICollection<RoleAssignment> RoleAssignments { get; set; }
}

public class AcademicCalendar : TenantScopedEntity
{
    public Guid SchoolYearId { get; set; }
    public SchoolYear SchoolYear { get; set; }
    
    public ICollection<Term> Terms { get; set; }
    public ICollection<Closure> Closures { get; set; }
}

public class Term : TenantScopedEntity
{
    public string Name { get; set; }                    // e.g., "Fall Semester", "Q1"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int SequenceNumber { get; set; }             // 1, 2, 3, 4 for ordering
}

public class Closure : TenantScopedEntity
{
    public string Name { get; set; }                    // e.g., "Winter Break", "Professional Development"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsRecurring { get; set; }
}
```

### School & Class Organization
```csharp
public class School : TenantScopedEntity
{
    public string Code { get; set; }                    // District-unique identifier
    public string Name { get; set; }
    public SchoolType Type { get; set; }                // Elementary, Middle, High, K12
    public bool IsActive { get; set; }
    public DateTime EstablishedDate { get; set; }
    
    // Navigation properties
    public ICollection<Class> Classes { get; set; }
    public ICollection<RoleAssignment> StaffAssignments { get; set; }
}

public class Class : TenantScopedEntity
{
    public string Code { get; set; }                    // School-unique identifier (e.g., "MATH-101-A")
    public string Name { get; set; }                    // e.g., "Algebra I - Period A"
    public string Subject { get; set; }                 // e.g., "Mathematics"
    public GradeLevel GradeLevel { get; set; }
    
    // Foreign keys
    public Guid SchoolId { get; set; }
    public Guid SchoolYearId { get; set; }
    
    // Navigation properties
    public School School { get; set; }
    public SchoolYear SchoolYear { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; }
    public ICollection<RoleAssignment> StaffAssignments { get; set; }
}

public enum SchoolType { Elementary, Middle, High, K12, Alternative, Charter }
public enum GradeLevel { PreK = -1, K = 0, Grade1 = 1, Grade2 = 2, /* ... */, Grade12 = 12 }
```

### User Management & Identity
```csharp
public class Staff : TenantScopedEntity
{
    public Guid UserId { get; set; }                    // Internal user ID
    public string EmployeeNumber { get; set; }          // District-specific identifier
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public UserLifecycleStatus Status { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    
    // Navigation properties
    public IdentityMapping IdentityMapping { get; set; }
    public ICollection<RoleAssignment> RoleAssignments { get; set; }
}

public class Student : TenantScopedEntity
{
    public Guid UserId { get; set; }                    // Internal user ID (globally unique)
    public string StudentNumber { get; set; }           // District-specific identifier
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public UserLifecycleStatus Status { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public DateTime? WithdrawalDate { get; set; }
    
    // Program participation flags
    public bool IsSpecialEducation { get; set; }
    public bool IsGifted { get; set; }
    public bool IsEnglishLanguageLearner { get; set; }
    public string[] AccommodationTags { get; set; }      // JSON array of accommodation codes
    
    // Navigation properties
    public IdentityMapping IdentityMapping { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; }
    public ICollection<GuardianStudentRelationship> GuardianRelationships { get; set; }
}

public class Guardian : TenantScopedEntity
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public UserLifecycleStatus Status { get; set; }
    
    // Navigation properties
    public IdentityMapping IdentityMapping { get; set; }
    public ICollection<GuardianStudentRelationship> StudentRelationships { get; set; }
}

public class GuardianStudentRelationship : TenantScopedEntity
{
    public Guid GuardianId { get; set; }
    public Guid StudentId { get; set; }
    public RelationshipType RelationshipType { get; set; } // Parent, Guardian, Emergency
    public bool IsPrimary { get; set; }
    public bool CanPickup { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    // Navigation properties
    public Guardian Guardian { get; set; }
    public Student Student { get; set; }
}

public enum UserLifecycleStatus { Active, Suspended, Transferred, Graduated, Withdrawn }
public enum RelationshipType { Parent, Guardian, StepParent, Grandparent, EmergencyContact }
```

### Enrollment & Academic Progression
```csharp
public class Enrollment : TenantScopedEntity
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid SchoolYearId { get; set; }
    
    public GradeLevel GradeLevel { get; set; }           // Grade level for this enrollment
    public EnrollmentStatus Status { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public DateTime? WithdrawalDate { get; set; }
    public string WithdrawalReason { get; set; }
    
    // Navigation properties
    public Student Student { get; set; }
    public Class Class { get; set; }
    public SchoolYear SchoolYear { get; set; }
}

public enum EnrollmentStatus { Active, Transferred, Graduated, Withdrawn, Inactive }
```

### RBAC & Authorization
```csharp
public class RoleDefinition : TenantScopedEntity
{
    public string Name { get; set; }                    // e.g., "Teacher", "Principal", "Counselor"
    public string Description { get; set; }
    public RoleScope Scope { get; set; }               // Platform, District, School, Class
    public string[] Permissions { get; set; }           // JSON array of permission codes
    public bool IsSystemRole { get; set; }             // Cannot be deleted
    public bool AllowsDelegation { get; set; }
    
    // Navigation properties
    public ICollection<RoleAssignment> Assignments { get; set; }
}

public class RoleAssignment : TenantScopedEntity
{
    public Guid UserId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    
    // Scope context (at least one must be non-null)
    public Guid? SchoolId { get; set; }                 // Role scoped to specific school
    public Guid? ClassId { get; set; }                  // Role scoped to specific class
    public Guid? SchoolYearId { get; set; }             // Role scoped to specific year
    
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive { get; set; }
    
    // Delegation support
    public Guid? DelegatedByUserId { get; set; }        // Who delegated this role
    public DateTime? DelegationExpiry { get; set; }     // Auto-revoke delegation
    
    // Navigation properties
    public RoleDefinition RoleDefinition { get; set; }
    public School School { get; set; }
    public Class Class { get; set; }
    public SchoolYear SchoolYear { get; set; }
}

public enum RoleScope { Platform, District, School, Class }
```

### Assessment Management
```csharp
public class AssessmentDefinition : TenantScopedEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public byte[] PdfContent { get; set; }              // Stored as binary data
    public string PdfFileName { get; set; }
    public long PdfSizeBytes { get; set; }
    public string PdfContentType { get; set; }
    
    // Versioning
    public int Version { get; set; }                    // Incremental version number
    public Guid? ParentAssessmentId { get; set; }       // Link to previous version
    public bool IsCurrentVersion { get; set; }
    
    // Optional school year pinning
    public Guid? PinnedToSchoolYearId { get; set; }
    public SchoolYear PinnedToSchoolYear { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public string CreatedByUserId { get; set; }
    public bool IsImmutable { get; set; }               // Cannot be modified once set
}
```

### Identity & External Integration
```csharp
public class IdentityMapping : TenantScopedEntity
{
    public Guid UserId { get; set; }                    // Internal user ID
    public string ExternalId { get; set; }              // Entra External ID
    public string Issuer { get; set; }                  // Identity provider identifier
    public DateTime MappedDate { get; set; }
    public DateTime? UnmappedDate { get; set; }
    public bool IsActive { get; set; }
    
    // Prevent duplicate external IDs
    // Unique constraint on (ExternalId, Issuer, TenantId, IsActive=true)
}
```

### Compliance & Audit
```csharp
public class RetentionPolicy : TenantScopedEntity
{
    public string EntityType { get; set; }              // "Student", "Staff", "Assessment"
    public int RetentionYears { get; set; }             // FERPA defaults: Student=7, Staff=5, Assessment=3
    public bool IsDefault { get; set; }                 // District default policy
    public DateTime EffectiveDate { get; set; }
    public DateTime? SupersededDate { get; set; }
}

public class LegalHold : TenantScopedEntity
{
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }                  // ID of held entity
    public string Reason { get; set; }                  // Legal case reference
    public DateTime HoldDate { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool IsActive { get; set; }
    public string AuthorizingUser { get; set; }
}

public class AuditRecord : TenantScopedEntity
{
    public Guid Id { get; set; }                        // Primary key
    public string EventType { get; set; }               // "Create", "Update", "Delete", "Access"
    public string EntityType { get; set; }              // "Student", "Staff", "Class", etc.
    public Guid? EntityId { get; set; }                 // ID of affected entity
    public string UserId { get; set; }                  // Acting user
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    
    // Change tracking
    public string ChangeDetails { get; set; }           // JSON of before/after values
    public string CorrelationId { get; set; }           // For bulk operations
    
    // Tamper-evident chaining
    public string PreviousRecordHash { get; set; }      // SHA-256 of previous record
    public string RecordHash { get; set; }              // SHA-256 of this record
    public long SequenceNumber { get; set; }            // Monotonic sequence per tenant
}

public class PlatformAuditRecord
{
    // Similar to AuditRecord but for cross-tenant platform operations
    // No TenantId - stored in separate schema/database
    public Guid Id { get; set; }
    public string EventType { get; set; }               // "DistrictCreated", "DistrictSuspended"
    public string TenantId { get; set; }                // Which tenant was affected
    public string ActingUserId { get; set; }            // PlatformAdmin who acted
    public DateTime Timestamp { get; set; }
    public string Details { get; set; }                 // JSON event details
    public string RecordHash { get; set; }
    public long SequenceNumber { get; set; }
}
```

## Database Schema Design

### Multi-Tenant Schema Strategy
```sql
-- Each district gets its own schema
CREATE SCHEMA oakland_unified;
CREATE SCHEMA berkeley_unified;
CREATE SCHEMA san_francisco_unified;

-- Platform-level data in shared schema
CREATE SCHEMA platform;

-- Example tenant-scoped table
CREATE TABLE oakland_unified.students (
    user_id UUID PRIMARY KEY,
    student_number VARCHAR(50) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    -- ... other fields
    -- No tenant_id column needed - schema provides isolation
);
```

### Key Constraints & Indexes
```sql
-- Unique constraints respecting tenant boundaries
ALTER TABLE oakland_unified.students 
ADD CONSTRAINT uk_students_number UNIQUE (student_number);

-- Performance indexes for common queries
CREATE INDEX ix_enrollments_student_year ON oakland_unified.enrollments (student_id, school_year_id);
CREATE INDEX ix_audit_timestamp ON oakland_unified.audit_records (timestamp DESC);
CREATE INDEX ix_audit_entity ON oakland_unified.audit_records (entity_type, entity_id);

-- Audit chain integrity
CREATE INDEX ix_audit_sequence ON oakland_unified.audit_records (sequence_number);
```

## Entity Framework Configuration

### Multi-Tenant Context Factory
```csharp
public class TenantDbContextFactory : IDbContextFactory<LmsDbContext>
{
    private readonly ITenantContextAccessor _tenantAccessor;
    
    public LmsDbContext CreateDbContext()
    {
        var tenant = _tenantAccessor.GetTenant();
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseSqlServer(tenant.ConnectionString, opts => 
            {
                opts.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
            })
            .EnableSensitiveDataLogging(false)
            .EnableDetailedErrors(false)
            .Options;
            
        var context = new LmsDbContext(options);
        context.SetTenantContext(tenant);
        return context;
    }
}
```

### Entity Configurations
```csharp
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        
        builder.HasKey(s => s.UserId);
        
        builder.Property(s => s.StudentNumber)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.HasIndex(s => s.StudentNumber)
            .IsUnique()
            .HasDatabaseName("uk_students_number");
            
        builder.Property(s => s.AccommodationTags)
            .HasConversion(
                tags => JsonSerializer.Serialize(tags, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<string[]>(json, JsonSerializerOptions.Default)
            )
            .HasColumnType("nvarchar(max)");
            
        // Relationships
        builder.HasOne(s => s.IdentityMapping)
            .WithOne()
            .HasForeignKey<IdentityMapping>(im => im.UserId);
            
        builder.HasMany(s => s.Enrollments)
            .WithOne(e => e.Student)
            .HasForeignKey(e => e.StudentId);
    }
}
```

## Migration Strategy

### Initial Schema Creation
```bash
# Create migration for tenant schema
dotnet ef migrations add InitialTenantSchema --context LmsDbContext

# Apply migration to specific tenant
dotnet ef database update --context LmsDbContext --connection "Server=...;Database=lms;Schema=oakland_unified;..."
```

### Tenant Provisioning Process
1. Create new database schema for district
2. Run all migrations against new schema  
3. Insert default retention policies
4. Create default admin role assignments
5. Initialize audit chain with genesis record

## Performance Considerations

### Read Optimization
- Separate read/write connection strings for tenant contexts
- Cached role assignments with expiration
- Indexed audit queries for compliance reporting
- Materialized views for complex enrollment reports

### Write Optimization  
- Bulk insert optimization for enrollment data
- Asynchronous audit record processing
- Connection pooling per tenant schema
- Deferred foreign key validation for bulk operations

**Phase 1 Data Model Complete** - Ready for API contract design and quickstart guide.