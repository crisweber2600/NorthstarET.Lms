# Tasks: Foundational LMS with Tenant Isolation and Compliance

**Input**: Design documents from `/specs/001-foundational-lms-with/`  
**Prerequisites**: plan.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

## Execution Summary

**Tech Stack**: .NET 9, ASP.NET Core, Entity Framework Core, .NET Aspire, Reqnroll, xUnit  
**Architecture**: Clean Architecture (4-layer) with multi-tenant isolation  
**Testing**: BDD-first with Reqnroll, TDD with >90% coverage requirement  
**Key Entities**: 12 domain entities supporting educational hierarchies and compliance

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- All file paths relative to repository root

## Phase 3.1: Project Setup & Structure

- [X] **T001** Create .NET solution and Clean Architecture project structure per quickstart.md
- [X] **T002** Install required NuGet packages (EF Core, Aspire, Reqnroll, xUnit, FluentAssertions)
- [X] **T003** [P] Configure EditorConfig and code analysis rules
- [X] **T004** [P] Set up Aspire app host project with SQL Server and Redis components
- [X] **T005** [P] Configure appsettings.json for multi-tenant connection strings

## Phase 3.2: BDD Features & Step Definitions (MUST COMPLETE BEFORE 3.3)

**CRITICAL: Constitutional requirement - All BDD feature files and step definitions MUST fail before any implementation**

### District Management Features
- [X] **T006** [P] BDD feature file for district provisioning in `tests/Features/Districts/CreateDistrict.feature`
- [X] **T007** [P] BDD feature file for district lifecycle in `tests/Features/Districts/DistrictLifecycle.feature` 
- [X] **T008** [P] BDD feature file for quota management in `tests/Features/Districts/QuotaManagement.feature`

### Student Management Features  
- [X] **T009** [P] BDD feature file for student creation in `tests/Features/Students/CreateStudent.feature`
- [X] **T010** [P] BDD feature file for student enrollment in `tests/Features/Students/StudentEnrollment.feature`
- [X] **T011** [P] BDD feature file for grade progression in `tests/Features/Students/GradeProgression.feature`
- [X] **T012** [P] BDD feature file for bulk rollover in `tests/Features/Students/BulkRollover.feature`

### Academic Calendar & RBAC Features
- [X] **T013** [P] BDD feature file for academic calendar in `tests/Features/Calendar/AcademicCalendar.feature`
- [X] **T014** [P] BDD feature file for role assignment in `tests/Features/RBAC/RoleAssignment.feature`
- [X] **T015** [P] BDD feature file for composite roles in `tests/Features/RBAC/CompositeRoles.feature`

### Compliance & Audit Features
- [X] **T016** [P] BDD feature file for audit logging in `tests/Features/Compliance/AuditLogging.feature`
- [X] **T017** [P] BDD feature file for retention policies in `tests/Features/Compliance/RetentionPolicies.feature`
- [X] **T018** [P] BDD feature file for legal holds in `tests/Features/Compliance/LegalHolds.feature`

### Assessment Management Features
- [X] **T018a** [P] BDD feature file for assessment management in `tests/Features/Assessments/AssessmentManagement.feature`

### Step Definitions (Must Fail Initially)
- [X] **T019** [P] Step definitions for district management in `tests/StepDefinitions/DistrictSteps.cs`
- [X] **T020** [P] Step definitions for student management in `tests/StepDefinitions/StudentSteps.cs`
- [X] **T021** [P] Step definitions for calendar management in `tests/StepDefinitions/CalendarSteps.cs`
- [X] **T022** [P] Step definitions for RBAC in `tests/StepDefinitions/RbacSteps.cs`
- [X] **T023** [P] Step definitions for compliance in `tests/StepDefinitions/ComplianceSteps.cs`
- [X] **T023a** [P] Step definitions for assessments in `tests/StepDefinitions/AssessmentSteps.cs`

## Phase 3.3: Unit Tests (TDD - Must Fail Before Implementation)

**Coverage requirement: Minimum 90% for domain and application layers**

### Domain Entity Tests
- [X] **T024** [P] Unit tests for DistrictTenant entity in `tests/NorthstarET.Lms.Domain.Tests/Entities/DistrictTenantTests.cs` ✅ 19 tests passing
- [X] **T025** [P] Unit tests for Student entity in `tests/NorthstarET.Lms.Domain.Tests/Entities/StudentTests.cs` ✅ 12 tests passing  
- [X] **T026** [P] Unit tests for DistrictQuotas value object in `tests/NorthstarET.Lms.Domain.Tests/ValueObjects/DistrictQuotasTests.cs` ✅ 7 tests passing
- [X] **T027** [P] Unit tests for SchoolYear entity in `tests/NorthstarET.Lms.Domain.Tests/Entities/SchoolYearTests.cs` ✅ 18 tests passing
- [X] **T030** [P] Unit tests for RoleAssignment entity in `tests/NorthstarET.Lms.Domain.Tests/Entities/RoleAssignmentTests.cs` ✅ 20 tests passing
- [X] **T031** [P] Unit tests for AuditRecord entity in `tests/NorthstarET.Lms.Domain.Tests/Entities/AuditRecordTests.cs` ✅ 15 tests passing
- [X] **T028** [P] Unit tests for AcademicCalendar entity in `tests/NorthstarET.Lms.Domain.Tests/Entities/AcademicCalendarTests.cs` ✅ 18 tests passing
- [X] **T029** [P] Unit tests for Enrollment entity in `tests/NorthstarET.Lms.Domain.Tests/Entities/EnrollmentTests.cs` ✅ 21 tests passing

### Application Service Tests
- [ ] **T032** [P] Unit tests for DistrictService in `tests/NorthstarET.Lms.Application.Tests/Services/DistrictServiceTests.cs`
- [ ] **T033** [P] Unit tests for StudentService in `tests/NorthstarET.Lms.Application.Tests/Services/StudentServiceTests.cs`
- [ ] **T034** [P] Unit tests for EnrollmentService in `tests/NorthstarET.Lms.Application.Tests/Services/EnrollmentServiceTests.cs`
- [ ] **T035** [P] Unit tests for RoleAuthorizationService in `tests/NorthstarET.Lms.Application.Tests/Services/RoleAuthorizationServiceTests.cs`
- [ ] **T036** [P] Unit tests for AuditService in `tests/NorthstarET.Lms.Application.Tests/Services/AuditServiceTests.cs`

### Integration Tests
- [ ] **T037** [P] Integration tests for district API endpoints in `tests/NorthstarET.Lms.Api.Tests/Controllers/DistrictsControllerTests.cs`
- [ ] **T038** [P] Integration tests for student API endpoints in `tests/NorthstarET.Lms.Api.Tests/Controllers/StudentsControllerTests.cs`
- [ ] **T039** [P] Integration tests for multi-tenant isolation in `tests/NorthstarET.Lms.Infrastructure.Tests/MultiTenantIsolationTests.cs`

## Phase 3.4: Domain Layer Implementation (Clean Architecture - ZERO External Dependencies)

### Base Classes & Value Objects
- [X] **T040** [P] TenantScopedEntity base class in `src/NorthstarET.Lms.Domain/Common/TenantScopedEntity.cs` ✅ Already exists
- [X] **T041** [P] Domain events infrastructure in `src/NorthstarET.Lms.Domain/Common/IDomainEvent.cs` ✅ Already exists 
- [X] **T042** [P] Value objects (UserId, ExternalId, GradeLevel) in `src/NorthstarET.Lms.Domain/ValueObjects/` ✅ Enums implemented

### Core Domain Entities
- [X] **T043** [P] DistrictTenant entity in `src/NorthstarET.Lms.Domain/Entities/DistrictTenant.cs` ✅ Already exists
- [X] **T044** [P] SchoolYear entity in `src/NorthstarET.Lms.Domain/Entities/SchoolYear.cs` ✅ Already exists
- [X] **T045** [P] School entity in `src/NorthstarET.Lms.Domain/Entities/School.cs` ✅ Created in Class.cs
- [X] **T046** [P] Student entity in `src/NorthstarET.Lms.Domain/Entities/Student.cs` ✅ Already exists
- [ ] **T047** [P] Staff entity in `src/NorthstarET.Lms.Domain/Entities/Staff.cs`
- [X] **T048** [P] Class entity in `src/NorthstarET.Lms.Domain/Entities/Class.cs` ✅ Created
- [X] **T048a** [P] AcademicCalendar entity in `src/NorthstarET.Lms.Domain/Entities/AcademicCalendar.cs` ✅ Created
- [X] **T049** [P] Enrollment entity in `src/NorthstarET.Lms.Domain/Entities/Enrollment.cs` ✅ Created
- [ ] **T050** [P] Guardian entity in `src/NorthstarET.Lms.Domain/Entities/Guardian.cs`

### Compliance & RBAC Entities
- [ ] **T051** [P] RoleDefinition entity in `src/NorthstarET.Lms.Domain/Entities/RoleDefinition.cs`
- [X] **T052** [P] RoleAssignment entity in `src/NorthstarET.Lms.Domain/Entities/RoleAssignment.cs` ✅ Already exists
- [X] **T053** [P] AuditRecord entity in `src/NorthstarET.Lms.Domain/Entities/AuditRecord.cs` ✅ Already exists
- [ ] **T054** [P] RetentionPolicy entity in `src/NorthstarET.Lms.Domain/Entities/RetentionPolicy.cs`
- [ ] **T055** [P] LegalHold entity in `src/NorthstarET.Lms.Domain/Entities/LegalHold.cs`

### Domain Services & Events
- [ ] **T056** [P] Domain services interfaces in `src/NorthstarET.Lms.Domain/Services/`
- [X] **T057** [P] Domain events for audit trail in `src/NorthstarET.Lms.Domain/Events/` ✅ Enhanced with new events

## Phase 3.5: Application Layer Implementation (Depends Only on Domain)

### Repository Interfaces & DTOs
- [ ] **T058** [P] Repository interfaces in `src/NorthstarET.Lms.Application/Interfaces/`
- [ ] **T059** [P] Data Transfer Objects in `src/NorthstarET.Lms.Application/DTOs/`
- [ ] **T060** [P] Command and query models in `src/NorthstarET.Lms.Application/Commands/` and `Queries/`

### Use Case Services
- [ ] **T061** [P] District management use cases in `src/NorthstarET.Lms.Application/UseCases/Districts/`
- [ ] **T062** [P] Student management use cases in `src/NorthstarET.Lms.Application/UseCases/Students/`
- [ ] **T063** [P] Enrollment management use cases in `src/NorthstarET.Lms.Application/UseCases/Enrollment/`
- [ ] **T064** [P] RBAC management use cases in `src/NorthstarET.Lms.Application/UseCases/RBAC/`
- [ ] **T065** [P] Audit and compliance use cases in `src/NorthstarET.Lms.Application/UseCases/Audit/`

### Application Services
- [ ] **T066** DistrictService application service in `src/NorthstarET.Lms.Application/Services/DistrictService.cs`
- [ ] **T067** StudentService application service in `src/NorthstarET.Lms.Application/Services/StudentService.cs`
- [ ] **T068** EnrollmentService application service in `src/NorthstarET.Lms.Application/Services/EnrollmentService.cs`
- [ ] **T069** RoleAuthorizationService in `src/NorthstarET.Lms.Application/Services/RoleAuthorizationService.cs`
- [ ] **T070** AuditService application service in `src/NorthstarET.Lms.Application/Services/AuditService.cs`

## Phase 3.6: Infrastructure Layer Implementation (EF Core & External Services)

### Database Configuration
- [ ] **T071** Multi-tenant DbContext with schema isolation in `src/NorthstarET.Lms.Infrastructure/Data/LmsDbContext.cs`
- [ ] **T072** [P] Entity configurations for DistrictTenant in `src/NorthstarET.Lms.Infrastructure/Data/Configurations/DistrictTenantConfiguration.cs`
- [ ] **T073** [P] Entity configurations for Student in `src/NorthstarET.Lms.Infrastructure/Data/Configurations/StudentConfiguration.cs`
- [ ] **T074** [P] Entity configurations for Staff in `src/NorthstarET.Lms.Infrastructure/Data/Configurations/StaffConfiguration.cs`
- [ ] **T075** [P] Entity configurations for Enrollment in `src/NorthstarET.Lms.Infrastructure/Data/Configurations/EnrollmentConfiguration.cs`
- [ ] **T076** [P] Entity configurations for audit entities in `src/NorthstarET.Lms.Infrastructure/Data/Configurations/AuditConfiguration.cs`

### Repository Implementations
- [ ] **T077** [P] DistrictRepository implementation in `src/NorthstarET.Lms.Infrastructure/Repositories/DistrictRepository.cs`
- [ ] **T078** [P] StudentRepository implementation in `src/NorthstarET.Lms.Infrastructure/Repositories/StudentRepository.cs`
- [ ] **T079** [P] StaffRepository implementation in `src/NorthstarET.Lms.Infrastructure/Repositories/StaffRepository.cs`
- [ ] **T080** [P] EnrollmentRepository implementation in `src/NorthstarET.Lms.Infrastructure/Repositories/EnrollmentRepository.cs`
- [ ] **T081** [P] AuditRepository implementation in `src/NorthstarET.Lms.Infrastructure/Repositories/AuditRepository.cs`

### External Service Integrations
- [ ] **T082** [P] Entra External ID service in `src/NorthstarET.Lms.Infrastructure/ExternalServices/EntraIdentityService.cs`
- [ ] **T083** [P] Assessment file service in `src/NorthstarET.Lms.Infrastructure/ExternalServices/AssessmentFileService.cs`
- [ ] **T084** [P] Tenant context accessor in `src/NorthstarET.Lms.Infrastructure/Security/TenantContextAccessor.cs`

### Background Services
- [ ] **T085** [P] Retention policy enforcement job in `src/NorthstarET.Lms.Infrastructure/BackgroundServices/RetentionJobService.cs`
- [ ] **T086** [P] Audit chain processor in `src/NorthstarET.Lms.Infrastructure/BackgroundServices/AuditProcessorService.cs`

## Phase 3.7: Presentation Layer Implementation (API Controllers)

### API Controllers - Districts
- [ ] **T087** Districts API controller in `src/NorthstarET.Lms.Api/Controllers/DistrictsController.cs`
- [ ] **T088** District quota management endpoints in existing DistrictsController
- [ ] **T089** District lifecycle management endpoints in existing DistrictsController

### API Controllers - Students  
- [ ] **T090** Students API controller in `src/NorthstarET.Lms.Api/Controllers/StudentsController.cs`
- [ ] **T091** Student enrollment endpoints in existing StudentsController
- [ ] **T092** Student bulk operations endpoints in existing StudentsController

### API Controllers - Other Domains
- [ ] **T093** [P] Schools API controller in `src/NorthstarET.Lms.Api/Controllers/SchoolsController.cs`
- [ ] **T094** [P] Staff API controller in `src/NorthstarET.Lms.Api/Controllers/StaffController.cs`
- [ ] **T095** [P] Assessment API controller in `src/NorthstarET.Lms.Api/Controllers/AssessmentsController.cs`
- [ ] **T096** [P] Audit API controller in `src/NorthstarET.Lms.Api/Controllers/AuditController.cs`

### Middleware & Security
- [ ] **T097** [P] Tenant isolation middleware in `src/NorthstarET.Lms.Api/Middleware/TenantIsolationMiddleware.cs`
- [ ] **T098** [P] Audit logging middleware in `src/NorthstarET.Lms.Api/Middleware/AuditLoggingMiddleware.cs`
- [ ] **T099** [P] Security monitoring middleware in `src/NorthstarET.Lms.Api/Middleware/SecurityMonitoringMiddleware.cs`
- [ ] **T100** [P] JWT authentication configuration in `src/NorthstarET.Lms.Api/Authentication/JwtConfiguration.cs`

## Phase 3.8: Aspire Orchestration & Integration

**All integrations MUST use Aspire components and service discovery**

- [ ] **T101** Aspire app host configuration in `src/NorthstarET.Lms.AppHost/Program.cs`
- [ ] **T102** Multi-tenant database connection management using Aspire SQL Server component
- [ ] **T103** Redis integration for caching using Aspire Redis component  
- [ ] **T104** Service registration and dependency injection in `src/NorthstarET.Lms.Api/Program.cs`
- [ ] **T105** Health checks configuration for all services
- [ ] **T106** Structured logging configuration with Aspire observability
- [ ] **T107** Configuration management for multi-tenant settings

## Phase 3.9: Database Migration & Seeding

- [ ] **T108** Create initial EF Core migration for tenant schema
- [ ] **T109** [P] Seed data for default retention policies
- [ ] **T110** [P] Seed data for system role definitions
- [ ] **T111** Migration scripts for multi-tenant schema provisioning
- [ ] **T112** Database initialization and tenant provisioning logic

## Phase 3.10: Polish & Quality Gates

### Performance & Security
- [ ] **T113** [P] Performance tests for CRUD operations (<200ms p95) in `tests/Performance/CrudPerformanceTests.cs`
- [ ] **T114** [P] Performance tests for bulk operations (<120s for 10k records) in `tests/Performance/BulkOperationTests.cs`
- [ ] **T115** [P] Audit query performance tests (<2s for 1M records) in `tests/Performance/AuditQueryTests.cs`
- [ ] **T116** [P] Security analysis and penetration testing validation
- [ ] **T117** [P] Multi-tenant data isolation validation tests

### Code Quality & Documentation
- [ ] **T118** [P] Static analysis compliance and nullable reference types
- [ ] **T119** [P] Code coverage verification (>90% for domain/application layers)
- [ ] **T120** [P] API documentation with OpenAPI/Swagger annotations
- [ ] **T121** [P] Architecture decision records for key design choices
- [ ] **T122** Final BDD scenario validation and acceptance testing

## Dependencies

### Phase Dependencies
- Setup (T001-T005) → BDD Features (T006-T023) → Unit Tests (T024-T039) → Domain (T040-T057) → Application (T058-T070) → Infrastructure (T071-T086) → Presentation (T087-T100) → Integration (T101-T112) → Polish (T113-T122)

### Key Blocking Dependencies
- BDD feature files (T006-T018) MUST complete before step definitions (T019-T023)
- All tests (T024-T039) MUST fail before any implementation begins
- Domain entities (T040-T055) before application services (T058-T070)
- Application services before infrastructure repositories (T071-T086)
- Infrastructure before presentation controllers (T087-T100)

## Parallel Execution Examples

### BDD Features (Can Run Together)
```bash
# Execute T006-T018 in parallel (different feature files)
Task: "BDD feature file for district provisioning in tests/Features/Districts/CreateDistrict.feature"
Task: "BDD feature file for student creation in tests/Features/Students/CreateStudent.feature" 
Task: "BDD feature file for academic calendar in tests/Features/Calendar/AcademicCalendar.feature"
```

### Domain Entities (Can Run Together)
```bash
# Execute T043-T055 in parallel (different entity files)
Task: "DistrictTenant entity in src/NorthstarET.Lms.Domain/Entities/DistrictTenant.cs"
Task: "Student entity in src/NorthstarET.Lms.Domain/Entities/Student.cs"
Task: "Staff entity in src/NorthstarET.Lms.Domain/Entities/Staff.cs"
```

### Infrastructure Components (Can Run Together)
```bash
# Execute T072-T076 in parallel (different configuration files)
Task: "Entity configurations for DistrictTenant in src/NorthstarET.Lms.Infrastructure/Data/Configurations/DistrictTenantConfiguration.cs"
Task: "Entity configurations for Student in src/NorthstarET.Lms.Infrastructure/Data/Configurations/StudentConfiguration.cs"
```

## Validation Checklist

**Constitutional Compliance**:
- ✅ All user stories have BDD feature files (T006-T018)
- ✅ All feature files have corresponding step definitions (T019-T023)  
- ✅ All entities have domain layer tasks (T040-T055)
- ✅ All use cases have application layer tasks (T058-T070)
- ✅ BDD features and tests come before implementation
- ✅ Clean Architecture dependencies respected
- ✅ Aspire orchestration included (T101-T107)
- ✅ Performance requirements validated (T113-T115)

**Task Quality**:
- ✅ Each task specifies exact file path
- ✅ Parallel tasks are truly independent ([P] marking)
- ✅ No task modifies same file as another [P] task
- ✅ TDD ordering enforced (tests before implementation)

---

**122 Tasks Generated** | **Constitutional Requirements Met** | **Ready for Execution**

*Generated from complete design artifacts: plan.md, research.md, data-model.md, contracts/, quickstart.md*