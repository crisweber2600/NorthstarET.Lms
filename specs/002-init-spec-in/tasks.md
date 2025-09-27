# Tasks: LMS Roster Authority Baseline

**Input**: Design documents from `/specs/002-init-spec-in/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/, quickstart.md

## Execution Flow (main)

```
1. Load plan.md, extract tech stack (ASP.NET Core 8, EF Core, Hangfire, Kafka, OTel) and project structure (src/Api|Application|Domain|Infrastructure|Workers, tests/*).
2. Load supplemental artifacts:
   → data-model.md for entity definitions and invariants
   → contracts/roster-api.v1.yaml for REST surface
   → contracts/events-roster.md for event payloads
   → research.md for tooling decisions (PostgreSQL schemas, Hangfire, Testcontainers)
   → quickstart.md for test commands and smoke flows
3. Generate Phase 3 tasks honoring Constitution gates (tenant isolation, contract-first, outbox, observability, SLO discipline, lifecycle cascades).
4. Enforce task rules:
   → Tests before implementation (must fail first)
   → Different files = mark [P]
   → Same file/shared artifact = sequential (no [P])
5. Output numbered tasks (T001...) with explicit file paths, dependency notes, and parallel execution guidance.
```

## Format: `[ID] [P?] Description`

- **[P]**: Safe to execute in parallel (independent files/resources)
- Include precise file paths (absolute from repo root)
- Note constitutional guardrails explicitly where enforced

## Path Conventions

- API host: `src/Api`
- Domain model: `src/Domain`
- Application layer: `src/Application`
- Infrastructure (EF Core, Kafka, Outbox): `src/Infrastructure`
- Background workers (Hangfire): `src/Workers`
- Tests mirror structure under `tests/`

## Phase 3.1: Setup

- [ ] T001 Create solution and projects per plan (Api, Domain, Application, Infrastructure, Workers, ContractTests, IntegrationTests, DomainTests, Performance) under `src/` and `tests/`, wiring shared solution files.
- [ ] T002 Add baseline package references (ASP.NET Core Minimal APIs, EF Core Npgsql, Hangfire, Confluent.Kafka, OpenTelemetry, FluentValidation, Spectral CLI tooling, xUnit, FluentAssertions, Testcontainers) in relevant `.csproj` files.
- [ ] T003 Configure base settings and infrastructure manifests (`src/Api/appsettings.Development.json`, `src/Workers/appsettings.Development.json`, `infra/docker-compose.yml`) for PostgreSQL schemas, Kafka brokers, Hangfire storage, and constitutional headers (tenant, correlation, causation, idempotency).

## Phase 3.2: Tests First (TDD) ⚠️ MUST FAIL BEFORE IMPLEMENTATION

- [ ] T004 [P] Author tenant provisioning/read contract tests covering `POST /v1/tenants` and `GET /v1/tenants/{tenantId}` in `tests/ContractTests/Tenants/TenantContractTests.cs`.
- [ ] T005 [P] Author school contract tests (create, list, get, update, archive) in `tests/ContractTests/Schools/SchoolContractTests.cs`.
- [ ] T006 [P] Author class contract tests (create, list, get, update, archive) in `tests/ContractTests/Classes/ClassContractTests.cs`.
- [ ] T007 [P] Author student contract tests (create, list, get, update, deactivate) in `tests/ContractTests/Students/StudentContractTests.cs`.
- [ ] T008 [P] Author teacher contract tests (create, list, get, update) in `tests/ContractTests/Teachers/TeacherContractTests.cs`.
- [ ] T009 [P] Author enrollment contract tests (add, remove) in `tests/ContractTests/Enrollments/EnrollmentContractTests.cs`.
- [ ] T010 [P] Author class-teacher assignment contract tests (assign, unassign) in `tests/ContractTests/ClassTeachers/ClassTeacherContractTests.cs`.
- [ ] T011 [P] Author roster read contract tests (`GET /v1/roster/classes/{classId}/members`, `POST /v1/lookup/students:batch`) in `tests/ContractTests/Roster/RosterReadContractTests.cs`.
- [ ] T012 [P] Expand bulk operations contract tests (update `BulkStudentsUpsert` stub and add job status assertions) in `tests/ContractTests/Bulk/BulkContractTests.cs`.
- [ ] T013 [P] Validate roster event payload contracts for all `lms.roster.v1.*` topics in `tests/ContractTests/Events/RosterEventsContractTests.cs` using JSON schema fixtures.
- [ ] T014 [P] Create integration test `TenantProvisioningScenario` validating audit, outbox enqueue, and event emission within 5s p95 in `tests/IntegrationTests/TenantProvisioningTests.cs`.
- [ ] T015 [P] Create integration test `RosterLifecycleScenario` covering school/class creation, teacher assignment, enrollment cascade, and roster query latency in `tests/IntegrationTests/RosterLifecycleTests.cs`.

## Phase 3.3: Core Implementation (only after Phase 3.2 tests exist and fail)

### Domain Modeling

- [ ] T016 [P] Implement `Tenant` aggregate with suspension/reactivation rules in `src/Domain/Tenants/Tenant.cs`.
- [ ] T017 [P] Implement `School` aggregate enforcing tenant uniqueness and archive guard in `src/Domain/Schools/School.cs`.
- [ ] T018 [P] Implement `Class` aggregate managing term/subject metadata in `src/Domain/Classes/Class.cs`.
- [ ] T019 [P] Implement `Student` aggregate with deactivation policy and lifecycle event hooks in `src/Domain/Students/Student.cs`.
- [ ] T020 [P] Implement `Teacher` aggregate with certification metadata in `src/Domain/Teachers/Teacher.cs`.
- [ ] T021 [P] Implement `Enrollment` entity/value enforcing tenant alignment and effective dating in `src/Domain/Enrollments/Enrollment.cs`.
- [ ] T022 [P] Implement `ClassTeacher` entity with role uniqueness in `src/Domain/ClassTeachers/ClassTeacher.cs`.
- [ ] T023 [P] Implement `BulkJob` aggregate for job lifecycle in `src/Domain/Bulk/BulkJob.cs`.
- [ ] T024 [P] Implement `BulkJobResult` entity capturing per-row outcomes in `src/Domain/Bulk/BulkJobResult.cs`.
- [ ] T025 [P] Implement immutable `AuditTrail` record in `src/Domain/Audit/AuditRecord.cs`.

### Application Layer & Services

- [ ] T026 Implement tenant command/query handlers (Create/Get/Suspend/Reactivate) in `src/Application/Tenants` with audit + outbox integration.
- [ ] T027 Implement school command/query handlers (Create/List/Get/Update/Archive) in `src/Application/Schools` enforcing tenant guardrails.
- [ ] T028 Implement class command/query handlers in `src/Application/Classes` including event publishing and archive rules.
- [ ] T029 Implement student command/query handlers in `src/Application/Students` handling upsert semantics and enrollment cascade on deactivation.
- [ ] T030 Implement teacher command/query handlers in `src/Application/Teachers` with external reference validation.
- [ ] T031 Implement enrollment command handlers in `src/Application/Enrollments` ensuring tenant + status invariants.
- [ ] T032 Implement class-teacher assignment orchestrators in `src/Application/ClassTeachers` with audit + event emission.
- [ ] T033 Implement bulk student upsert orchestrator scheduling Hangfire jobs in `src/Application/Bulk/StudentUpsertJobScheduler.cs`.
- [ ] T034 Implement bulk job status query handler in `src/Application/Bulk/BulkJobQueries.cs`.
- [ ] T035 Implement roster read query handlers (class members, batch student lookup) in `src/Application/Roster` with performance budgets.

### Infrastructure & Cross-Cutting

- [ ] T036 Implement EF Core `RosterDbContext` with per-tenant schema strategy and migrations in `src/Infrastructure/Persistence`.
- [ ] T037 Implement repositories for aggregates with multi-tenant filters in `src/Infrastructure/Repositories`.
- [ ] T038 Implement transactional outbox schema + repository in `src/Infrastructure/Outbox` (captures events + headers).
- [ ] T039 Implement Kafka producer + topic configuration in `src/Infrastructure/Messaging/KafkaRosterProducer.cs`.
- [ ] T040 Implement Hangfire workers for outbox dispatch and bulk job execution in `src/Workers` honoring retry/backoff policies.
- [ ] T041 Implement audit logging pipeline persisting `AuditTrail` entries in `src/Infrastructure/Auditing/AuditLogger.cs`.
- [ ] T042 Implement tenant enforcement middleware checking `X-Tenant-Id` and binding scoped tenant context in `src/Api/Middleware/TenantContextMiddleware.cs`.
- [ ] T043 Implement correlation/idempotency middleware capturing headers and enforcing replay safety in `src/Api/Middleware/RequestEnvelopeMiddleware.cs`.
- [ ] T044 Implement OpenTelemetry tracing/metrics/logging setup in `src/Api/Telemetry/TelemetryExtensions.cs` including Kafka + Hangfire instrumentation.
- [ ] T045 Implement health and readiness endpoints exposing DB, Kafka, outbox backlog, Hangfire queues in `src/Api/Diagnostics/HealthEndpoints.cs`.
- [ ] T046 Implement event mapping utilities translating domain events to contract payloads in `src/Application/Events/RosterEventMapper.cs`.

### HTTP Endpoints (Minimal API)

_Sequential per file—do not mark [P] for endpoints sharing the same source._

- [ ] T047 Implement `POST /v1/tenants` endpoint wiring CreateTenant handler in `src/Api/Endpoints/TenantsEndpoints.cs`.
- [ ] T048 Implement `GET /v1/tenants/{tenantId}` endpoint wiring query handler in `src/Api/Endpoints/TenantsEndpoints.cs`.
- [ ] T049 Implement `POST /v1/schools` endpoint in `src/Api/Endpoints/SchoolsEndpoints.cs` with duplicate guard + audit.
- [ ] T050 Implement `GET /v1/schools` endpoint in `src/Api/Endpoints/SchoolsEndpoints.cs` with pagination metadata.
- [ ] T051 Implement `GET /v1/schools/{schoolId}` endpoint in `src/Api/Endpoints/SchoolsEndpoints.cs` with tenant filter.
- [ ] T052 Implement `PATCH /v1/schools/{schoolId}` endpoint in `src/Api/Endpoints/SchoolsEndpoints.cs` supporting partial updates.
- [ ] T053 Implement `DELETE /v1/schools/{schoolId}` archive endpoint in `src/Api/Endpoints/SchoolsEndpoints.cs` returning 204 on success.
- [ ] T054 Implement `POST /v1/classes` endpoint in `src/Api/Endpoints/ClassesEndpoints.cs` enforcing school ownership.
- [ ] T055 Implement `GET /v1/classes` endpoint in `src/Api/Endpoints/ClassesEndpoints.cs` supporting filtering by `schoolId` and pagination.
- [ ] T056 Implement `GET /v1/classes/{classId}` endpoint in `src/Api/Endpoints/ClassesEndpoints.cs`.
- [ ] T057 Implement `PATCH /v1/classes/{classId}` endpoint in `src/Api/Endpoints/ClassesEndpoints.cs`.
- [ ] T058 Implement `DELETE /v1/classes/{classId}` archive endpoint in `src/Api/Endpoints/ClassesEndpoints.cs`.
- [ ] T059 Implement `POST /v1/students` endpoint in `src/Api/Endpoints/StudentsEndpoints.cs` applying minimal PII rules.
- [ ] T060 Implement `GET /v1/students` endpoint in `src/Api/Endpoints/StudentsEndpoints.cs` with pagination.
- [ ] T061 Implement `GET /v1/students/{studentId}` endpoint in `src/Api/Endpoints/StudentsEndpoints.cs`.
- [ ] T062 Implement `PATCH /v1/students/{studentId}` endpoint in `src/Api/Endpoints/StudentsEndpoints.cs` with idempotent merge semantics.
- [ ] T063 Implement `POST /v1/students/{studentId}:deactivate` endpoint in `src/Api/Endpoints/StudentsEndpoints.cs`, triggering enrollment termination workflow.
- [ ] T064 Implement `POST /v1/teachers` endpoint in `src/Api/Endpoints/TeachersEndpoints.cs`.
- [ ] T065 Implement `GET /v1/teachers` endpoint in `src/Api/Endpoints/TeachersEndpoints.cs`.
- [ ] T066 Implement `GET /v1/teachers/{teacherId}` endpoint in `src/Api/Endpoints/TeachersEndpoints.cs`.
- [ ] T067 Implement `PATCH /v1/teachers/{teacherId}` endpoint in `src/Api/Endpoints/TeachersEndpoints.cs`.
- [ ] T068 Implement `POST /v1/enrollments` endpoint in `src/Api/Endpoints/EnrollmentsEndpoints.cs` enforcing active student/class invariants.
- [ ] T069 Implement `DELETE /v1/enrollments` endpoint in `src/Api/Endpoints/EnrollmentsEndpoints.cs` performing effective end-date removal.
- [ ] T070 Implement `POST /v1/classes/{classId}/teachers` endpoint in `src/Api/Endpoints/ClassTeachersEndpoints.cs` assigning teachers.
- [ ] T071 Implement `DELETE /v1/classes/{classId}/teachers/{teacherId}` endpoint in `src/Api/Endpoints/ClassTeachersEndpoints.cs`.
- [ ] T072 Implement `GET /v1/roster/classes/{classId}/members` endpoint in `src/Api/Endpoints/RosterEndpoints.cs` enforcing authorization and ≤300 ms p95 response.
- [ ] T073 Implement `POST /v1/lookup/students:batch` endpoint in `src/Api/Endpoints/RosterEndpoints.cs` returning student summaries for up to 5k IDs.
- [ ] T074 Implement `POST /v1/bulk/students:upsert` endpoint in `src/Api/Endpoints/BulkEndpoints.cs` scheduling Hangfire job and returning `Location` header.
- [ ] T075 Implement `GET /v1/bulk/jobs/{jobId}` endpoint in `src/Api/Endpoints/BulkEndpoints.cs` returning job status payload.

## Phase 3.4: Integration & Cross-Cutting Completion

- [ ] T076 Configure transactional outbox dispatcher pipelines (telemetry, retry, DLQ) in `src/Workers/OutboxDispatcher.cs` ensuring publish latency ≤5s p95.
- [ ] T077 Wire lifecycle cascade for student deactivation to terminate enrollments + emit `EnrollmentRemoved` events in `src/Application/Students/StudentLifecycleService.cs`.
- [ ] T078 Implement tenancy-aware query performance caches for roster reads in `src/Infrastructure/Caching/RosterCache.cs`.
- [ ] T079 Implement Testcontainers fixtures for Postgres + Kafka shared across integration tests in `tests/Infrastructure/TestcontainersFixture.cs`.
- [ ] T080 Implement Spectral + schema lint CI hook in `.github/workflows/contracts.yml` referencing quickstart commands.

## Phase 3.5: Polish & Validation

- [ ] T081 [P] Add domain unit tests for aggregates/invariants in `tests/DomainTests/*` ensuring guardrails (tenant isolation, lifecycle cascades).
- [ ] T082 [P] Add integration tests verifying outbox latency and audit logging in `tests/IntegrationTests/OutboxAuditTests.cs`.
- [ ] T083 [P] Add performance test scenario (bulk upsert 50k rows) in `tests/Performance/BulkUpsertPerformanceTests.cs` meeting ≤10 min p95.
- [ ] T084 [P] Update `docs/observability.md` with telemetry dashboards, metrics, and alerting tied to new instruments.
- [ ] T085 [P] Update `docs/audit.md` (create if missing) documenting audit payload fingerprints and retention plan.
- [ ] T086 [P] Refresh `specs/002-init-spec-in/quickstart.md` smoke steps with final API routes and tooling notes.
- [ ] T087 [P] Run and document final constitutional gate checklist results in `specs/002-init-spec-in/plan.md` Progress Tracking.

## Dependencies

- T001 → T002 → T003 (environment before tests/implementation)
- Tests (T004–T015) must finish before any domain/application/infrastructure task (T016+)
- Domain modeling (T016–T025) unblock corresponding application handlers (T026–T035)
- Application handlers (T026–T035) unblock infrastructure adapters (T036–T046)
- Infrastructure (T036–T046) must be in place before HTTP endpoints (T047–T075)
- Endpoint implementations unblock integration wiring (T076–T080) and polish tasks (T081–T087)
- Lifecycle cascade task T077 depends on T029, T031, T068–T069 being ready

## Parallel Execution Examples

```
# Parallelize independent contract tests to accelerate Phase 3.2
Task: tasks.run T004
Task: tasks.run T005
Task: tasks.run T013
Task: tasks.run T015

# Parallelize domain modeling across separate files
Task: tasks.run T016
Task: tasks.run T018
Task: tasks.run T023
Task: tasks.run T025
```

## Validation Checklist

- [ ] All contracts (REST + events) have corresponding failing tests (T004–T013)
- [ ] Every entity from data-model.md mapped to domain tasks (T016–T025)
- [ ] Every endpoint from roster-api.v1.yaml mapped to an implementation task (T047–T075)
- [ ] Observability, audit, tenancy, outbox, and lifecycle guardrails enforced (T036–T046, T076–T078, T082, T087)
- [ ] Bulk performance target captured (T033, T074, T083)
- [ ] Documentation updated for operators and auditors (T084–T086)
