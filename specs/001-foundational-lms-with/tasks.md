# Tasks: Foundational LMS with Tenant Isolation and Compliance

**Input**: Design documents from `/specs/001-foundational-lms-with/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)

```
1. Load plan.md from feature directory
   → If not found: ERROR "No implementation plan found"
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Extract decisions → setup tasks
3. Generate tasks by category:
   → Setup: project init, dependencies, linting
   → Tests: contract tests, integration tests
   → Core: models, services, CLI commands
   → Integration: DB, middleware, logging
   → Polish: unit tests, performance, docs
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have tests?
   → All entities have models?
   → All endpoints implemented?
9. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions
- Reference the verifying test(s) (feature file, test class, suite) that prove the task complete

## Path Conventions

Based on plan.md structure:
- **Domain**: `src/Domain/`
- **Application**: `src/Application/`
- **Infrastructure**: `src/Infrastructure/`
- **Presentation**: `src/Presentation/`
- **Tests**: `tests/`

## Phase 3.1: Setup

- [x] T001 Create .NET 9 solution structure per implementation plan in repository root
- [x] T002 Initialize Domain project with zero external dependencies
- [x] T003 [P] Initialize Application project referencing only Domain
- [x] T004 [P] Initialize Infrastructure project referencing Application and Domain
- [x] T005 [P] Initialize Presentation.Api project with ASP.NET Core Minimal APIs
- [x] T006 [P] Initialize Presentation.Aspire.AppHost project for orchestration
- [x] T007 [P] Initialize test projects (Domain, Application, Infrastructure, Presentation, Bdd)
- [x] T008 [P] Configure EditorConfig and Directory.Build.props for code formatting
- [x] T009 [P] Configure NuGet packages: EF Core 9, MediatR, FluentAssertions, Reqnroll, xUnit, Testcontainers

## Phase 3.2: BDD Features & Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3

**CRITICAL: Constitutional requirement - BDD feature files MUST be complete before any code**
**Tests MUST be written and MUST FAIL before ANY implementation**
**Step definitions MUST be fully implemented (no placeholders) and executed at the start of this phase**
**Record a full solution build and failing suite before moving to implementation tasks**
**Coverage requirement: Minimum 90% for domain and application layers**

### BDD Feature Files (from quickstart.md scenarios)

- [ ] T010 [P] Feature file for district provisioning in `tests/Bdd/Features/DistrictProvisioning.feature`
- [ ] T011 [P] Feature file for identity mapping lifecycle in `tests/Bdd/Features/IdentityLifecycle.feature`
- [ ] T012 [P] Feature file for academic calendar validation in `tests/Bdd/Features/AcademicCalendar.feature`
- [ ] T013 [P] Feature file for composite role authorization in `tests/Bdd/Features/RoleAuthorization.feature`
- [ ] T014 [P] Feature file for bulk student rollover in `tests/Bdd/Features/BulkRollover.feature`
- [ ] T015 [P] Feature file for legal hold retention in `tests/Bdd/Features/LegalHoldRetention.feature`
- [ ] T016 [P] Feature file for security anomaly detection in `tests/Bdd/Features/SecurityAnomalyDetection.feature`

### BDD Step Definitions

- [ ] T017 [P] Step definitions for district provisioning in `tests/Bdd/StepDefinitions/DistrictProvisioningSteps.cs`
- [ ] T018 [P] Step definitions for identity lifecycle in `tests/Bdd/StepDefinitions/IdentityLifecycleSteps.cs`
- [ ] T019 [P] Step definitions for academic calendar in `tests/Bdd/StepDefinitions/AcademicCalendarSteps.cs`
- [ ] T020 [P] Step definitions for role authorization in `tests/Bdd/StepDefinitions/RoleAuthorizationSteps.cs`
- [ ] T021 [P] Step definitions for bulk rollover in `tests/Bdd/StepDefinitions/BulkRolloverSteps.cs`
- [ ] T022 [P] Step definitions for legal hold retention in `tests/Bdd/StepDefinitions/LegalHoldRetentionSteps.cs`
- [ ] T023 [P] Step definitions for security anomaly in `tests/Bdd/StepDefinitions/SecurityAnomalySteps.cs`

### Contract Tests (one per contract file)

- [ ] T024 [P] Contract tests for district provisioning API in `tests/Presentation/DistrictProvisioningContractTests.cs`
- [ ] T025 [P] Contract tests for identity lifecycle API in `tests/Presentation/IdentityLifecycleContractTests.cs`
- [ ] T026 [P] Contract tests for academic calendar API in `tests/Presentation/AcademicCalendarContractTests.cs`
- [ ] T027 [P] Contract tests for RBAC API in `tests/Presentation/RbacContractTests.cs`
- [ ] T028 [P] Contract tests for enrollment API in `tests/Presentation/EnrollmentContractTests.cs`
- [ ] T029 [P] Contract tests for assessments API in `tests/Presentation/AssessmentsContractTests.cs`
- [ ] T030 [P] Contract tests for compliance API in `tests/Presentation/ComplianceContractTests.cs`

### Unit Tests for Domain Entities

- [ ] T031 [P] Unit tests for DistrictTenant entity in `tests/Domain/Entities/DistrictTenantTests.cs`
- [ ] T032 [P] Unit tests for SchoolYear entity in `tests/Domain/Entities/SchoolYearTests.cs`
- [ ] T033 [P] Unit tests for AcademicCalendar entity in `tests/Domain/Entities/AcademicCalendarTests.cs`
- [ ] T034 [P] Unit tests for School entity in `tests/Domain/Entities/SchoolTests.cs`
- [ ] T035 [P] Unit tests for Class entity in `tests/Domain/Entities/ClassTests.cs`
- [ ] T036 [P] Unit tests for Staff entity in `tests/Domain/Entities/StaffTests.cs`
- [ ] T037 [P] Unit tests for Student entity in `tests/Domain/Entities/StudentTests.cs`
- [ ] T038 [P] Unit tests for Guardian entity in `tests/Domain/Entities/GuardianTests.cs`
- [ ] T039 [P] Unit tests for RoleDefinition entity in `tests/Domain/Entities/RoleDefinitionTests.cs`
- [ ] T040 [P] Unit tests for RoleAssignment entity in `tests/Domain/Entities/RoleAssignmentTests.cs`
- [ ] T041 [P] Unit tests for Enrollment entity in `tests/Domain/Entities/EnrollmentTests.cs`
- [ ] T042 [P] Unit tests for AssessmentDefinition entity in `tests/Domain/Entities/AssessmentDefinitionTests.cs`
- [ ] T043 [P] Unit tests for IdentityMapping entity in `tests/Domain/Entities/IdentityMappingTests.cs`
- [ ] T044 [P] Unit tests for RetentionPolicy entity in `tests/Domain/Entities/RetentionPolicyTests.cs`
- [ ] T045 [P] Unit tests for LegalHold entity in `tests/Domain/Entities/LegalHoldTests.cs`
- [ ] T046 [P] Unit tests for AuditRecord entity in `tests/Domain/Entities/AuditRecordTests.cs`
- [ ] T047 [P] Unit tests for BulkJob entity in `tests/Domain/Entities/BulkJobTests.cs`

### Unit Tests for Application Services

- [ ] T048 [P] Unit tests for district management service in `tests/Application/Services/DistrictManagementServiceTests.cs`
- [ ] T049 [P] Unit tests for identity mapping service in `tests/Application/Services/IdentityMappingServiceTests.cs`
- [ ] T050 [P] Unit tests for academic calendar service in `tests/Application/Services/AcademicCalendarServiceTests.cs`
- [ ] T051 [P] Unit tests for role management service in `tests/Application/Services/RoleManagementServiceTests.cs`
- [ ] T052 [P] Unit tests for enrollment service in `tests/Application/Services/EnrollmentServiceTests.cs`
- [ ] T053 [P] Unit tests for assessment service in `tests/Application/Services/AssessmentServiceTests.cs`
- [ ] T054 [P] Unit tests for bulk operation service in `tests/Application/Services/BulkOperationServiceTests.cs`
- [ ] T055 [P] Unit tests for audit service in `tests/Application/Services/AuditServiceTests.cs`
- [ ] T056 [P] Unit tests for retention service in `tests/Application/Services/RetentionServiceTests.cs`

### Integration Tests

- [ ] T057 [P] Integration tests for district provisioning APIs in `tests/Integration/DistrictProvisioningIntegrationTests.cs`
- [ ] T058 [P] Integration tests for tenant isolation in `tests/Integration/TenantIsolationIntegrationTests.cs`
- [ ] T059 [P] Integration tests for audit trail integrity in `tests/Integration/AuditTrailIntegrationTests.cs`
- [ ] T060 [P] Integration tests for bulk operations in `tests/Integration/BulkOperationIntegrationTests.cs`

_Gate_: Before proceeding to Phase 3.3, run a full solution build and execute the entire test suite, capturing the failing evidence tied to the tasks above.

## Phase 3.3: Clean Architecture Implementation (ONLY after Phase 3.2 red state is recorded)

**All implementations MUST follow Clean Architecture layers with proper dependency flow**
**Domain layer MUST have zero external dependencies and the phase must end with the full suite back to green**

### Domain Layer - Core Entities

- [ ] T061 [P] TenantScopedEntity base class in `src/Domain/Entities/TenantScopedEntity.cs`
- [ ] T062 [P] DistrictTenant aggregate in `src/Domain/Entities/DistrictTenant.cs`
- [ ] T063 [P] SchoolYear aggregate in `src/Domain/Entities/SchoolYear.cs`
- [ ] T064 [P] AcademicCalendar aggregate in `src/Domain/Entities/AcademicCalendar.cs`
- [ ] T065 [P] School aggregate in `src/Domain/Entities/School.cs`
- [ ] T066 [P] Class aggregate in `src/Domain/Entities/Class.cs`
- [ ] T067 [P] Staff aggregate in `src/Domain/Entities/Staff.cs`
- [ ] T068 [P] Student aggregate in `src/Domain/Entities/Student.cs`
- [ ] T069 [P] Guardian aggregate in `src/Domain/Entities/Guardian.cs`
- [ ] T070 [P] RoleDefinition aggregate in `src/Domain/Entities/RoleDefinition.cs`
- [ ] T071 [P] RoleAssignment aggregate in `src/Domain/Entities/RoleAssignment.cs`
- [ ] T072 [P] Enrollment aggregate in `src/Domain/Entities/Enrollment.cs`
- [ ] T073 [P] AssessmentDefinition aggregate in `src/Domain/Entities/AssessmentDefinition.cs`
- [ ] T074 [P] IdentityMapping aggregate in `src/Domain/Entities/IdentityMapping.cs`
- [ ] T075 [P] RetentionPolicy aggregate in `src/Domain/Entities/RetentionPolicy.cs`
- [ ] T076 [P] LegalHold aggregate in `src/Domain/Entities/LegalHold.cs`
- [ ] T077 [P] AuditRecord aggregate in `src/Domain/Entities/AuditRecord.cs`
- [ ] T078 [P] BulkJob aggregate in `src/Domain/Entities/BulkJob.cs`

### Domain Layer - Value Objects and Events

- [ ] T079 [P] Value objects (TenantSlug, Quota, etc.) in `src/Domain/ValueObjects/`
- [ ] T080 [P] Domain events in `src/Domain/Events/`
- [ ] T081 [P] Domain interfaces in `src/Domain/Interfaces/`

### Application Layer

- [ ] T082 [P] Commands and command handlers in `src/Application/Commands/`
- [ ] T083 [P] Queries and query handlers in `src/Application/Queries/`
- [ ] T084 [P] DTOs in `src/Application/DTOs/`
- [ ] T085 [P] Validators using FluentValidation in `src/Application/Validators/`
- [ ] T086 [P] Application service interfaces in `src/Application/Abstractions/`
- [ ] T087 [P] MediatR pipeline behaviors in `src/Application/Behaviors/`

### Infrastructure Layer - Persistence

- [ ] T088 [P] EF Core DbContext with tenant schema support in `src/Infrastructure/Persistence/LmsDbContext.cs`
- [ ] T089 [P] Entity configurations in `src/Infrastructure/Persistence/Configurations/`
- [ ] T090 [P] Repository implementations in `src/Infrastructure/Persistence/Repositories/`
- [ ] T091 [P] Tenant context accessor in `src/Infrastructure/Persistence/TenantContextAccessor.cs`

### Infrastructure Layer - External Services

- [ ] T092 [P] Identity mapping service implementation in `src/Infrastructure/Identity/IdentityMappingService.cs`
- [ ] T093 [P] File storage service for assessments in `src/Infrastructure/Files/AssessmentFileService.cs`
- [ ] T094 [P] Audit service implementation in `src/Infrastructure/Audit/AuditService.cs`
- [ ] T095 [P] Background job service in `src/Infrastructure/BackgroundJobs/BackgroundJobService.cs`

### Presentation Layer - API Controllers

- [ ] T096 [P] District provisioning controller in `src/Presentation/Api/Controllers/DistrictsController.cs`
- [ ] T097 [P] Identity lifecycle controller in `src/Presentation/Api/Controllers/IdentityController.cs`
- [ ] T098 [P] Academic calendar controller in `src/Presentation/Api/Controllers/AcademicCalendarController.cs`
- [ ] T099 [P] RBAC controller in `src/Presentation/Api/Controllers/RolesController.cs`
- [ ] T100 [P] Enrollment controller in `src/Presentation/Api/Controllers/EnrollmentController.cs`
- [ ] T101 [P] Assessments controller in `src/Presentation/Api/Controllers/AssessmentsController.cs`
- [ ] T102 [P] Compliance controller in `src/Presentation/Api/Controllers/ComplianceController.cs`
- [ ] T103 [P] Bulk operations controller in `src/Presentation/Api/Controllers/BulkController.cs`

### Dependency Injection and Composition Root

- [ ] T104 Dependency injection setup in `src/Presentation/CompositionRoot/DependencyInjection.cs`
- [ ] T105 Service registration with proper layer separation
- [ ] T106 Configure MediatR pipeline in `src/Presentation/Api/Program.cs`

_Gate_: Before moving to Phase 3.4, run a full solution build and execute the entire suite; only proceed when all tests (including those from earlier phases) return to green.

## Phase 3.4: Integration & Aspire Orchestration

**All integrations MUST use Aspire components and abstractions**
**Structured logging with ILogger and performance monitoring required**

- [ ] T107 Aspire AppHost configuration in `src/Presentation/Aspire/AppHost/Program.cs`
- [ ] T108 SQL Server integration using Aspire SqlServer resource
- [ ] T109 Azurite blob storage integration using Aspire storage resource
- [ ] T110 Service discovery setup for LMS API service
- [ ] T111 Configuration management with Aspire configuration abstractions
- [ ] T112 Health checks configuration using Aspire health check abstractions
- [ ] T113 Structured logging implementation with Aspire logging abstractions
- [ ] T114 Background services orchestration via Aspire hosted services
- [ ] T115 Database migration automation in Aspire startup
- [ ] T116 Tenant schema provisioning automation

_Gate_: Before transitioning to Phase 3.5, rerun the full solution build and test suite to confirm the integrations leave the system green.

## Phase 3.5: Polish & Quality Gates

- [ ] T117 [P] Additional unit tests to achieve >90% coverage for Domain layer
- [ ] T118 [P] Additional unit tests to achieve >90% coverage for Application layer
- [ ] T119 [P] Performance tests to validate SLA requirements (<200ms p95 for CRUD APIs) in `tests/Performance/`
- [ ] T120 [P] Bulk operation performance tests (<120s for 10k rows) in `tests/Performance/`
- [ ] T121 [P] Security analysis and vulnerability scanning
- [ ] T122 [P] Static analysis and nullable reference types compliance
- [ ] T123 [P] OpenAPI documentation validation and schema export
- [ ] T124 [P] Tenant isolation security testing
- [ ] T125 [P] Audit trail tamper-evident verification tests
- [ ] T126 Code quality review and refactoring
- [ ] T127 BDD scenario validation and acceptance testing via quickstart.md
- [ ] T128 End-to-end performance validation under load

## Dependencies

### Setup Dependencies
- T001 (solution structure) before all other tasks
- T002-T009 (project setup) before any implementation

### BDD and Test Dependencies
- BDD Features (T010-T016) before step definitions (T017-T023)
- Step definitions (T017-T023) before contract tests (T024-T030)
- All test tasks (T010-T060) before implementation (T061+)

### Domain Layer Dependencies
- T061 (TenantScopedEntity) before all other domain entities (T062-T078)
- Domain entities (T062-T078) before value objects and events (T079-T081)

### Application Layer Dependencies
- Domain layer (T061-T081) before application layer (T082-T087)

### Infrastructure Layer Dependencies
- Application layer (T082-T087) before infrastructure (T088-T095)
- T088 (DbContext) before configurations and repositories (T089-T090)

### Presentation Layer Dependencies
- Infrastructure layer (T088-T095) before presentation controllers (T096-T103)
- Controllers (T096-T103) before composition root (T104-T106)

### Integration Dependencies
- Implementation (T061-T106) before integration (T107-T116)
- Aspire setup (T107) before other integration tasks (T108-T116)

### Polish Dependencies
- Integration (T107-T116) before polish (T117-T128)
- Code complete before performance testing (T119-T120, T128)

### Task Completion Requirements
- Tasks may only be marked complete after their verifying tests pass and a full solution build succeeds

## Parallel Example

```bash
# Phase 3.2 - Launch BDD features together (different files):
Task: "Feature file for district provisioning in tests/Bdd/Features/DistrictProvisioning.feature"
Task: "Feature file for identity mapping lifecycle in tests/Bdd/Features/IdentityLifecycle.feature"
Task: "Feature file for academic calendar validation in tests/Bdd/Features/AcademicCalendar.feature"
Task: "Feature file for composite role authorization in tests/Bdd/Features/RoleAuthorization.feature"

# Phase 3.3 - Launch domain entities together (different files):
Task: "DistrictTenant aggregate in src/Domain/Entities/DistrictTenant.cs"
Task: "SchoolYear aggregate in src/Domain/Entities/SchoolYear.cs"
Task: "School aggregate in src/Domain/Entities/School.cs"
Task: "Student aggregate in src/Domain/Entities/Student.cs"

# Phase 3.3 - Launch application services together (different files):
Task: "Commands and command handlers in src/Application/Commands/"
Task: "Queries and query handlers in src/Application/Queries/"
Task: "DTOs in src/Application/DTOs/"
Task: "Validators using FluentValidation in src/Application/Validators/"
```

## Notes

- [P] tasks = different files, no dependencies
- Verify BDD feature files are complete, step definitions are fully implemented, and they fail before implementing
- Run a full solution build and execute the entire suite at the start and end of every phase; record evidence linked to task IDs
- Maintain Clean Architecture layer boundaries with compile-time enforcement
- Use Aspire for all service orchestration and external resource management
- Commit after each task and include references to the verifying tests in commit notes when applicable
- Do not mark tasks complete until their verifying tests pass and the full suite is green
- Multi-tenant isolation must be maintained throughout all implementations
- FERPA compliance and audit requirements must be satisfied in all data operations

## Task Generation Rules

_Applied during main() execution_

1. **From Contracts**:
   - 7 contract files → 7 contract test tasks [P] (T024-T030)
   - Each endpoint → corresponding controller implementation task

2. **From Data Model**:
   - 18 entities → 18 domain entity creation tasks [P] (T062-T078)
   - 18 entities → 18 unit test tasks [P] (T031-T047)
   - 9 application services → 9 service test tasks [P] (T048-T056)

3. **From User Stories (quickstart.md)**:
   - 7 scenarios → 7 BDD feature files [P] (T010-T016)
   - 7 scenarios → 7 step definition files [P] (T017-T023)
   - Integration tests for end-to-end validation (T057-T060)

4. **Ordering**:
   - Setup → BDD Features → Step Definitions → Unit Tests → Domain → Application → Infrastructure → Presentation → Integration → Polish
   - Dependencies block parallel execution
   - Clean Architecture layer dependencies must be respected
   - TDD red-green cycle enforced with full solution build gates

## Validation Checklist

_GATE: Checked by main() before returning_

- [x] All user stories have BDD feature files (T010-T016)
- [x] All feature files have corresponding step definitions (T017-T023)
- [x] All entities have domain layer tasks (T062-T078)
- [x] All entities have unit test tasks (T031-T047)
- [x] All use cases have application layer tasks (T082-T087)
- [x] All contracts have contract test tasks (T024-T030)
- [x] BDD features and tests come before implementation
- [x] Clean Architecture dependencies respected
- [x] Parallel tasks truly independent
- [x] Each task specifies exact file path
- [x] No task modifies same file as another [P] task
- [x] Each task references verifying test(s)
- [x] Full solution build/test gates recorded for every phase
- [x] Multi-tenant isolation requirements covered
- [x] FERPA compliance and audit requirements addressed