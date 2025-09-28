# Domain Entities

## Purpose
Contains all domain entities representing the core business concepts in the Learning Management System. These are the building blocks of the domain model with rich behavior and business rules.

## Architecture Context
Part of the Domain layer in Clean Architecture:
- **Dependencies**: None (pure domain objects)
- **Dependents**: Application layer, Infrastructure layer
- **Responsibilities**: Business logic, domain rules, entity relationships

## File Inventory
```
Entities/
├── TenantScopedEntity.cs        # Base class for multi-tenant entities
├── DistrictTenant.cs            # District as top-level tenant
├── SchoolYear.cs                # Academic year temporal scoping
├── School.cs                    # School within a district
├── Class.cs                     # Class within a school and year
├── Student.cs                   # Student entity with enrollments
├── Staff.cs                     # Staff with role assignments
├── Guardian.cs                  # Student guardians/parents
├── User.cs                      # System user identity
├── RoleDefinition.cs            # RBAC role definitions
├── RoleAssignment.cs            # Role-to-user assignments
├── Enrollment.cs                # Student-to-class relationships
├── AuditLog.cs                  # Immutable audit trail
└── README.md                    # This file
```

## Usage Examples

### Creating Domain Entities
```csharp
// Student with rich domain behavior
public class Student : TenantScopedEntity
{
    public Guid UserId { get; private set; }
    public string StudentNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public GradeLevel CurrentGradeLevel { get; private set; }
    
    // Rich domain behavior
    public void UpdateGradeLevel(GradeLevel newGrade, string updatedBy)
    {
        var oldGrade = CurrentGradeLevel;
        CurrentGradeLevel = newGrade;
        
        // Domain event for audit trail
        AddDomainEvent(new StudentGradeUpdatedEvent(
            UserId, oldGrade, newGrade, updatedBy));
    }
}
```

### Multi-Tenant Scoping
```csharp
// All entities inherit tenant scoping
public abstract class TenantScopedEntity
{
    // Tenant context handled by infrastructure
    // Never expose TenantId in domain model
    public DateTime CreatedAt { get; protected set; }
    public DateTime? LastModifiedAt { get; protected set; }
    
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
```

## Key Principles

### Business Rule Enforcement
- All business rules implemented in entities
- Invalid state transitions prevented through private setters
- Domain events raised for state changes

### Multi-Tenant Isolation
- All entities implicitly tenant-scoped
- No direct tenant ID exposure in domain
- Infrastructure handles tenant filtering

### Rich Domain Model
- Entities contain business behavior, not just data
- Domain services for cross-entity operations
- Value objects for complex attributes

## Entity Relationships

### Hierarchical Structure
```
DistrictTenant (1) -> (∞) School
School (1) -> (∞) Class
Class (1) -> (∞) Enrollment -> (1) Student
Staff (1) -> (∞) RoleAssignment -> (1) School/Class
User (1) -> (1) Student/Staff
```

### Temporal Scoping
- SchoolYear provides academic year context
- Class, Enrollment scoped to specific school year
- Historical data preserved across years

## Domain Events

### Audit Events
```csharp
public record StudentCreatedEvent(
    Guid UserId,
    string StudentNumber,
    string CreatedBy) : IDomainEvent;

public record StudentGradeUpdatedEvent(
    Guid UserId,
    GradeLevel OldGrade,
    GradeLevel NewGrade,
    string UpdatedBy) : IDomainEvent;
```

## Validation Rules

### Student Entity
- StudentNumber must be unique within tenant
- Grade level transitions must be valid
- Enrollment dates cannot overlap for same student

### School Entity  
- School names unique within district
- School codes follow district naming conventions

### Class Entity
- Class capacity limits enforced
- Teacher assignments validated through RBAC

## Testing Strategy
- Entity unit tests focus on business rule validation
- Domain event testing for state change verification
- Invalid state transition testing

## Recent Changes
- 2025-01-09: Added comprehensive domain entity model
- 2025-01-09: Implemented multi-tenant entity base class
- 2025-01-09: Added domain events for audit trail

## Related Documentation
- See `../ValueObjects/README.md` for value object implementations
- See `../Events/README.md` for domain event definitions
- See `../../Application/README.md` for entity usage in application services
- See `../../../tests/NorthstarET.Lms.Domain.Tests/Entities/README.md` for entity testing