# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
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
   → Constitution gates: tenant isolation, contract-first artifacts, outbox publishing, observability/audit, lifecycle integrity cascades, SLO validation
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

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure

## Phase 3.1: Setup

- [ ] T001 Create project structure per implementation plan
- [ ] T002 Initialize [language] project with [framework] dependencies
- [ ] T003 [P] Configure linting and formatting tools

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3

**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**

- [ ] T004 [P] Contract test POST /api/v1/tenants/{tenantId}/schools with `X-Tenant-Id` + `Idempotency-Key` headers in tests/contract/test_schools_post.py
- [ ] T005 [P] Contract test GET /api/v1/tenants/{tenantId}/schools/{schoolId} in tests/contract/test_schools_get.py
- [ ] T006 [P] Event contract test `lms.roster.v1.SchoolCreated` in tests/contract/events/test_school_created_event.py
- [ ] T007 [P] Integration test outbox publishes SchoolCreated within 5s p95 and writes audit trail in tests/integration/test_outbox_school_created.py

## Phase 3.3: Core Implementation (ONLY after tests are failing)

- [ ] T008 [P] School aggregate with tenant guard in src/models/school.py
- [ ] T009 [P] SchoolService with tenant-scoped repositories in src/services/school_service.py
- [ ] T010 [P] Transactional outbox writer for roster events in src/services/outbox_publisher.py
- [ ] T011 POST /api/v1/tenants/{tenantId}/schools endpoint with idempotency + audit hooks
- [ ] T012 GET /api/v1/tenants/{tenantId}/schools/{schoolId} endpoint with tenant filter
- [ ] T013 Input validation enforcing `X-Tenant-Id`, correlation IDs, and payload rules
- [ ] T014 Error handling with problem+json responses and audit logging

## Phase 3.4: Integration

- [ ] T015 Connect SchoolService and outbox to the tenant-partitioned database
- [ ] T016 Configure outbox dispatcher worker and retry strategy
- [ ] T017 Wire OpenTelemetry spans/metrics and structured logging for roster operations
- [ ] T018 Update health endpoints to reflect DB, broker, and outbox backlog status

## Phase 3.5: Polish

- [ ] T019 [P] Unit tests for tenant guards and audit logging in tests/unit/test_tenant_and_audit.py
- [ ] T020 Performance regression test for roster SLOs in tests/perf/test_roster_latency.py
- [ ] T021 [P] Update docs/observability.md with metrics dashboards and event consumer impact
- [ ] T022 Remove duplication and document any temporary waivers in risk register
- [ ] T023 Validate telemetry dashboards and run manual smoke checklist

## Dependencies

- Tests (T004-T007) before implementation (T008-T014)
- T008 blocks T009 and T015
- T010 blocks T016
- T017 must complete before T023
- Implementation before polish (T019-T023)

## Parallel Example

```
# Launch T004-T007 together:
Task: "Contract test POST /api/v1/tenants/{tenantId}/schools with headers in tests/contract/test_schools_post.py"
Task: "Contract test GET /api/v1/tenants/{tenantId}/schools/{schoolId} in tests/contract/test_schools_get.py"
Task: "Event contract test lms.roster.v1.SchoolCreated in tests/contract/events/test_school_created_event.py"
Task: "Integration test outbox publishes SchoolCreated in tests/integration/test_outbox_school_created.py"
```

## Notes

- [P] tasks = different files, no dependencies
- Verify tests fail before implementing
- Commit after each task
- Avoid: vague tasks, same file conflicts

## Task Generation Rules

_Applied during main() execution_

1. **From Contracts**:
   - Each contract file → contract test task [P]
   - Each endpoint → implementation task
2. **From Data Model**:
   - Each entity → model creation task [P]
   - Relationships → service layer tasks
3. **From User Stories**:

   - Each story → integration test [P]
   - Quickstart scenarios → validation tasks

4. **Ordering**:
   - Setup → Tests → Models → Services → Endpoints → Polish
   - Dependencies block parallel execution

## Validation Checklist

_GATE: Checked by main() before returning_

- [ ] All contracts have corresponding tests
- [ ] All entities have model tasks
- [ ] All tests come before implementation
- [ ] Parallel tasks truly independent
- [ ] Each task specifies exact file path
- [ ] No task modifies same file as another [P] task
