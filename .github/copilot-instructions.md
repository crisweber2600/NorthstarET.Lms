# GitHub Copilot Instructions: Foundational LMS Development

## Project Context
This is a foundational Learning Management System (LMS) for K-12 education with strict multi-tenant isolation, comprehensive RBAC, and FERPA compliance requirements. The system supports school districts as tenants with complex hierarchical relationships between districts, schools, classes, staff, and students.

## Architecture Principles

### Clean Architecture (NON-NEGOTIABLE)
- **Domain Layer**: Pure business entities with zero external dependencies
- **Application Layer**: Use cases and abstractions, depends only on Domain
- **Infrastructure Layer**: EF Core, external APIs, depends on Application
- **Presentation Layer**: ASP.NET Core API controllers, depends on Infrastructure

When writing code, ALWAYS respect dependency direction (inward only).

### Multi-Tenant Data Isolation
- Each school district gets its own database schema (e.g., `oakland_unified`)  
- All entities are tenant-scoped but never expose tenant_id in APIs
- Use `TenantScopedEntity` base class for all domain entities
- EF Core configurations handle schema isolation automatically

```csharp
// CORRECT - Domain entity with implicit tenant scoping
public class Student : TenantScopedEntity
{
    public Guid UserId { get; set; }
    public string StudentNumber { get; set; }
    // No explicit TenantId property
}

// INCORRECT - Never expose tenant concerns in domain
public class Student
{
    public string TenantId { get; set; } // ❌ WRONG
}
```

### BDD-First Development (NON-NEGOTIABLE)
Always write Reqnroll feature files BEFORE implementing any functionality:

```gherkin
Feature: Create Student
    As a DistrictAdmin
    I want to create student records
    So that I can manage student enrollment

Scenario: Create student with valid data
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    When I create a student with student number "STU-2024-001"
    Then the student should be created successfully
    And the creation should be audited
```

Step definitions must be implemented FIRST and shown to FAIL before any production code.

## Domain Model Guidance

### Key Entities and Relationships
- **DistrictTenant**: Top-level tenant, contains all other entities
- **SchoolYear**: Provides temporal scoping for academic data
- **School**: Belongs to district, contains classes
- **Class**: Scoped to school + school year, has enrollments
- **Student**: Global identity, enrollments scoped per district/year
- **Staff**: Flexible roles, can be assigned to multiple schools/classes
- **Guardian**: Associated with students, no system access in MVP

### RBAC Implementation
Use hierarchical roles with predicate-based permissions:
- **Platform Level**: PlatformAdmin (global access)
- **District Level**: DistrictAdmin (district-wide access)
- **School Level**: SchoolUser (school-scoped access)  
- **Class Level**: Staff (class/role-scoped access)

```csharp
// CORRECT - Hierarchical role assignment
public class RoleAssignment : TenantScopedEntity
{
    public Guid UserId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    
    // Scope context - at least one must be non-null
    public Guid? SchoolId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? SchoolYearId { get; set; }
}
```

### Audit and Compliance
ALL mutations must generate audit records:
- Immutable audit logs with tamper-evident chaining
- SHA-256 hash linking for integrity verification
- Tenant-scoped audit tables (per district schema)
- Platform-level audit for cross-tenant operations

```csharp
// CORRECT - Domain events for audit trail
public class Student : TenantScopedEntity
{
    public void UpdateGradeLevel(GradeLevel newGrade, string updatedBy)
    {
        var oldGrade = GradeLevel;
        GradeLevel = newGrade;
        
        AddDomainEvent(new StudentGradeUpdatedEvent(
            UserId, oldGrade, newGrade, updatedBy));
    }
}
```

## Code Generation Guidelines

### Entity Framework Configurations
Always use explicit configurations instead of attributes:

```csharp
// CORRECT - Explicit configuration
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
            .IsUnique();
    }
}

// INCORRECT - Don't use data annotations on domain entities
[Table("students")] // ❌ WRONG - domain contamination
public class Student
{
    [Key] // ❌ WRONG
    public Guid UserId { get; set; }
}
```

### API Controllers
Follow consistent patterns for all controllers:

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Always require authentication
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    [HttpPost]
    [RequireRole("DistrictAdmin", "SchoolUser")]
    public async Task<ActionResult<StudentDto>> CreateStudent(
        [FromBody] CreateStudentRequest request)
    {
        var command = new CreateStudentCommand(request);
        var result = await _studentService.CreateStudentAsync(command);
        
        return result.IsSuccess 
            ? Created($"/api/v1/students/{result.Value.UserId}", result.Value)
            : BadRequest(result.Error);
    }
}
```

### Error Handling
Use Result pattern for error handling, never throw exceptions for business logic:

```csharp
// CORRECT - Result pattern
public async Task<Result<Student>> CreateStudentAsync(CreateStudentCommand command)
{
    if (await _repository.StudentNumberExistsAsync(command.StudentNumber))
        return Result.Failure<Student>("Student number already exists");
        
    var student = new Student(command.StudentNumber, command.FirstName, command.LastName);
    await _repository.AddAsync(student);
    
    return Result.Success(student);
}

// INCORRECT - Don't throw for business logic
public async Task<Student> CreateStudentAsync(CreateStudentCommand command)
{
    if (await _repository.StudentNumberExistsAsync(command.StudentNumber))
        throw new BusinessRuleException("Student number exists"); // ❌ WRONG
}
```

## Performance and Scale Considerations

### Database Queries
- Always use async/await for database operations
- Implement proper pagination (default 20, max 100 items)
- Use projection for list queries to minimize data transfer
- Add appropriate indexes for tenant + query patterns

```csharp
// CORRECT - Efficient paginated query with projection
public async Task<PagedResult<StudentSummaryDto>> GetStudentsAsync(
    GetStudentsQuery query)
{
    var studentsQuery = _context.Students
        .Where(s => s.Status == StudentStatus.Active)
        .OrderBy(s => s.LastName)
        .ThenBy(s => s.FirstName);
        
    var students = await studentsQuery
        .Skip((query.Page - 1) * query.Size)
        .Take(query.Size)
        .Select(s => new StudentSummaryDto
        {
            UserId = s.UserId,
            StudentNumber = s.StudentNumber,
            FullName = $"{s.FirstName} {s.LastName}",
            CurrentGradeLevel = s.CurrentGradeLevel
        })
        .ToListAsync();
        
    var totalCount = await studentsQuery.CountAsync();
    
    return new PagedResult<StudentSummaryDto>(students, query.Page, query.Size, totalCount);
}
```

### Bulk Operations  
For large data operations (>1000 records):
- Use background jobs with progress tracking
- Implement user-selectable error handling strategies
- Provide preview/dry-run capabilities
- Return correlation IDs for async operation tracking

## Testing Patterns

### Unit Tests (Domain and Application)
```csharp
public class StudentTests
{
    [Fact]
    public void CreateStudent_WithValidData_ShouldSucceed()
    {
        // Arrange
        var studentNumber = "STU-2024-001";
        var firstName = "John";
        var lastName = "Smith";

        // Act
        var student = new Student(studentNumber, firstName, lastName);

        // Assert
        student.StudentNumber.Should().Be(studentNumber);
        student.FullName.Should().Be("John Smith");
        student.Status.Should().Be(StudentStatus.Active);
    }
}
```

### Integration Tests (Infrastructure)
```csharp
public class StudentRepositoryTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task AddStudent_ShouldPersistToDatabase()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repository = new StudentRepository(context);
        var student = new Student("STU-2024-001", "John", "Smith");

        // Act
        await repository.AddAsync(student);
        await context.SaveChangesAsync();

        // Assert
        var savedStudent = await repository.GetByIdAsync(student.UserId);
        savedStudent.Should().NotBeNull();
        savedStudent.StudentNumber.Should().Be("STU-2024-001");
    }
}
```

## Common Patterns to Follow

### Dependency Injection Registration
```csharp
// In Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddDbContextFactory<LmsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        
        return services;
    }
}
```

### Configuration Patterns
Use strongly-typed configuration:
```csharp
public class DistrictQuotaOptions
{
    public int DefaultMaxStudents { get; set; } = 50000;
    public int DefaultMaxStaff { get; set; } = 5000;
    public int DefaultMaxAdmins { get; set; } = 100;
}

// Register in Program.cs
builder.Services.Configure<DistrictQuotaOptions>(
    builder.Configuration.GetSection("DistrictQuotas"));
```

## Security Considerations
- NEVER log sensitive PII data (student info, grades)
- Always validate tenant context in middleware
- Use scoped URLs for file access (assessments)
- Implement rate limiting per tenant
- Audit all RBAC changes and suspicious activities

When implementing any feature, ALWAYS consider:
1. What BDD scenario covers this?
2. How does this maintain tenant isolation?
3. What audit trail is needed?
4. How does RBAC apply?
5. What are the performance implications?

Remember: Educational data is highly sensitive and regulated. When in doubt, err on the side of privacy and security.