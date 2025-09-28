# Application Service Tests

## Purpose
Contains unit tests for application services following TDD Red-Green Cycle principles, ensuring comprehensive coverage of use case implementations.

## Architecture Context
Implements Constitution Principle II (TDD Red-Green Cycle):
- Tests written before service implementation (RED phase)
- Implementation makes tests pass (GREEN phase)
- Refactoring maintains test coverage
- Validates application layer business logic without external dependencies

## Directory Structure
```
Services/
├── DistrictServiceTests.cs        # District management use case tests
├── StudentServiceTests.cs         # Student lifecycle operation tests  
├── EnrollmentServiceTests.cs      # Enrollment workflow tests
├── RoleAuthorizationServiceTests.cs # RBAC permission and assignment tests
└── AuditServiceTests.cs           # Audit trail and compliance tests
```

## File Inventory

### Service Test Classes
- **DistrictServiceTests.cs** (6 test scenarios)
  - District creation with validation
  - Quota management and enforcement
  - District suspension workflows
  - Duplicate slug prevention
  - Audit trail integration
  - Error handling and validation

- **StudentServiceTests.cs** (6 test scenarios)  
  - Student registration with validation
  - Grade level progression workflows
  - Student withdrawal processing
  - Duplicate student number prevention
  - Search and filtering operations
  - Lifecycle status management

- **EnrollmentServiceTests.cs** (7 test scenarios)
  - Class enrollment with validation
  - Student transfer workflows
  - Graduation processing
  - Bulk rollover operations
  - Class roster generation
  - Enrollment status management
  - Capacity constraint validation

- **RoleAuthorizationServiceTests.cs** (8 test scenarios)
  - Role assignment with scope validation
  - Permission resolution and inheritance
  - Role revocation workflows
  - Hierarchical permission checking
  - Context-aware authorization
  - Role lifecycle management
  - Delegation and expiration
  - Audit integration for RBAC changes

- **AuditServiceTests.cs** (7 test scenarios)
  - Tamper-evident audit record creation
  - Audit chain hash validation
  - Multi-tenant audit isolation
  - Platform-level audit operations
  - Audit export and reporting
  - Chain integrity verification
  - Tampering detection algorithms

## Usage Examples

### Running Application Service Tests
```bash
# Run all application service tests
dotnet test tests/NorthstarET.Lms.Application.Tests/Services/

# Run specific service tests
dotnet test --filter "ClassName~DistrictServiceTests"

# Run with coverage reporting
dotnet test tests/NorthstarET.Lms.Application.Tests/Services/ --collect:"XPlat Code Coverage"
```

### Test Structure Example
```csharp
[Fact]
public async Task CreateDistrictAsync_WithValidData_ShouldSucceed()
{
    // Arrange
    var createDto = new CreateDistrictDto
    {
        Slug = "test-district",
        DisplayName = "Test District",
        Quotas = new DistrictQuotasDto { MaxStudents = 1000, MaxStaff = 100, MaxAdmins = 10 }
    };
    var createdBy = "admin-user";

    _mockDistrictRepository
        .Setup(r => r.GetBySlugAsync("test-district"))
        .ReturnsAsync((DistrictTenant?)null);

    // Act
    var result = await _districtService.CreateDistrictAsync(createDto, createdBy);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Slug.Should().Be("test-district");
    result.Value.DisplayName.Should().Be("Test District");
    
    _mockDistrictRepository.Verify(r => r.AddAsync(It.IsAny<DistrictTenant>()), Times.Once);
    _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    _mockAuditService.Verify(a => a.LogPlatformAuditEventAsync(It.IsAny<CreateAuditRecordDto>()), Times.Once);
}
```

## Test Strategy

### Unit Testing Approach
- **Isolation**: Each service tested in isolation using mock dependencies
- **Behavior Verification**: Focus on business logic behavior, not implementation details
- **Edge Cases**: Comprehensive testing of validation rules and error conditions
- **Mock Verification**: Ensure proper interaction with repository and audit services

### TDD Implementation Status
- ✅ **RED Phase Complete**: All 34 tests written first and failing appropriately
- 🔄 **GREEN Phase In Progress**: Service implementations making tests pass
- ⏳ **REFACTOR Phase**: Planned after all tests pass

### Coverage Requirements
- **Business Logic**: 100% coverage of service public methods
- **Error Handling**: All validation scenarios and edge cases covered
- **Integration Points**: Repository and audit service interactions verified
- **Domain Events**: Proper domain event handling validated

## Current Test Status
- ✅ **34 Unit Tests**: Comprehensive coverage of all application services
- ✅ **TDD Discipline**: Tests written before implementation (RED phase)
- 🔄 **Build Status**: Tests appropriately failing as services are implemented
- ✅ **Mock Setup**: All external dependencies properly mocked

## Testing Patterns

### Repository Mocking
```csharp
_mockStudentRepository
    .Setup(r => r.GetByStudentNumberAsync("STU-001"))
    .ReturnsAsync((Student?)null); // No existing student

_mockStudentRepository
    .Setup(r => r.AddAsync(It.IsAny<Student>()))
    .Returns(Task.CompletedTask);
```

### Result Pattern Testing
```csharp
// Success path
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();

// Failure path  
result.IsSuccess.Should().BeFalse();
result.Error.Should().Be("Expected error message");
```

### Domain Event Verification
```csharp
// Verify domain events are properly handled
var student = result.Value;
student.DomainEvents.Should().ContainSingle(e => e is StudentCreatedEvent);
```

## Recent Changes
- 2025-01-09: Added comprehensive audit service tests with chain validation
- 2025-01-09: Enhanced RBAC service tests with hierarchical permission scenarios
- 2025-01-09: Added enrollment service tests with bulk operation coverage
- 2025-01-09: Initial TDD RED phase implementation for all application services

## Related Documentation
- See `../../../src/NorthstarET.Lms.Application/Services/` for service implementations
- See `../../Domain.Tests/` for domain entity unit tests
- See `.specify/memory/constitution.md` for TDD requirements and principles