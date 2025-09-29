# Tasks: Foundational LMS with Tenant Isolation and Compliance

**Input**: Design documents from `specs/001-foundational-lms-with/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/, quickstart.md

## Phase 3.1 Setup

- [ ] T001 Establish Clean Architecture solution skeleton (`NorthstarET.Lms.sln`) with projects under `src/Domain`, `src/Application`, `src/Infrastructure`, `src/Presentation/Api`, `src/Presentation/Aspire/AppHost`, and mirrored test projects in `tests/`.
- [ ] T002 Author `Directory.Build.props` at repo root enabling nullable reference types, strict warnings-as-errors, analyzers, and shared code style aligned with constitution.
- [ ] T003 Configure `Directory.Packages.props` (or per-csproj packages) to reference .NET Aspire, ASP.NET Core Minimal APIs, EF Core 9, MediatR, Microsoft Graph SDK, Hangfire-compatible scheduler, Reqnroll, xUnit, FluentAssertions, Testcontainers, and Playwright.
- [ ] T004 Seed solution-level tooling: add `.editorconfig`, reusable `global.json`, and bootstrap scripts in `build/` to standardize local dev plus CI lint/test hooks.

## Phase 3.2 BDD Features & Tests (Red Phase)

### Feature Files

- [ ] T005 [P] Create `tests/Bdd/Features/DistrictProvisioning.feature` covering district creation and auto-assigned admin rights.
- [ ] T006 [P] Create `tests/Bdd/Features/IdentityLifecycle.feature` covering identity mapping and lifecycle events (join/suspend/reinstate/leave).
- [ ] T007 [P] Create `tests/Bdd/Features/AcademicCalendar.feature` enforcing non-overlapping terms and boundary validation.
- [ ] T008 [P] Create `tests/Bdd/Features/CompositeRoleAuthorization.feature` covering multi-role access resolution.
- [ ] T009 [P] Create `tests/Bdd/Features/BulkStudentRollover.feature` highlighting preview vs commit behavior and strategy selection.
- [ ] T010 [P] Create `tests/Bdd/Features/LegalHoldCompliance.feature` validating retention skip when holds exist.
- [ ] T011 [P] Create `tests/Bdd/Features/SecurityMonitoring.feature` simulating anomaly detection tiers.

### Step Definitions (Failing)

- [ ] T012 [P] Scaffold failing step definitions in `tests/Bdd/StepDefinitions/DistrictProvisioningSteps.cs` using `PendingStep()` placeholders.
- [ ] T013 [P] Scaffold failing step definitions in `tests/Bdd/StepDefinitions/IdentityLifecycleSteps.cs`.
- [ ] T014 [P] Scaffold failing step definitions in `tests/Bdd/StepDefinitions/AcademicCalendarSteps.cs`.
- [ ] T015 [P] Scaffold failing step definitions in `tests/Bdd/StepDefinitions/CompositeRoleAuthorizationSteps.cs`.
- [ ] T016 [P] Scaffold failing step definitions in `tests/Bdd/StepDefinitions/BulkStudentRolloverSteps.cs`.
- [ ] T017 [P] Scaffold failing step definitions in `tests/Bdd/StepDefinitions/LegalHoldComplianceSteps.cs`.
- [ ] T018 [P] Scaffold failing step definitions in `tests/Bdd/StepDefinitions/SecurityMonitoringSteps.cs`.

### Contract Tests (Failing)

- [ ] T019 [P] Add failing approval/contract tests for `contracts/district-provisioning.openapi.yaml` in `tests/Presentation/Contracts.Tests/DistrictProvisioningContractTests.cs`.
- [ ] T020 [P] Add failing contract tests for `contracts/identity-lifecycle.openapi.yaml`.
- [ ] T021 [P] Add failing contract tests for `contracts/academic-calendar.openapi.yaml`.
- [ ] T022 [P] Add failing contract tests for `contracts/rbac.openapi.yaml`.
- [ ] T023 [P] Add failing contract tests for `contracts/enrollment.openapi.yaml`.
- [ ] T024 [P] Add failing contract tests for `contracts/assessments.openapi.yaml`.
- [ ] T025 [P] Add failing contract tests for `contracts/compliance.openapi.yaml`.

### Domain & Integration Test Scaffolds (Failing)

- [ ] T026 [P] Create failing unit tests in `tests/Domain/DistrictTenantTests.cs` covering slug immutability, quota defaults, and lifecycle transitions.
- [ ] T027 [P] Create failing unit tests in `tests/Domain/AcademicCalendarTests.cs` validating non-overlapping terms and closure overrides.
- [ ] T028 [P] Create failing unit tests in `tests/Domain/RbacAggregateTests.cs` for role definitions, assignments, and delegation expiry.
- [ ] T029 [P] Create failing unit tests in `tests/Domain/EnrollmentAggregateTests.cs` for status transitions and capacity enforcement.
- [ ] T030 [P] Create failing unit tests in `tests/Domain/ComplianceEntitiesTests.cs` for retention policy overrides, legal holds, and audit hash chaining.
- [ ] T031 [P] Create failing unit tests in `tests/Domain/BulkJobTests.cs` for preview runs, threshold aborts, and progress updates.
- [ ] T032 [P] Add failing integration scenario `tests/Integration/Quickstart/DistrictProvisioningFlowTests.cs` mirroring quickstart Section 3 & 5 expectations.
- [ ] T033 [P] Add failing integration scenario `tests/Integration/Quickstart/BulkRolloverFlowTests.cs` validating preview + commit paths.
- [ ] T034 [P] Add failing integration scenario `tests/Integration/Quickstart/SecurityMonitoringFlowTests.cs` simulating Tier 2 alert suspension.

## Phase 3.3 Domain & Application Implementation

### Domain Entities & Value Objects

- [ ] T035 Implement shared tenant base abstractions (`TenantScopedEntity`, `TenantSlug`, program/accommodation value objects) in `src/Domain` (sequential dependency for following tasks).
- [ ] T036 [P] Implement `DistrictTenant` aggregate root in `src/Domain/Entities/DistrictTenant.cs` with lifecycle domain events.
- [ ] T037 [P] Implement `SchoolYear` aggregate in `src/Domain/Entities/SchoolYear.cs` with archive semantics.
- [ ] T038 [P] Implement `AcademicCalendar` aggregate/value objects in `src/Domain/Entities/AcademicCalendar.cs` enforcing term rules.
- [ ] T039 [P] Implement `School` entity with status change events in `src/Domain/Entities/School.cs`.
- [ ] T040 [P] Implement `Class` aggregate in `src/Domain/Entities/Class.cs` handling capacity and staff links.
- [ ] T041 [P] Implement `Staff` entity in `src/Domain/Entities/Staff.cs` including employment lifecycle.
- [ ] T042 [P] Implement `Student` entity in `src/Domain/Entities/Student.cs` with program flags and grade tracking.
- [ ] T043 [P] Implement `Guardian` entity and `GuardianLink` value objects in `src/Domain/Entities/Guardian.cs`.
- [ ] T044 [P] Implement `RoleDefinition` aggregate in `src/Domain/Entities/RoleDefinition.cs` with permission versioning.
- [ ] T045 [P] Implement `RoleAssignment` aggregate in `src/Domain/Entities/RoleAssignment.cs` supporting delegation.
- [ ] T046 [P] Implement `Enrollment` entity in `src/Domain/Entities/Enrollment.cs` with history snapshots.
- [ ] T047 [P] Implement `AssessmentDefinition` aggregate in `src/Domain/Entities/AssessmentDefinition.cs` with versioning and size guards.
- [ ] T048 [P] Implement `IdentityMapping` aggregate in `src/Domain/Entities/IdentityMapping.cs` enforcing unique issuer pairs.
- [ ] T049 [P] Implement `RetentionPolicy` aggregate in `src/Domain/Entities/RetentionPolicy.cs`.
- [ ] T050 [P] Implement `LegalHold` aggregate in `src/Domain/Entities/LegalHold.cs`.
- [ ] T051 [P] Implement `AuditRecord` entity with hash chaining helpers in `src/Domain/Entities/AuditRecord.cs`.
- [ ] T052 [P] Implement `BulkJob` aggregate with `BulkJobItem` value object in `src/Domain/Entities/BulkJob.cs`.

### Application Layer (Commands/Queries)

- [ ] T053 [P] Create `CreateDistrictCommand` + handler in `src/Application/Commands/Districts/CreateDistrictCommand.cs` wiring tenant provisioning and auto role assignment.
- [ ] T054 [P] Create `UpdateDistrictStatusCommand` + handler managing retention/legal hold checks in `src/Application/Commands/Districts/UpdateDistrictStatusCommand.cs`.
- [ ] T055 [P] Create `MapIdentityCommand` + handler in `src/Application/Commands/Identity/MapIdentityCommand.cs` handling conflicts.
- [ ] T056 [P] Create `RecordIdentityLifecycleEventCommand` + handler queuing lifecycle events.
- [ ] T057 [P] Create `UpsertAcademicCalendarCommand` + handler validating term overlaps.
- [ ] T058 [P] Create `CreateRoleDefinitionCommand` + handler for RBAC definitions.
- [ ] T059 [P] Create `AssignRoleCommand` + handler with scope validation.
- [ ] T060 [P] Create `DelegateRoleCommand` + handler managing expiry scheduling.
- [ ] T061 [P] Create `EnrollStudentCommand` + handler enforcing capacity and duplicates.
- [ ] T062 [P] Create `StartBulkRolloverCommand` + handler orchestrating bulk job creation with strategy metadata.
- [ ] T063 [P] Create `CreateAssessmentCommand` + handler verifying quotas and immutability.
- [ ] T064 [P] Create `ListAssessmentsQuery` + handler using pagination projection.
- [ ] T065 [P] Create `UploadAssessmentFileCommand` + handler issuing scoped URLs.
- [ ] T066 [P] Create `QueryAuditRecordsQuery` + handler supporting filters.
- [ ] T067 [P] Create `ApplyLegalHoldCommand` + handler enforcing uniqueness.
- [ ] T068 [P] Create `UpsertRetentionPolicyCommand` + handler applying overrides with approval metadata.
- [ ] T069 [P] Create `ProcessSecurityAlertCommand` + handler escalating tiers and suspending identities.

## Phase 3.4 Infrastructure & Integration

- [ ] T070 Implement `TenantContextAccessor` and middleware in `src/Infrastructure/Tenancy/TenantContextAccessor.cs` to resolve schema per request.
- [ ] T071 Implement `LmsDbContext` and factory in `src/Infrastructure/Persistence/LmsDbContext.cs` with schema translation using `ITenantContextAccessor`.
- [ ] T072 Author EF Core configurations in `src/Infrastructure/Persistence/Configurations/` for all domain entities, including indexes and constraints.
- [ ] T073 Add initial EF Core migrations and migration orchestration scripts under `src/Infrastructure/Persistence/Migrations/`.
- [ ] T074 Implement repository/Unit of Work abstractions in `src/Infrastructure/Persistence/Repositories/` aligned with application interfaces.
- [ ] T075 Integrate Microsoft Graph using `GraphClientFactory` in `src/Infrastructure/Identity/` for Entra External ID mapping.
- [ ] T076 Implement blob storage service with quota enforcement in `src/Infrastructure/Files/AssessmentStorageService.cs` using Azure Blob SDK / Azurite.
- [ ] T077 Implement audit writer pipeline in `src/Infrastructure/Audit/AuditTrailService.cs` computing SHA-256 chains and syncing PlatformAudit summaries.
- [ ] T078 Implement retention purge background service in `src/Infrastructure/BackgroundJobs/RetentionPurgeService.cs` honoring legal holds.
- [ ] T079 Implement bulk job worker + progress tracking in `src/Infrastructure/BackgroundJobs/BulkJobProcessor.cs`.
- [ ] T080 Implement security anomaly detection service in `src/Infrastructure/Monitoring/SecurityAlertService.cs` producing tiered alerts and suspension events.

## Phase 3.5 Presentation Layer

- [ ] T081 Implement `DistrictsController` in `src/Presentation/Api/Controllers/DistrictsController.cs` with POST /districts and PATCH /districts/{slug}/status endpoints.
- [ ] T082 Implement `IdentityMappingsController` in `src/Presentation/Api/Controllers/IdentityMappingsController.cs` for identity mapping and lifecycle events.
- [ ] T083 Implement `AcademicCalendarsController` in `src/Presentation/Api/Controllers/AcademicCalendarsController.cs`.
- [ ] T084 Implement `RolesController` in `src/Presentation/Api/Controllers/RolesController.cs` handling role definitions.
- [ ] T085 Implement `RoleAssignmentsController` in `src/Presentation/Api/Controllers/RoleAssignmentsController.cs` handling assignments and delegations.
- [ ] T086 Implement `ClassEnrollmentsController` in `src/Presentation/Api/Controllers/ClassEnrollmentsController.cs` for per-class enrollments.
- [ ] T087 Implement `BulkEnrollmentsController` in `src/Presentation/Api/Controllers/BulkEnrollmentsController.cs` launching rollover jobs.
- [ ] T088 Implement `AssessmentsController` in `src/Presentation/Api/Controllers/AssessmentsController.cs` for create/list endpoints.
- [ ] T089 Implement `AssessmentFilesController` in `src/Presentation/Api/Controllers/AssessmentFilesController.cs` for scoped upload URLs.
- [ ] T090 Implement `ComplianceController` in `src/Presentation/Api/Controllers/ComplianceController.cs` for audit queries.
- [ ] T091 Implement `LegalHoldsController` in `src/Presentation/Api/Controllers/LegalHoldsController.cs`.
- [ ] T092 Implement `RetentionPoliciesController` in `src/Presentation/Api/Controllers/RetentionPoliciesController.cs`.

## Phase 3.6 Aspire & Cross-Cutting Integration

- [ ] T093 Configure Aspire AppHost in `src/Presentation/Aspire/AppHost/AppHost.cs` wiring API, background worker(s), SQL Server, Azurite, and configuration secrets.
- [ ] T094 Register health checks, OpenTelemetry tracing, and structured logging using Aspire instrumentation in `src/Presentation/Api/Program.cs` and `AppHost` resources.
- [ ] T095 Implement onboarding CLI `tools/Admin/AdminCli.csproj` to provision sample tenants per quickstart Section 2.
- [ ] T096 Implement isolation smoke tool `tools/TenantSmoke/TenantSmoke.csproj` validating cross-tenant access denial per quickstart Section 5.
- [ ] T097 Add background scheduling host configuration (Hangfire/worker) with Aspire integration in `src/Presentation/Aspire/AppHost/Workers.cs`.

## Phase 3.7 Polish & Quality Gates

- [ ] T098 [P] Expand automated tests to exceed 90% coverage for Domain/Application projects and document metrics.
- [ ] T099 [P] Author performance tests in `tests/Performance/BulkRolloverPerformanceTests.cs` validating 10k row SLA (<120s).
- [ ] T100 [P] Integrate security scanning (Codacy CLI + trivy) and resolve findings in pipeline scripts.
- [ ] T101 [P] Update documentation: feature README, `specs/001-foundational-lms-with/quickstart.md`, and root `README.md` to reflect implemented architecture.
- [ ] T102 [P] Execute quickstart validation end-to-end (Sections 1-9) and capture evidence in `docs/validation/quickstart-report.md`.

## Dependencies

- T001 → prerequisite for all subsequent development tasks.
- T035 must precede T036-T052.
- Domain tasks (T036-T052) must complete before application handlers (T053-T069).
- Application handlers must complete before infrastructure implementations (T070-T080) which in turn precede presentation controllers (T081-T092).
- Aspire and tooling tasks (T093-T097) depend on infrastructure and presentation completion.
- Polish tasks (T098-T102) require full stack implementation and earlier tests to exist.

## Parallel Execution Examples

```
/task run T005 T006 T007 T008 T009 T010 T011
/task run T012 T013 T014 T015 T016 T017 T018
/task run T019 T020 T021 T022 T023 T024 T025
/task run T036 T037 T038 T039 T040 T041 T042 T043
/task run T058 T059 T060 T061 T062 T063 T064
/task run T098 T099 T100 T101 T102
```
