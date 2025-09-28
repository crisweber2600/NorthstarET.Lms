# Domain Layer

## Purpose
Contains pure business entities, value objects, and domain services with zero external dependencies, implementing the core business logic of the LMS system.

## Architecture Context
This is the innermost layer of Clean Architecture (Constitution Principle III). The Domain layer:
- Contains business entities with behavior and invariants
- Defines domain events for state changes  
- Implements domain services for cross-aggregate operations
- Has NO dependencies on external frameworks or infrastructure

## Directory Structure
```
NorthstarET.Lms.Domain/
├── Entities/           # Aggregate roots and domain entities
├── ValueObjects/       # Immutable value objects
├── Enums/             # Domain-specific enumerations
├── Events/            # Domain events for state changes
├── Services/          # Domain services for business logic
└── Common/            # Base classes and abstractions
```

## File Inventory

### Entities (Aggregate Roots)
- `DistrictTenant.cs` - Multi-tenant district management with quotas and policies
- `Student.cs` - Student lifecycle with grade progression and withdrawals
- `Staff.cs` - Staff management with role assignments and permissions
- `Enrollment.cs` - Class enrollment with transfer and completion workflows
- `Class.cs` - Academic class management with capacity and scheduling
- `SchoolYear.cs` - Academic year periods with date validation
- `RoleDefinition.cs` - RBAC role definitions with hierarchical permissions
- `RoleAssignment.cs` - User role assignments with scope and expiration
- `AuditRecord.cs` - Tamper-evident audit trails with hash chaining
- `PlatformAuditRecord.cs` - Cross-tenant audit records
- `RetentionPolicy.cs` - FERPA-compliant data retention policies
- `LegalHold.cs` - Legal preservation requirements

### Value Objects
- `DistrictQuotas.cs` - User capacity limits and quotas
- `UserName.cs` - Validated user name components
- `AcademicPeriod.cs` - Start and end date ranges with validation

### Enumerations
- `GradeLevel.cs` - K-12 grade level progression
- `UserLifecycleStatus.cs` - User account status transitions
- `EnrollmentStatus.cs` - Enrollment state management
- `RoleScope.cs` - RBAC permission scopes (District/School/Class)
- `DistrictStatus.cs` - District tenant status management

### Domain Events
- `StudentEnrolledEvent.cs` - Student enrollment notifications
- `StudentGradeUpdatedEvent.cs` - Grade level progression events
- `RoleAssignedEvent.cs` - RBAC assignment notifications
- `RetentionPolicyCreatedEvent.cs` - Compliance policy events
- `LegalHoldReleasedEvent.cs` - Legal preservation events

### Domain Services
- `IAuditChainService.cs` - Tamper-evident audit chain validation
- Business logic that spans multiple aggregates

## Usage Examples

### Creating Domain Entities
```csharp
// Create a new student with grade validation
var student = new Student(
    "STU-2024-001",
    "John",  
    "Smith",
    new DateTime(2010, 9, 1),
    DateTime.UtcNow.Date
);

// Update grade level with domain event
student.UpdateGradeLevel(GradeLevel.Grade6, "system");
```

### Working with Value Objects
```csharp
// Create validated district quotas
var quotas = new DistrictQuotas(
    maxStudents: 50000,
    maxStaff: 5000, 
    maxAdmins: 100
);

// Immutable - returns new instance on change
var updatedQuotas = quotas.WithMaxStudents(60000);
```

### Domain Events
```csharp
// Entities automatically raise domain events
var enrollment = new Enrollment(studentId, classId, schoolYearId, gradeLevel, DateTime.UtcNow);
// Raises StudentEnrolledEvent automatically

// Events can be processed by application layer
var events = enrollment.GetDomainEvents();
```

## Design Principles

### Entity Design
- Rich domain models with behavior, not anemic data containers
- Invariants enforced through constructors and methods
- Domain events for state change notifications
- Aggregate boundaries respect business transactions

### Value Object Design  
- Immutable with structural equality
- Validation in constructor with meaningful exceptions
- Factory methods for complex creation scenarios
- No identity - compared by value

### Domain Service Design
- Stateless operations that don't fit in a single aggregate
- Interface in Domain layer, implementation in Infrastructure
- Used when business logic spans multiple aggregates

## Recent Changes
- 2025-01-09: Added PlatformAuditRecord for cross-tenant operations
- 2025-01-09: Enhanced Student entity with middle name and accommodation tags
- 2025-01-09: Complete domain event framework for all state changes
- 2024-12-19: Initial domain model with multi-tenant entities and RBAC

## Testing
All domain entities and value objects have comprehensive unit tests in `tests/NorthstarET.Lms.Domain.Tests/`. Domain logic is tested in isolation without external dependencies.

## Related Documentation
- See `../Application/` for use cases that orchestrate domain operations
- See `../../tests/NorthstarET.Lms.Domain.Tests/` for domain testing examples
- See `../../specs/001-foundational-lms-with/data-model.md` for complete domain model documentation