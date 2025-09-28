# NorthstarET.Lms.Infrastructure

## Purpose
Infrastructure layer implementation providing data access, external service integrations, and infrastructure concerns. This layer depends on the Application layer and implements its abstractions.

## Architecture Context
This is the Infrastructure layer in our Clean Architecture:
- **Dependencies**: Application layer (interfaces and DTOs)
- **Dependents**: Presentation layer (API controllers)
- **Responsibilities**: Data persistence, external APIs, infrastructure services

## Directory Structure

### Core Infrastructure
- `Data/` - Entity Framework Core database context and configurations
- `Repositories/` - Repository pattern implementations for data access
- `ExternalServices/` - Third-party service integrations and HTTP clients
- `Security/` - Authentication, authorization, and security implementations
- `BackgroundServices/` - Hosted services and background job processing

## File Inventory
```
NorthstarET.Lms.Infrastructure/
├── Data/                         # EF Core database context and configurations
│   ├── Configurations/           # Entity type configurations
│   ├── LmsDbContext.cs          # Main database context
│   └── TenantScopedDbContext.cs # Multi-tenant database handling
├── Repositories/                 # Repository implementations
├── ExternalServices/            # Third-party service clients
├── Security/                    # Security and authentication
├── BackgroundServices/          # Background job processing
├── DependencyInjection.cs       # Service registration
└── README.md                    # This file
```

## Usage Examples

### Database Context Usage
```csharp
public class StudentRepository : IStudentRepository
{
    private readonly LmsDbContext _context;
    
    public StudentRepository(LmsDbContext context)
    {
        _context = context;
    }
    
    public async Task<Student> GetByIdAsync(Guid id)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == id);
    }
}
```

### Service Registration
```csharp
services.AddInfrastructure(configuration);
```

## Key Features

### Multi-Tenant Data Isolation
- Each school district gets its own database schema
- Tenant-scoped entities with automatic filtering
- Schema-based isolation for data security

### Entity Framework Configuration
- Explicit entity type configurations (no data annotations)
- Optimized queries with proper indexing
- Audit trail implementation with tamper-evident chaining

### Repository Pattern
- Generic repository base with common operations
- Specific repositories for complex domain operations
- Unit of work pattern for transaction management

## Dependencies
- **NorthstarET.Lms.Application** - Application services and interfaces
- **Microsoft.EntityFrameworkCore.SqlServer** - SQL Server provider
- **Microsoft.Extensions.Configuration** - Configuration abstractions
- **Microsoft.Extensions.DependencyInjection** - DI container

## Testing Strategy
- Integration tests with Testcontainers for database testing
- Repository tests with in-memory database
- External service tests with mock HTTP handlers

## Recent Changes
- 2025-01-09: Added Infrastructure layer with EF Core setup
- 2025-01-09: Implemented multi-tenant data isolation
- 2025-01-09: Created repository pattern with audit support

## Related Documentation
- See `Data/README.md` for Entity Framework configuration details
- See `Repositories/README.md` for repository implementation patterns
- See `../Application/README.md` for interface definitions
- See `../../tests/NorthstarET.Lms.Infrastructure.Tests/README.md` for testing approach