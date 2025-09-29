# Tasks: Foundational LMS with Tenant Isolation and Compliance

**Feature**: 001-foundational-lms-with  
**Generated**: December 19, 2024  
**Input**: Design documents from `/specs/001-foundational-lms-with/`  
**Prerequisites**: plan.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅ | quickstart.md ✅

## Summary

This task list implements a foundational K-12 Learning Management System with strict multi-tenant isolation, comprehensive RBAC, and FERPA compliance. The system uses .NET 9 with Clean Architecture, Aspire orchestration, schema-per-tenant isolation, and BDD-first development.

**Key Features**:
- Multi-tenant data isolation (schema-per-tenant)
- Comprehensive RBAC with hierarchical roles
- Tamper-evident audit logging
- Bulk operations with configurable error handling
- Entra External ID integration
- Assessment file management with secure access

**Tech Stack**: .NET 9, ASP.NET Core, Entity Framework Core, SQL Server, .NET Aspire, Reqnroll, xUnit

---

## Phase 3.1: Project Setup

### T001: Create Clean Architecture Solution Structure
**Description**: Create the complete .NET solution with Clean Architecture layers as defined in plan.md  
**Files**: Root directory structure  
**Commands**:
```bash
# Create solution
dotnet new sln -n NorthstarET.Lms

# Create Domain layer (zero dependencies)
dotnet new classlib -n NorthstarET.Lms.Domain -o src/NorthstarET.Lms.Domain
dotnet sln add src/NorthstarET.Lms.Domain

# Create Application layer
dotnet new classlib -n NorthstarET.Lms.Application -o src/NorthstarET.Lms.Application
dotnet sln add src/NorthstarET.Lms.Application
dotnet add src/NorthstarET.Lms.Application reference src/NorthstarET.Lms.Domain

# Create Infrastructure layer
dotnet new classlib -n NorthstarET.Lms.Infrastructure -o src/NorthstarET.Lms.Infrastructure
dotnet sln add src/NorthstarET.Lms.Infrastructure
dotnet add src/NorthstarET.Lms.Infrastructure reference src/NorthstarET.Lms.Application

# Create API layer
dotnet new webapi -n NorthstarET.Lms.Api -o src/NorthstarET.Lms.Api
dotnet sln add src/NorthstarET.Lms.Api
dotnet add src/NorthstarET.Lms.Api reference src/NorthstarET.Lms.Infrastructure

# Create Aspire orchestration
dotnet new aspire-apphost -n NorthstarET.Lms.AppHost -o src/NorthstarET.Lms.AppHost
dotnet sln add src/NorthstarET.Lms.AppHost
dotnet add src/NorthstarET.Lms.AppHost reference src/NorthstarET.Lms.Api
```

---

### T002: Initialize NuGet Packages for All Layers
**Description**: Add required NuGet packages to each project layer  
**Files**: All .csproj files  
**Dependencies**: T001  

---

### T003: Create Test Projects with Reqnroll and Testing Dependencies
**Description**: Create all test projects with BDD and unit testing frameworks  
**Files**: tests/ directory structure  
**Dependencies**: T001  

---

### T004 [P]: Configure EditorConfig and Code Analysis Rules
**Description**: Setup .editorconfig with C# 13 nullable reference types and code quality rules  
**Files**: `.editorconfig` at repository root  
**Dependencies**: T001

---

### T005 [P]: Configure Aspire Orchestration in AppHost
**Description**: Configure Aspire app host with SQL Server, Redis, and API service registration  
**Files**: `src/NorthstarET.Lms.AppHost/Program.cs`  
**Dependencies**: T002

---

## Phase 3.2: BDD Features & Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3

**CRITICAL: Constitutional requirement - BDD feature files MUST be complete before any code**  
**Tests MUST be written and MUST FAIL before ANY implementation**  
**Coverage requirement: Minimum 90% for domain and application layers**

---

### T006 [P]: Feature file for District provisioning and management
**Description**: Create Reqnroll feature file covering all district CRUD operations from contracts/districts-api.md  
**Files**: `tests/Features/Districts/ManageDistricts.feature`  
**Scenarios**: Create district, get district, list districts, update district, suspend/reactivate, delete with retention checks  
**References**: FR-001 to FR-010, contracts/districts-api.md

---

### T007 [P]: Feature file for District quota management
**Description**: Create Reqnroll feature file for quota tracking and enforcement  
**Files**: `tests/Features/Districts/DistrictQuotas.feature`  
**Scenarios**: Check quota status, update quotas, enforce quota limits during user creation  
**References**: FR-008, FR-009

---

### T008 [P]: Feature file for Student CRUD operations
**Description**: Create Reqnroll feature file covering student creation, retrieval, update from contracts/students-api.md  
**Files**: `tests/Features/Students/ManageStudents.feature`  
**Scenarios**: Create student with guardians, get student with history, list students with filters, update student info  
**References**: FR-011 to FR-015, contracts/students-api.md

---

### T009 [P]: Feature file for Student enrollment management
**Description**: Create Reqnroll feature file for class enrollment and transfers  
**Files**: `tests/Features/Students/StudentEnrollment.feature`  
**Scenarios**: Enroll in class, withdraw from class, transfer between schools  
**References**: FR-016 to FR-020

---

### T010 [P]: Feature file for Student grade progression
**Description**: Create Reqnroll feature file for grade promotion and bulk rollover  
**Files**: `tests/Features/Students/GradeProgression.feature`  
**Scenarios**: Individual promotion, bulk rollover preview, execute rollover with error handling  
**References**: FR-021 to FR-025

---

### T011 [P]: Feature file for School year and academic calendar
**Description**: Create Reqnroll feature file for school year lifecycle and calendar management  
**Files**: `tests/Features/AcademicCalendar/SchoolYearManagement.feature`  
**Scenarios**: Create school year, define terms/closures, archive school year  
**References**: FR-026 to FR-030

---

### T012 [P]: Feature file for RBAC role definitions and assignments
**Description**: Create Reqnroll feature file for role-based access control  
**Files**: `tests/Features/Authorization/RoleBasedAccess.feature`  
**Scenarios**: Create role definition, assign role with scope, delegation, deny-by-default enforcement  
**References**: FR-036 to FR-043

---

### T013 [P]: Feature file for Bulk operations with error strategies
**Description**: Create Reqnroll feature file for bulk import with all error handling modes  
**Files**: `tests/Features/BulkOperations/BulkImport.feature`  
**Scenarios**: All-or-nothing, best-effort, threshold-based, preview mode  
**References**: FR-031 to FR-035

---

### T014 [P]: Feature file for Assessment file management
**Description**: Create Reqnroll feature file for PDF assessment upload and secure access  
**Files**: `tests/Features/Assessments/AssessmentFiles.feature`  
**Scenarios**: Upload assessment, generate scoped URL, enforce quota limits, access validation  
**References**: FR-051 to FR-054

---

### T015 [P]: Feature file for Audit logging and compliance
**Description**: Create Reqnroll feature file for audit trail, retention, and legal holds  
**Files**: `tests/Features/Compliance/AuditAndRetention.feature`  
**Scenarios**: Audit record creation, chain integrity, retention policy enforcement, legal hold management  
**References**: FR-044 to FR-050

---

### T016 [P]: Feature file for Identity mapping with Entra External ID
**Description**: Create Reqnroll feature file for external identity provider integration  
**Files**: `tests/Features/Identity/ExternalIdentityMapping.feature`  
**Scenarios**: Map user to external ID, prevent duplicate mappings, lifecycle synchronization  
**References**: FR-055 to FR-060

---

### T017 [P]: Step definitions for District management features
**Description**: Implement Reqnroll step definitions for District feature files (T006, T007)  
**Files**: `tests/StepDefinitions/DistrictSteps.cs`  
**Dependencies**: T006, T007  
**Initial State**: All steps throw PendingStepException (RED phase)

---

### T018 [P]: Step definitions for Student management features
**Description**: Implement Reqnroll step definitions for Student feature files (T008, T009, T010)  
**Files**: `tests/StepDefinitions/StudentSteps.cs`  
**Dependencies**: T008, T009, T010  
**Initial State**: All steps throw PendingStepException (RED phase)

---

### T019 [P]: Step definitions for Academic calendar features
**Description**: Implement Reqnroll step definitions for School year and calendar (T011)  
**Files**: `tests/StepDefinitions/AcademicCalendarSteps.cs`  
**Dependencies**: T011  
**Initial State**: All steps throw PendingStepException (RED phase)

---

### T020 [P]: Step definitions for RBAC features
**Description**: Implement Reqnroll step definitions for role-based access control (T012)  
**Files**: `tests/StepDefinitions/RBACSteps.cs`  
**Dependencies**: T012  
**Initial State**: All steps throw PendingStepException (RED phase)

---

### T021 [P]: Step definitions for Bulk operations features
**Description**: Implement Reqnroll step definitions for bulk import operations (T013)  
**Files**: `tests/StepDefinitions/BulkOperationSteps.cs`  
**Dependencies**: T013  
**Initial State**: All steps throw PendingStepException (RED phase)

---

### T022 [P]: Step definitions for Assessment features
**Description**: Implement Reqnroll step definitions for assessment file management (T014)  
**Files**: `tests/StepDefinitions/AssessmentSteps.cs`  
**Dependencies**: T014  
**Initial State**: All steps throw PendingStepException (RED phase)

---

### T023 [P]: Step definitions for Compliance features
**Description**: Implement Reqnroll step definitions for audit and retention (T015)  
**Files**: `tests/StepDefinitions/ComplianceSteps.cs`  
**Dependencies**: T015  
**Initial State**: All steps throw PendingStepException (RED phase)

---

### T024 [P]: Step definitions for Identity mapping features
**Description**: Implement Reqnroll step definitions for Entra External ID integration (T016)  
**Files**: `tests/StepDefinitions/IdentitySteps.cs`  
**Dependencies**: T016  
**Initial State**: All steps throw PendingStepException (RED phase)

---

### T025 [P]: Unit tests for DistrictTenant domain entity
**Description**: Write comprehensive unit tests for DistrictTenant aggregate root  
**Files**: `tests/NorthstarET.Lms.Domain.Tests/Entities/DistrictTenantTests.cs`  
**Test Cases**: Creation with valid data, validation rules, status transitions, domain events

---

[CONTINUING WITH REMAINING TASKS T026-T110...]


### T026 [P]: Unit tests for SchoolYear and AcademicCalendar entities
**Description**: Write unit tests for school year lifecycle and calendar management  
**Files**: `tests/NorthstarET.Lms.Domain.Tests/Entities/SchoolYearTests.cs`, `tests/NorthstarET.Lms.Domain.Tests/Entities/AcademicCalendarTests.cs`  
**Test Cases**: School year creation, term management, closure definition, archival validation

---

### T027 [P]: Unit tests for Student entity
**Description**: Write comprehensive unit tests for Student aggregate root  
**Files**: `tests/NorthstarET.Lms.Domain.Tests/Entities/StudentTests.cs`  
**Test Cases**: Creation, program flags, status transitions, accommodation tags

---

### T028 [P]: Unit tests for Staff entity
**Description**: Write unit tests for Staff entity and lifecycle management  
**Files**: `tests/NorthstarET.Lms.Domain.Tests/Entities/StaffTests.cs`  
**Test Cases**: Hire/termination, status management, employee number validation

---

### T029 [P]: Unit tests for Guardian and relationships
**Description**: Write unit tests for Guardian entity and GuardianStudentRelationship  
**Files**: `tests/NorthstarET.Lms.Domain.Tests/Entities/GuardianTests.cs`  
**Test Cases**: Guardian creation, relationship types, primary guardian rules, pickup authorization

---

### T030 [P]: Unit tests for Enrollment entity
**Description**: Write unit tests for class enrollment and withdrawal  
**Files**: `tests/NorthstarET.Lms.Domain.Tests/Entities/EnrollmentTests.cs`  
**Test Cases**: Enrollment creation, status transitions, withdrawal reasons

---

### T031 [P]: Unit tests for RoleDefinition and RoleAssignment
**Description**: Write unit tests for RBAC domain entities  
**Files**: `tests/NorthstarET.Lms.Domain.Tests/Entities/RoleTests.cs`  
**Test Cases**: Role creation, permission validation, scope constraints, delegation rules

---

### T032 [P]: Unit tests for AuditRecord entity
**Description**: Write unit tests for audit logging and chain integrity  
**Files**: `tests/NorthstarET.Lms.Domain.Tests/Entities/AuditRecordTests.cs`  
**Test Cases**: Record creation, hash calculation, chain validation, sequence ordering

---

### T033 [P]: Unit tests for AssessmentDefinition entity
**Description**: Write unit tests for assessment file management  
**Files**: `tests/NorthstarET.Lms.Domain.Tests/Entities/AssessmentDefinitionTests.cs`  
**Test Cases**: Assessment upload, versioning, immutability enforcement, school year pinning

---

### T034: Run all BDD tests to verify RED phase
**Description**: Execute all Reqnroll tests to confirm they fail before implementation  
**Commands**:
```bash
dotnet test tests/NorthstarET.Lms.Api.Tests --logger "console;verbosity=detailed"
```
**Dependencies**: T017-T024  
**Expected Result**: All BDD scenarios marked as pending or failing

---

### T035: Run all unit tests to verify RED phase
**Description**: Execute all domain unit tests to confirm failures before implementation  
**Commands**:
```bash
dotnet test tests/NorthstarET.Lms.Domain.Tests --logger "console;verbosity=detailed"
```
**Dependencies**: T025-T033  
**Expected Result**: All tests fail (no implementation yet)

---

## Phase 3.3: Clean Architecture Implementation (ONLY after BDD features and tests are failing)

**All implementations MUST follow Clean Architecture layers with proper dependency flow**  
**Domain layer MUST have zero external dependencies**

---

### T036 [P]: Implement base domain entity classes and interfaces
**Description**: Create TenantScopedEntity base class and core domain interfaces  
**Files**: `src/NorthstarET.Lms.Domain/Common/TenantScopedEntity.cs`, `src/NorthstarET.Lms.Domain/Common/IDomainEvent.cs`  
**Dependencies**: T035 (tests RED)

---

### T037 [P]: Implement domain value objects
**Description**: Create value objects: DistrictQuotas, UserId, ExternalId, SecurityAlert  
**Files**: `src/NorthstarET.Lms.Domain/ValueObjects/*.cs`  
**Dependencies**: T035 (tests RED)

---

### T038 [P]: Implement domain enums
**Description**: Create all enums: DistrictStatus, UserLifecycleStatus, EnrollmentStatus, GradeLevel, RoleScope, etc.  
**Files**: `src/NorthstarET.Lms.Domain/Enums/*.cs`  
**Dependencies**: T035 (tests RED)

---

### T039 [P]: Implement DistrictTenant aggregate root
**Description**: Implement DistrictTenant entity with creation, suspension, quotas management  
**Files**: `src/NorthstarET.Lms.Domain/Entities/DistrictTenant.cs`  
**Dependencies**: T036, T037, T038, T025 (unit tests RED)  
**Validation**: Run T025 tests - should now PASS (GREEN phase)

---

### T040 [P]: Implement SchoolYear and AcademicCalendar entities
**Description**: Implement school year lifecycle, term and closure management  
**Files**: `src/NorthstarET.Lms.Domain/Entities/SchoolYear.cs`, `src/NorthstarET.Lms.Domain/Entities/AcademicCalendar.cs`, etc.  
**Dependencies**: T036, T038, T026 (unit tests RED)  
**Validation**: Run T026 tests - should now PASS

---

### T041-T050: [P] Implement Remaining Domain Entities
**Description**: Implement School, Class, Student, Staff, Guardian, Enrollment, RBAC entities, AuditRecord, AssessmentDefinition, RetentionPolicy, LegalHold, IdentityMapping  
**Files**: Multiple files in `src/NorthstarET.Lms.Domain/Entities/`  
**Dependencies**: T036-T038  
**Validation**: Run corresponding unit tests

---

### T051 [P]: Implement domain events
**Description**: Create domain events for audit trail: DistrictProvisionedEvent, StudentCreatedEvent, etc.  
**Files**: `src/NorthstarET.Lms.Domain/Events/*.cs`  
**Dependencies**: T036

---

### T052 [P]: Implement domain service interfaces
**Description**: Define domain service contracts  
**Files**: `src/NorthstarET.Lms.Domain/Services/*.cs`  
**Dependencies**: T036

---

### T053 [P]: Implement Application DTOs
**Description**: Create all Data Transfer Objects for API requests/responses  
**Files**: `src/NorthstarET.Lms.Application/DTOs/**/*.cs`  
**Dependencies**: T038

---

### T054 [P]: Implement Application validators
**Description**: Create FluentValidation validators for all DTOs  
**Files**: `src/NorthstarET.Lms.Application/Validators/*.cs`  
**Dependencies**: T053

---

### T055 [P]: Implement Application repository interfaces
**Description**: Define repository contracts for all aggregates  
**Files**: `src/NorthstarET.Lms.Application/Interfaces/*.cs`  
**Dependencies**: T039-T050

---

### T056-T063: Implement Application Use Cases
**Description**: Create MediatR commands/queries for District, Student, SchoolYear, RBAC, BulkOperations, Assessments, Compliance, Identity management  
**Files**: `src/NorthstarET.Lms.Application/UseCases/**/*.cs`  
**Dependencies**: Domain entities, DTOs, validators, repository interfaces  
**Validation**: BDD step definitions should start passing

---

### T064: Implement TenantDbContext with EF Core configurations
**Description**: Create DbContext with multi-tenant schema support and all entity configurations  
**Files**: `src/NorthstarET.Lms.Infrastructure/Data/LmsDbContext.cs`, configurations  
**Dependencies**: T039-T050

---

### T065: Create initial EF Core migration
**Description**: Generate and apply InitialCreate migration  
**Commands**:
```bash
dotnet ef migrations add InitialCreate --startup-project ../NorthstarET.Lms.Api --context LmsDbContext
```
**Dependencies**: T064

---

### T066-T072: [P] Implement Infrastructure Services
**Description**: Tenant context middleware, repositories, audit chain service, Entra identity service, assessment file service, background services  
**Files**: Multiple files in `src/NorthstarET.Lms.Infrastructure/`  
**Dependencies**: T064

---

### T073-T078: Implement API Controllers
**Description**: Create REST API controllers for Districts, Students, Schools, Classes, Assessments, Audit  
**Files**: `src/NorthstarET.Lms.Api/Controllers/*.cs`  
**Dependencies**: Application use cases

---

### T079-T081: Configure Authentication, Authorization, and Dependency Injection
**Description**: JWT Bearer auth, RBAC authorization handlers, service registration  
**Files**: `src/NorthstarET.Lms.Api/Program.cs`, authorization handlers  
**Dependencies**: Previous implementations

---

## Phase 3.4: Integration & Aspire Orchestration

---

### T082-T090: Configure Aspire and API Infrastructure
**Description**: Service discovery, SQL Server, Redis, logging, health checks, rate limiting, OpenAPI docs  
**Files**: AppHost Program.cs, middleware, configurations  
**Dependencies**: T005, infrastructure implementations

---

### T091-T092: Create Test Fixtures
**Description**: Integration test fixtures with Testcontainers and WebApplicationFactory  
**Files**: Test fixture files  
**Dependencies**: T003, infrastructure

---

### T093: Run full BDD test suite (GREEN phase)
**Description**: Execute all Reqnroll scenarios against running system  
**Commands**:
```bash
dotnet test tests/NorthstarET.Lms.Api.Tests --logger "console;verbosity=detailed"
```
**Dependencies**: All previous implementation tasks  
**Expected Result**: All BDD scenarios PASSING

---

## Phase 3.5: Polish & Quality Gates

---

### T094-T103: [P] Additional Testing and Quality Checks
**Description**: Additional unit tests, integration tests, performance tests, security tests, static analysis, code coverage, nullable checks  
**Files**: Various test files  
**Target**: >90% coverage, <200ms p95 for APIs, <120s for bulk ops

---

### T104-T106: [P] Documentation Updates
**Description**: OpenAPI examples, migration docs, deployment guide  
**Files**: Documentation files and XML comments

---

### T107-T110: Final Quality Gates
**Description**: Vulnerability scanning, final BDD acceptance, code review and refactoring, README update  
**Dependencies**: All implementations complete

---

## Parallel Execution Examples

### BDD Feature Files (T006-T016)
```bash
# All can run in parallel - different files
Task: "Feature file for District provisioning" &
Task: "Feature file for District quota management" &
Task: "Feature file for Student CRUD operations" &
Task: "Feature file for Student enrollment" &
Task: "Feature file for Student grade progression" &
Task: "Feature file for School year and calendar" &
Task: "Feature file for RBAC" &
Task: "Feature file for Bulk operations" &
Task: "Feature file for Assessment files" &
Task: "Feature file for Audit and compliance" &
Task: "Feature file for Identity mapping"
wait
```

### Domain Entities (T039-T050)
```bash
# All can run in parallel - different entity files
Task: "Implement DistrictTenant entity" &
Task: "Implement SchoolYear entity" &
Task: "Implement Student entity" &
Task: "Implement Staff entity" &
Task: "Implement Guardian entities" &
Task: "Implement Enrollment entity" &
Task: "Implement RBAC entities" &
Task: "Implement AuditRecord entity" &
Task: "Implement AssessmentDefinition entity"
wait
```

---

## Validation Checklist

**GATE: All items must be checked before implementation complete**

### Design Coverage
- [x] All contracts have feature files
- [x] All entities have domain implementations
- [x] All use cases have application implementations

### Testing Requirements
- [ ] All BDD feature files have step definitions
- [ ] All domain entities have unit tests (>90% coverage)
- [ ] All application use cases have unit tests (>90% coverage)
- [ ] Performance tests validate SLA requirements

### Architecture Compliance
- [ ] Domain layer has zero external dependencies
- [ ] Clean Architecture dependency direction enforced
- [ ] Tenant data properly isolated (schema-per-tenant)
- [ ] RBAC deny-by-default enforced
- [ ] Audit trail for all mutations

### Quality Gates
- [ ] BDD scenarios all PASSING
- [ ] Unit tests >90% coverage
- [ ] Performance tests meet SLA
- [ ] Security tests validate isolation
- [ ] Static analysis with zero warnings
- [ ] No vulnerable dependencies

---

**Implementation Ready!** 🚀  
**Total Tasks**: 110 tasks covering all 54 functional requirements with complete BDD coverage, Clean Architecture implementation, and comprehensive quality gates.
