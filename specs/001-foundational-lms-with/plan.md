# Implementation Plan: Foundational LMS with Tenant Isolation and Compliance

**Branch**: `001-foundational-lms-with` | **Date**: 2025-09-29 | **Spec**: [`specs/001-foundational-lms-with/spec.md`](../001-foundational-lms-with/spec.md)
**Input**: Feature specification from `specs/001-foundational-lms-with/spec.md`

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

Deliver a multi-tenant K-12 LMS that enforces per-district isolation, FERPA-aligned retention, comprehensive RBAC, and auditable operations using a .NET 9 Clean Architecture stack orchestrated through .NET Aspire. The system exposes REST APIs for district provisioning, identity lifecycle, academic calendars, role assignments, enrollments, and assessments while guaranteeing audit traceability and performance SLAs.

## Technical Context

**Language/Version**: C# 13 / .NET 9.0  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core 9 (SQL Server provider), .NET Aspire, MediatR, Reqnroll, xUnit, FluentAssertions, Testcontainers  
**Storage**: SQL Server 2022 with per-district schemas and blob storage for assessment PDFs  
**Testing**: Reqnroll (BDD), xUnit (unit/integration), FluentAssertions, Testcontainers, Playwright (contract smoke)  
**Target Platform**: Containerized Linux workloads orchestrated via .NET Aspire  
**Project Type**: Single solution (backend-focused Clean Architecture with layered projects)  
**Performance Goals**: CRUD 95th percentile <200ms; bulk ops (10k rows) <120s; audit queries <2s @ 1M rows  
**Constraints**: Tenant isolation, FERPA retention (Students 7y, Staff 5y, Assessments 3y), 100MB max PDF, 10GB district storage, deny-by-default RBAC, tamper-evident audits  
**Scale/Scope**: 1-5 platform admins, up to 200 districts, 50k students & 5k staff per district, peak 500 concurrent staff actions

## Constitution Check

_GATE: Must pass before Phase 0 research. Re-check after Phase 1 design._

**BDD-First Testing**: Every functional requirement will be expressed as Reqnroll feature files before any implementation. Step definitions will be added to the BDD test project and intentionally assert `PendingStep` until domain/application logic exists, guaranteeing a failing red state.

**TDD Red-Green Cycle**: Unit and integration projects will receive failing tests (xUnit + FluentAssertions) derived from BDD scenarios prior to coding. Coverage dashboards will guard >90% in Domain/Application and refactoring steps are limited to green builds only.

**Clean Architecture**: Planned solution splits Domain, Application, Infrastructure, and Presentation projects with compile-time enforcement via project references. Domain remains dependency-free; application depends only on domain; infrastructure depends on application for abstractions; presentation depends on infrastructure.

**Aspire Orchestration**: Aspire AppHost will coordinate API, worker jobs, SQL Server, and blob emulator. Service discovery, configuration, and health checks leverage Aspire components (`SqlServerServerResource`, `Aspire.Hosting`).

**Feature Specification Completeness**: The provided spec includes clarified requirements, acceptance scenarios, edge cases, and constraints. No outstanding clarifications remain, so planning proceeds under approved scope.

## Project Structure

src/

### Documentation (this feature)

```
specs/001-foundational-lms-with/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)

```

├── Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/
│   └── Services/
├── Application/
│   ├── Abstractions/
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── Validators/
├── Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   └── Migrations/
│   ├── Identity/
│   ├── Files/
│   ├── Audit/
│   └── BackgroundJobs/
└── Presentation/
      ├── Api/
      │   ├── Controllers/
      │   ├── Contracts/
      │   └── Filters/
      ├── Aspire/
      │   └── AppHost/
      └── CompositionRoot/

tests/
├── Domain/
├── Application/
├── Infrastructure/
├── Presentation/
└── Bdd/
      ├── Features/
      └── StepDefinitions/

build/
└── pipelines/
```

**Structure Decision**: Single .NET solution with Clean Architecture layering; ASP.NET Core APIs, background workers, and supporting jobs orchestrated by a shared Aspire AppHost. Testing projects mirror layer boundaries plus BDD suite for Reqnroll.

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

**Output**: research.md with all NEEDS CLARIFICATION resolved and technology choices justified (multi-tenant EF Core strategy, audit hashing, bulk job orchestration, retention pipeline, Entra External ID integration)

## Phase 1: Design & Contracts

_Prerequisites: research.md complete_

1. **Extract entities from feature spec** → `data-model.md`:

   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:

   - RESTful endpoints grouped by bounded contexts (DistrictProvisioning, IdentityLifecycle, AcademicCalendar, RBAC, Enrollment, Assessments, Compliance)
   - Define request/response schemas with tenant-implicit context
   - Output segmented OpenAPI fragments (`contracts/{context}.openapi.yaml`)

3. **Generate contract tests** from contracts:

   - Create placeholder Reqnroll-ready contract smoke tests in `tests/Bdd/Features` and xUnit contract approval tests in `tests/Presentation`
   - Each test asserts schema presence using `Assert.Fail("Pending implementation")`

4. **Extract test scenarios** from user stories:

   - Translate seven acceptance scenarios into QuickStart flow verifying high-level API interactions
   - Capture bulk rollover, legal hold, and security alert detection as dedicated walkthrough steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/bash/update-agent-context.sh copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/\*, failing tests, quickstart.md, agent-specific file

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

_Fill ONLY if Constitution Check has violations that must be justified_

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --------- | ---------- | ------------------------------------ |
| _None_    | —          | —                                    |

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
- [x] Complexity deviations documented

---

_Based on Constitution v1.0.0 - See `.specify/memory/constitution.md`_
