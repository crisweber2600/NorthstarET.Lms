# Implementation Plan: LMS Roster Authority Baseline

**Branch**: `002-init-spec-in` | **Date**: 2025-09-26 | **Spec**: [`specs/002-init-spec-in/spec.md`](./spec.md)
**Input**: Feature specification from `/specs/002-init-spec-in/spec.md`

## Execution Flow (/plan command scope)

```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:

- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary

Deliver a contract-first LMS roster authority service that provisions isolated district tenants, manages roster CRUD, emits at-least-once events, and supports bulk student upserts with per-row status while honoring constitution guardrails for tenancy, observability, and SLOs.

## Technical Context

**Language/Version**: C# with .NET 8.0
**Primary Dependencies**: ASP.NET Core Minimal APIs, Entity Framework Core, Hangfire, Confluent.Kafka, OpenTelemetry SDK
**Storage**: PostgreSQL 15 (per-tenant schemas) + Hangfire metadata tables
**Testing**: xUnit, FluentAssertions, Testcontainers for Postgres/Kafka, Spectral (OpenAPI), custom schema linter
**Target Platform**: Linux containers (Kubernetes) with CI/CD deploys
**Project Type**: Single backend service (API + background workers)
**Performance Goals**: Reads ≤300 ms p95, Writes ≤800 ms p95, Event publish ≤5 s p95, Bulk upsert ≤10 min p95
**Constraints**: Hard tenant isolation, PII exclusion from events, at-least-once events with per-tenant ordering, audit trail for every mutation
**Scale/Scope**: Up to 2k tenants, 5M enrollments per tenant, bulk jobs up to 50k rows

### Documentation (this feature)

```
specs/002-init-spec-in/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
│   ├── roster-api.v1.yaml
│   ├── events-roster.md
│   └── tests/
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)

```
src/
├── Api/                 # ASP.NET Core Minimal API host
├── Application/         # Use cases, commands, orchestrations
├── Domain/              # Aggregates, entities, value objects
├── Infrastructure/      # EF Core context, repositories, outbox, Kafka producer
└── Workers/             # Hangfire jobs for outbox + bulk processing

tests/
├── ContractTests/       # OpenAPI + Pact provider tests (failing until impl)
├── IntegrationTests/    # Testcontainers-backed API + persistence tests
├── DomainTests/         # Pure domain invariants
└── Performance/         # k6 scripts + assertions

tools/
├── EventSchemaLint/     # CLI to enforce PII-free events
└── PactProvider/        # Shared harness for consumer CDC verification

infra/
├── docker-compose.yml   # Postgres + Kafka + Grafana stack
└── k6/                  # Performance scenarios and thresholds
```

**Structure Decision**: Single backend service with layered architecture; API, Domain, Application, Infrastructure, and Workers folders live under `src/`, while tests mirror layers under `tests/`.
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)

api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]

```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Phase 0: Outline & Research

1. **Extract unknowns from Technical Context** above:

   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:

```

For each unknown in Technical Context:
Task: "Research {unknown} for {feature context}"
For each technology choice:
Task: "Find best practices for {tech} in {domain}"

```

3. **Consolidate findings** in `research.md` using format:
- Decision: [what was chosen]
- Rationale: [why chosen]
- Alternatives considered: [what else evaluated]

**Output**: `research.md` records technology selections (ASP.NET Core 8, PostgreSQL tenancy strategy, Kafka outbox, Hangfire bulk processor, OTel stack) resolving all Technical Context unknowns.

## Phase 1: Design & Contracts

_Prerequisites: research.md complete_

1. **Extract entities from feature spec** → `data-model.md`:

- Entity name, fields, relationships
- Validation rules from requirements
- State transitions if applicable

2. **Generate API contracts** from functional requirements:

- For each user action → endpoint
- Use standard REST/GraphQL patterns
- Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:

- One test file per endpoint
- Assert request/response schemas
- Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:

- Each story → integration test scenario
- Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
- Run `.specify/scripts/bash/update-agent-context.sh copilot`
  **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
- If exists: Add only NEW tech from current plan
- Preserve manual additions between markers
- Update recent changes (keep last 3)
- Keep under 150 lines for token efficiency
- Output to repository root

**Output**: data-model.md, /contracts/\*, failing tests, quickstart.md, agent-specific file
**Output**: `data-model.md`, `contracts/roster-api.v1.yaml`, `contracts/events-roster.md`, failing contract stubs in `contracts/tests/`, `quickstart.md`, and refreshed Copilot agent context.
## Phase 2: Task Planning Approach

_This section describes what the /tasks command will do - DO NOT execute during /plan_

**Task Generation Strategy**:

- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract → contract test task [P]
- Each entity → model creation task [P]
- Each user story → integration test task
- Implementation tasks to make tests pass

**Ordering Strategy**:

- TDD order: Tests before implementation
- Dependency order: Models before services before UI
- Mark [P] for parallel execution (independent files)

**Estimated Output**: 25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation

_These phases are beyond the scope of the /plan command_

**Phase 3**: Task execution (/tasks command creates tasks.md)
**Phase 4**: Implementation (execute tasks.md following constitutional principles)
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking

No deviations from constitutional guardrails identified; table remains empty.

## Progress Tracking

_This checklist is updated during execution flow_

**Phase Status**:

- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [ ] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:

- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [ ] Complexity deviations documented

---

_Based on Constitution v1.0.0 - See `/memory/constitution.md`_
```
