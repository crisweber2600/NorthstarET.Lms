# BDD Feature Files

## Purpose
Contains Reqnroll feature files that define business requirements as executable specifications following BDD-First Testing principles.

## Architecture Context
Implements Constitution Principle I (BDD-First Testing):
- Every functional requirement expressed as Gherkin scenarios
- Feature files written before any code implementation
- Covers complete user journeys and acceptance criteria
- Maps directly to business value and user stories

## Directory Structure
```
Features/
├── Districts/          # District tenant management scenarios
├── Students/           # Student lifecycle and management scenarios
├── RBAC/              # Role-based access control scenarios
├── Compliance/        # Audit and retention policy scenarios
├── Assessments/       # Assessment workflow scenarios (future)
└── Calendar/          # Academic calendar scenarios (future)
```

## File Inventory

### District Management (6 scenarios)
- `Districts/CreateDistrict.feature` - District tenant creation with validation
- `Districts/ManageDistrictQuotas.feature` - User capacity management
- `Districts/SuspendDistrict.feature` - District lifecycle operations

### Student Management (6 scenarios)  
- `Students/CreateStudent.feature` - Student registration and validation
- `Students/UpdateStudentGradeLevel.feature` - Grade progression workflows
- `Students/WithdrawStudent.feature` - Student withdrawal with audit trail

### Role-Based Access Control (4 scenarios)
- `RBAC/AssignRoleToUser.feature` - User role assignment with scope validation
- `RBAC/RevokeUserRole.feature` - Role revocation with audit requirements

### Compliance Management (2 scenarios)
- `Compliance/CreateAuditRecord.feature` - Tamper-evident audit trail creation
- `Compliance/ValidateAuditChain.feature` - Audit chain integrity verification

## Usage Examples

### Running BDD Tests
```bash
# Run all feature tests
dotnet test --filter Category=BDD

# Run specific feature
dotnet test --filter "DisplayName~CreateDistrict"

# Run features for specific domain
dotnet test --filter "DisplayName~District"
```

### Feature File Structure Example
```gherkin
Feature: Create District
    As a PlatformAdmin
    I want to create district tenant accounts
    So that school districts can access the LMS system

Background:
    Given I am authenticated as a PlatformAdmin

Scenario: Create district with valid information
    When I create a district with slug "oakland-unified" and name "Oakland Unified"
    Then the district should be created successfully
    And the district should have default quotas
    And the creation should be audited

Scenario: Reject duplicate district slug
    Given a district with slug "existing-district" already exists
    When I try to create a district with slug "existing-district"
    Then the operation should fail with error "District slug already exists"
    And no new district should be created
```

### Step Definition Integration
Each scenario maps to step definitions in `../StepDefinitions/`:
- `Given` steps set up test context and preconditions
- `When` steps execute the action being tested  
- `Then` steps verify outcomes and side effects

## Business Domain Coverage

### District Management
- **Tenant Creation**: Multi-tenant district setup with slug validation
- **Quota Management**: User capacity limits and enforcement  
- **Status Management**: District activation, suspension, and lifecycle

### Student Management  
- **Registration**: Student onboarding with data validation
- **Grade Progression**: K-12 grade level advancement workflows
- **Lifecycle Management**: Enrollment, transfer, withdrawal, graduation

### RBAC (Role-Based Access Control)
- **Role Assignment**: Hierarchical permission assignment with scope
- **Permission Validation**: Context-aware authorization checks
- **Role Lifecycle**: Assignment, modification, and revocation workflows

### Compliance & Audit
- **Audit Trail**: Tamper-evident audit record creation
- **Chain Validation**: Cryptographic integrity verification
- **Retention Policies**: FERPA-compliant data lifecycle management

## Current Status
- ✅ **18 Feature Files**: Complete BDD scenarios for core domains
- ✅ **46 Scenarios**: Comprehensive business requirement coverage
- ✅ **Step Definitions**: All scenarios have automated step implementations
- ✅ **Background Setup**: Proper test context and authentication

## Recent Changes
- 2025-01-09: Added comprehensive audit and compliance scenarios
- 2025-01-09: Enhanced RBAC scenarios with hierarchical permission testing
- 2025-01-09: Added student lifecycle scenarios with grade progression
- 2024-12-19: Initial BDD framework setup with district management scenarios

## Traceability

### Feature → Specification Mapping
- District features map to `../../specs/001-foundational-lms-with/contracts/district-management.md`
- Student features map to `../../specs/001-foundational-lms-with/contracts/student-management.md`
- RBAC features map to `../../specs/001-foundational-lms-with/contracts/rbac-management.md`
- Compliance features map to `../../specs/001-foundational-lms-with/contracts/compliance-management.md`

### Feature → Implementation Mapping  
- District scenarios test `../../src/NorthstarET.Lms.Application/Services/DistrictService.cs`
- Student scenarios test `../../src/NorthstarET.Lms.Application/Services/StudentService.cs`
- RBAC scenarios test `../../src/NorthstarET.Lms.Application/Services/RoleAuthorizationService.cs`
- Compliance scenarios test `../../src/NorthstarET.Lms.Application/Services/AuditService.cs`

## Related Documentation
- See `../StepDefinitions/` for step definition implementations
- See `../../specs/001-foundational-lms-with/` for detailed business requirements
- See `../../src/` for implementation code tested by these features