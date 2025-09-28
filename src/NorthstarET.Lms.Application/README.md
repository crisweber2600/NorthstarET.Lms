# Application Layer

## Purpose
Contains application services, DTOs, and interfaces that orchestrate domain operations and define contracts for external dependencies. Implements use cases while maintaining clean architecture boundaries.

## Architecture Context
Second layer of Clean Architecture (Constitution Principle III). The Application layer:
- Depends only on the Domain layer
- Defines interfaces for infrastructure dependencies (Repository pattern)
- Contains application services that coordinate domain operations
- Implements CQRS patterns with Commands and Queries
- Uses Result pattern for error handling without exceptions

## Directory Structure
```
NorthstarET.Lms.Application/
├── Services/           # Application services implementing use cases
├── Interfaces/         # Contracts for infrastructure dependencies
├── DTOs/              # Data transfer objects for external communication
├── Commands/          # Command objects for write operations (CQRS)
├── Queries/           # Query objects for read operations (CQRS) 
└── Common/            # Shared utilities and result patterns
```

## File Inventory

### Application Services (Use Case Implementation)
- `DistrictService.cs` - District management with quota validation and audit integration
- `StudentService.cs` - Student lifecycle operations with grade progression
- `EnrollmentService.cs` - Class enrollment workflows with bulk operations
- `RoleAuthorizationService.cs` - RBAC permission validation and role assignment
- `AuditService.cs` - Tamper-evident audit trail management

### Repository Interfaces (Infrastructure Contracts)
- `IDistrictRepository.cs` - District data access contract
- `IStudentRepository.cs` - Student data operations with search capabilities
- `IEnrollmentRepository.cs` - Enrollment data access with class rosters
- `IRoleDefinitionRepository.cs` - RBAC role definitions
- `IRoleAssignmentRepository.cs` - User role assignments
- `IAuditRepository.cs` - Tenant-scoped audit records
- `IPlatformAuditRepository.cs` - Cross-tenant audit operations
- `IUnitOfWork.cs` - Transaction coordination
- `ITenantContextAccessor.cs` - Multi-tenant context management

### Data Transfer Objects
- `CreateDistrictDto.cs` - District creation request
- `DistrictDto.cs` - District response with quotas
- `CreateStudentDto.cs` - Student registration request
- `StudentDto.cs` - Student information response
- `CreateEnrollmentDto.cs` - Enrollment creation request
- `EnrollmentDto.cs` - Enrollment details response
- `AssignRoleDto.cs` - Role assignment request
- `RoleAssignmentDto.cs` - Role assignment details
- `CreateAuditRecordDto.cs` - Audit event creation
- `AuditQueryDto.cs` - Audit search parameters

### Commands (Write Operations - CQRS)
- `CreateDistrictCommand.cs` - District creation with validation
- `UpdateDistrictQuotasCommand.cs` - Quota modification
- `CreateStudentCommand.cs` - Student registration
- `WithdrawStudentCommand.cs` - Student withdrawal workflow
- `CreateEnrollmentCommand.cs` - Class enrollment
- `TransferEnrollmentCommand.cs` - Student class transfers
- `AssignRoleCommand.cs` - Role assignment operations
- `RevokeRoleCommand.cs` - Role revocation with audit

### Queries (Read Operations - CQRS)
- `GetDistrictByIdQuery.cs` - District lookup
- `SearchStudentsQuery.cs` - Student search with filters
- `GetClassRosterQuery.cs` - Class enrollment lists
- `GetUserPermissionsQuery.cs` - RBAC permission resolution
- `QueryAuditRecordsQuery.cs` - Audit trail queries

### Common Utilities
- `Result.cs` - Result pattern for error handling without exceptions
- `PagedResult.cs` - Paginated query results

## Usage Examples

### Application Service Usage
```csharp
// District creation with validation
var createDto = new CreateDistrictDto 
{
    Slug = "oakland-unified",
    DisplayName = "Oakland Unified School District",
    Quotas = new DistrictQuotasDto { MaxStudents = 50000, MaxStaff = 5000, MaxAdmins = 100 }
};

var result = await districtService.CreateDistrictAsync(createDto, "admin-user");
if (result.IsSuccess) 
{
    var district = result.Value;
    // Use district data
}
else
{
    // Handle validation error from result.Error
}
```

### Repository Pattern Usage
```csharp
// Repository interface defines contract
public class StudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<Result<StudentDto>> CreateStudentAsync(CreateStudentDto dto, string createdBy)
    {
        // Check business rules
        var existing = await _studentRepository.GetByStudentNumberAsync(dto.StudentNumber);
        if (existing != null)
            return Result.Failure<StudentDto>("Student number already exists");
            
        // Create domain entity
        var student = new Student(dto.StudentNumber, dto.FirstName, dto.LastName, dto.DateOfBirth, dto.EnrollmentDate);
        
        // Persist through repository
        await _studentRepository.AddAsync(student);
        await _unitOfWork.SaveChangesAsync();
        
        return Result.Success(MapToDto(student));
    }
}
```

### CQRS Pattern Usage
```csharp
// Commands for write operations
public class CreateStudentCommand
{
    public CreateStudentDto Student { get; }
    public string CreatedBy { get; }
    
    public CreateStudentCommand(CreateStudentDto student, string createdBy)
    {
        Student = student;
        CreatedBy = createdBy;
    }
}

// Queries for read operations  
public class SearchStudentsQuery
{
    public StudentSearchDto SearchCriteria { get; }
    
    public SearchStudentsQuery(StudentSearchDto searchCriteria)
    {
        SearchCriteria = searchCriteria;
    }
}
```

## Design Principles

### Clean Architecture Compliance
- No dependencies on Infrastructure or Presentation layers
- All external dependencies accessed through interfaces
- Domain entities orchestrated but not modified directly
- Business logic remains in Domain layer

### CQRS Implementation
- Commands handle write operations with validation
- Queries handle read operations with projections
- Clear separation of read/write concerns
- Commands return Result objects, Queries return data

### Result Pattern
- No exceptions for business logic failures
- Success/failure explicitly handled by calling code
- Error messages provide clear feedback
- Null reference exceptions eliminated

### Repository Pattern
- Abstractions defined in Application layer
- Implementations provided by Infrastructure layer
- Unit of Work coordinates transactions
- Repository methods focus on business operations, not CRUD

## Current Implementation Status
- ✅ **5 Application Services**: Complete use case implementations
- ✅ **12 Repository Interfaces**: Full data access contracts  
- ✅ **20+ DTOs**: Request/response objects for all domains
- ✅ **15+ Commands**: Write operation coordination
- ✅ **10+ Queries**: Read operation definitions
- ✅ **Result Pattern**: Error handling without exceptions

## Recent Changes
- 2025-01-09: Complete application services implementing TDD GREEN phase
- 2025-01-09: Added comprehensive DTO layer with validation
- 2025-01-09: Implemented CQRS patterns with Commands and Queries
- 2025-01-09: Added Result pattern for clean error handling

## Testing
Application services have comprehensive unit tests in `tests/NorthstarET.Lms.Application.Tests/Services/`. All tests follow TDD principles with tests written before implementation.

## Related Documentation
- See `../Domain/` for domain entities orchestrated by these services
- See `../Infrastructure/` for repository implementations
- See `../../tests/NorthstarET.Lms.Application.Tests/` for service testing examples