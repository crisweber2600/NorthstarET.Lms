# Quickstart Guide: Foundational LMS Development

**Feature**: 001-foundational-lms-with  
**Target**: Development team setup and first implementation steps  
**Prerequisites**: .NET 9 SDK, SQL Server, Visual Studio or VS Code

## Development Environment Setup

### 1. Clone and Initial Setup
```bash
# Clone the repository
git clone https://github.com/crisweber2600/NorthstarET.Lms.git
cd NorthstarET.Lms

# Switch to feature branch  
git checkout 001-foundational-lms-with

# Create the solution and projects
dotnet new sln -n NorthstarET.Lms
```

### 2. Create Project Structure
```bash
# Create the Clean Architecture projects
mkdir src tests

# Domain layer (no external dependencies)
dotnet new classlib -n NorthstarET.Lms.Domain -o src/NorthstarET.Lms.Domain
dotnet sln add src/NorthstarET.Lms.Domain

# Application layer
dotnet new classlib -n NorthstarET.Lms.Application -o src/NorthstarET.Lms.Application
dotnet sln add src/NorthstarET.Lms.Application
dotnet add src/NorthstarET.Lms.Application reference src/NorthstarET.Lms.Domain

# Infrastructure layer  
dotnet new classlib -n NorthstarET.Lms.Infrastructure -o src/NorthstarET.Lms.Infrastructure
dotnet sln add src/NorthstarET.Lms.Infrastructure
dotnet add src/NorthstarET.Lms.Infrastructure reference src/NorthstarET.Lms.Application

# API layer
dotnet new webapi -n NorthstarET.Lms.Api -o src/NorthstarET.Lms.Api
dotnet sln add src/NorthstarET.Lms.Api
dotnet add src/NorthstarET.Lms.Api reference src/NorthstarET.Lms.Infrastructure

# Aspire orchestration
dotnet new aspire -n NorthstarET.Lms.AppHost -o src/NorthstarET.Lms.AppHost
dotnet sln add src/NorthstarET.Lms.AppHost
dotnet add src/NorthstarET.Lms.AppHost reference src/NorthstarET.Lms.Api
```

### 3. Add Required NuGet Packages

**Domain Layer** (zero external dependencies):
```bash
# Domain layer stays pure - no external packages
```

**Application Layer**:
```bash
cd src/NorthstarET.Lms.Application
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add package FluentValidation
dotnet add package MediatR
```

**Infrastructure Layer**:
```bash
cd ../NorthstarET.Lms.Infrastructure  
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Graph
dotnet add package Azure.Storage.Blobs
```

**API Layer**:
```bash
cd ../NorthstarET.Lms.Api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.AspNetCore.Authorization
dotnet add package Swashbuckle.AspNetCore
dotnet add package Serilog.AspNetCore
dotnet add package Microsoft.EntityFrameworkCore.Design
```

**Testing**:
```bash
# Create test projects
dotnet new xunit -n NorthstarET.Lms.Domain.Tests -o tests/NorthstarET.Lms.Domain.Tests
dotnet new xunit -n NorthstarET.Lms.Application.Tests -o tests/NorthstarET.Lms.Application.Tests
dotnet new xunit -n NorthstarET.Lms.Infrastructure.Tests -o tests/NorthstarET.Lms.Infrastructure.Tests
dotnet new xunit -n NorthstarET.Lms.Api.Tests -o tests/NorthstarET.Lms.Api.Tests

# Add test projects to solution
dotnet sln add tests/NorthstarET.Lms.Domain.Tests
dotnet sln add tests/NorthstarET.Lms.Application.Tests
dotnet sln add tests/NorthstarET.Lms.Infrastructure.Tests 
dotnet sln add tests/NorthstarET.Lms.Api.Tests

# Add test dependencies
cd tests
find . -name "*.Tests.csproj" -exec dirname {} \; | while read dir; do
  cd "$dir"
  dotnet add package FluentAssertions
  dotnet add package Reqnroll
  dotnet add package Reqnroll.xUnit
  dotnet add package Testcontainers
  dotnet add package Microsoft.AspNetCore.Mvc.Testing
  cd - > /dev/null
done
```

### 4. Configure Aspire Orchestration
Update `src/NorthstarET.Lms.AppHost/Program.cs`:
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server for multi-tenant data
var sqlserver = builder.AddSqlServer("sqlserver")
    .WithDataVolume("lms-data")
    .AddDatabase("lmsdb");

// Add Redis for caching (optional)
var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data");

// Add the main API with dependencies
var api = builder.AddProject<Projects.NorthstarET_Lms_Api>("api")
    .WithReference(sqlserver)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

builder.Build().Run();
```

## BDD-First Development Workflow

### 5. Create First Feature File
Create `tests/Features/Districts/CreateDistrict.feature`:
```gherkin
Feature: Create District
    As a PlatformAdmin
    I want to create new school districts
    So that I can provision tenant environments for educational organizations

Background:
    Given I am authenticated as a PlatformAdmin

Scenario: Create district with valid data
    When I create a district with the following details:
      | Field        | Value                          |
      | Slug         | oakland-unified                |
      | DisplayName  | Oakland Unified School District|
      | MaxStudents  | 50000                         |
      | MaxStaff     | 5000                          |
      | MaxAdmins    | 100                           |
    Then the district should be created successfully
    And I should automatically have DistrictAdmin rights for the district
    And the creation should be logged in the platform audit

Scenario: Reject duplicate district slug
    Given a district with slug "existing-district" already exists
    When I create a district with slug "existing-district"
    Then the creation should be rejected with error "District slug already exists"

Scenario: Enforce quota limits
    When I create a district with MaxStudents of 150000
    Then the creation should be rejected with error "MaxStudents cannot exceed 100000"
```

### 6. Create Step Definitions
Create `tests/StepDefinitions/DistrictSteps.cs`:
```csharp
[Binding]
public class DistrictSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly TestWebApplicationFactory _factory;
    
    public DistrictSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _factory = new TestWebApplicationFactory();
    }

    [Given(@"I am authenticated as a PlatformAdmin")]
    public void GivenIAmAuthenticatedAsAPlatformAdmin()
    {
        // Setup test authentication context
        throw new PendingStepException(); // Will fail initially (RED)
    }

    [When(@"I create a district with the following details:")]
    public void WhenICreateADistrictWithTheFollowingDetails(Table table)
    {
        // Send POST request to /api/v1/districts
        throw new PendingStepException(); // Will fail initially (RED)
    }

    [Then(@"the district should be created successfully")]
    public void ThenTheDistrictShouldBeCreatedSuccessfully()
    {
        // Verify 201 Created response
        throw new PendingStepException(); // Will fail initially (RED)
    }
}
```

### 7. Run Tests (RED Phase)
```bash
# Run the BDD tests - they should fail
dotnet test tests/NorthstarET.Lms.Api.Tests --logger "console;verbosity=detailed"

# Expected: All steps marked as pending, tests fail
# This confirms we're in the RED phase of TDD
```

## Domain-Driven Design Implementation

### 8. Create Domain Entities (TDD RED → GREEN)
Create `src/NorthstarET.Lms.Domain/Entities/DistrictTenant.cs`:
```csharp
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Domain.Entities;

public class DistrictTenant
{
    // Private constructor for EF Core
    private DistrictTenant() { }

    public DistrictTenant(
        string slug,
        string displayName,
        DistrictQuotas quotas,
        string createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("District slug is required", nameof(slug));
            
        if (!IsValidSlug(slug))
            throw new ArgumentException("Invalid slug format", nameof(slug));
            
        Id = Guid.NewGuid();
        Slug = slug.ToLowerInvariant();
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Quotas = quotas ?? throw new ArgumentNullException(nameof(quotas));
        Status = DistrictStatus.Active;
        CreatedDate = DateTime.UtcNow;
        CreatedByUserId = createdByUserId;
        
        // Domain event for audit trail
        AddDomainEvent(new DistrictProvisionedEvent(Id, slug, displayName, createdByUserId));
    }

    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DistrictStatus Status { get; private set; }
    public DistrictQuotas Quotas { get; private set; } = null!;
    public DateTime CreatedDate { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private static bool IsValidSlug(string slug)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            slug, @"^[a-z0-9-]+$");
    }

    public void Suspend(string reason, string suspendedByUserId)
    {
        if (Status == DistrictStatus.Suspended)
            throw new InvalidOperationException("District is already suspended");
            
        Status = DistrictStatus.Suspended;
        AddDomainEvent(new DistrictSuspendedEvent(Id, reason, suspendedByUserId));
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
```

### 9. Write Unit Tests (TDD)
Create `tests/NorthstarET.Lms.Domain.Tests/Entities/DistrictTenantTests.cs`:
```csharp
public class DistrictTenantTests
{
    [Fact]
    public void CreateDistrict_WithValidData_ShouldSucceed()
    {
        // Arrange
        var slug = "oakland-unified";
        var displayName = "Oakland Unified School District";
        var quotas = new DistrictQuotas { MaxStudents = 50000, MaxStaff = 5000, MaxAdmins = 100 };
        var createdBy = "platform-admin-123";

        // Act
        var district = new DistrictTenant(slug, displayName, quotas, createdBy);

        // Assert
        district.Slug.Should().Be(slug);
        district.DisplayName.Should().Be(displayName);
        district.Status.Should().Be(DistrictStatus.Active);
        district.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DistrictProvisionedEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("INVALID-UPPER")]
    [InlineData("invalid_underscore")]
    [InlineData("invalid spaces")]
    public void CreateDistrict_WithInvalidSlug_ShouldThrowArgumentException(string invalidSlug)
    {
        // Arrange
        var displayName = "Test District";
        var quotas = new DistrictQuotas { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 };

        // Act & Assert
        var act = () => new DistrictTenant(invalidSlug, displayName, quotas, "admin");
        act.Should().Throw<ArgumentException>();
    }
}
```

## Database Setup and Migrations

### 10. Configure Entity Framework
Create `src/NorthstarET.Lms.Infrastructure/Data/LmsDbContext.cs`:
```csharp
public class LmsDbContext : DbContext
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public LmsDbContext(DbContextOptions<LmsDbContext> options, ITenantContextAccessor tenantContextAccessor) 
        : base(options)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<DistrictTenant> Districts { get; set; } = null!;
    public DbSet<School> Schools { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    // ... other DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LmsDbContext).Assembly);
        
        // Configure tenant context if available
        var tenant = _tenantContextAccessor.GetTenant();
        if (tenant != null)
        {
            modelBuilder.HasDefaultSchema(tenant.SchemaName);
        }
    }
}
```

### 11. Create Initial Migration
```bash
# Set the API project as startup project for EF tools
cd src/NorthstarET.Lms.Infrastructure

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../NorthstarET.Lms.Api --context LmsDbContext

# Apply to development database
dotnet ef database update --startup-project ../NorthstarET.Lms.Api --context LmsDbContext
```

## Running the Application

### 12. Start Development Environment
```bash
# Start Aspire orchestration (from repository root)
cd src/NorthstarET.Lms.AppHost
dotnet run

# This starts:
# - SQL Server container
# - Redis container (if configured)
# - Main API application
# - Aspire dashboard at https://localhost:15000
```

### 13. Verify Setup
```bash
# Test API health endpoint
curl https://localhost:7000/health

# Test basic authentication (should return 401)
curl https://localhost:7000/api/v1/districts

# Run all tests
dotnet test --logger "console;verbosity=detailed"
```

## Next Steps

### Immediate Development Tasks
1. **Complete Domain Layer**: Implement all entities from data-model.md
2. **Application Services**: Create use cases for district management
3. **Repository Pattern**: Implement data access abstractions
4. **Authentication Setup**: Configure JWT Bearer authentication
5. **API Controllers**: Implement district management endpoints

### BDD Development Cycle
1. Write feature file for next requirement
2. Create step definitions (will fail - RED)
3. Implement domain logic to make tests pass (GREEN)
4. Refactor while maintaining tests (REFACTOR)
5. Repeat for each functional requirement

### Architecture Validation
- **Domain Independence**: Verify Domain layer has no external dependencies
- **Dependency Direction**: Ensure dependencies point inward only
- **Test Coverage**: Maintain >90% coverage in Domain and Application layers
- **Performance Targets**: Validate <200ms p95 for CRUD operations

### Compliance Preparation
- **Audit Infrastructure**: Implement tamper-evident audit logging
- **Multi-Tenant Isolation**: Validate strict tenant data separation
- **RBAC Implementation**: Test role-based access controls
- **Data Retention**: Implement retention policy enforcement

**Development Ready!** 🚀 The foundation is set for BDD-first, Clean Architecture development of the foundational LMS.