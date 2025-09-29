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
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD): author tests at the start of each phase, run a full solution build + suite to capture the red state, then plan implementation tasks to return to green
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

- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure

## Phase 3.1: Setup

- [ ] T001 Create project structure per implementation plan
- [ ] T002 Initialize [language] project with [framework] dependencies
- [ ] T003 [P] Configure linting and formatting tools

## Phase 3.2: BDD Features & Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3

**CRITICAL: Constitutional requirement - BDD feature files MUST be complete before any code**
**Tests MUST be written and MUST FAIL before ANY implementation**
**Step definitions MUST be fully implemented (no placeholders) and executed at the start of this phase**
**Record a full solution build and failing suite before moving to implementation tasks**
**Coverage requirement: Minimum 90% for domain and application layers**

- [ ] T004 [P] Feature file for user registration in Features/UserRegistration.feature
- [ ] T005 [P] Feature file for user authentication in Features/UserAuthentication.feature
- [ ] T006 [P] Step definitions for user registration in StepDefinitions/UserRegistrationSteps.cs
- [ ] T007 [P] Step definitions for user authentication in StepDefinitions/UserAuthenticationSteps.cs
- [ ] T008 [P] Unit tests for User domain entity in Tests/Unit/Domain/UserTests.cs
- [ ] T009 [P] Unit tests for UserService application service in Tests/Unit/Application/UserServiceTests.cs
- [ ] T010 [P] Integration tests for user API endpoints in Tests/Integration/UserApiTests.cs

_Gate_: Before proceeding to Phase 3.3, run a full solution build and execute the entire test suite, capturing the failing evidence tied to the tasks above.

## Phase 3.3: Clean Architecture Implementation (ONLY after Phase 3.2 red state is recorded)

**All implementations MUST follow Clean Architecture layers with proper dependency flow**
**Domain layer MUST have zero external dependencies and the phase must end with the full suite back to green**

- [ ] T011 [P] Domain entities and value objects in Domain/Entities/
- [ ] T012 [P] Domain interfaces in Domain/Interfaces/
- [ ] T013 [P] Application use cases in Application/UseCases/
- [ ] T014 [P] Application interfaces in Application/Interfaces/
- [ ] T015 [P] Infrastructure data access in Infrastructure/Data/
- [ ] T016 [P] Infrastructure external services in Infrastructure/Services/
- [ ] T017 [P] API controllers in Presentation/Controllers/
- [ ] T018 [P] Aspire service registration and orchestration in Program.cs
- [ ] T019 Dependency injection configuration with proper layer separation

_Gate_: Before moving to Phase 3.4, run a full solution build and execute the entire suite; only proceed when all tests (including those from earlier phases) return to green.

## Phase 3.4: Integration & Aspire Orchestration

**All integrations MUST use Aspire components and abstractions**
**Structured logging with ILogger and performance monitoring required**

- [ ] T020 Aspire app host configuration for service orchestration
- [ ] T021 Database integration using Aspire components
- [ ] T022 HTTP client configuration using IHttpClientFactory
- [ ] T023 Background services using IHostedService
- [ ] T024 Configuration management with IConfiguration
- [ ] T025 Structured logging implementation with ILogger
- [ ] T026 Health checks configuration using Aspire abstractions
- [ ] T027 Service discovery setup for microservices communication

_Gate_: Before transitioning to Phase 3.5, rerun the full solution build and test suite to confirm the integrations leave the system green.

## Phase 3.5: Polish & Quality Gates

- [ ] T028 [P] Additional unit tests to achieve >90% coverage
- [ ] T029 [P] Performance tests to validate SLA requirements (<200ms p95 for APIs)
- [ ] T030 [P] Security analysis and vulnerability scanning
- [ ] T031 [P] Static analysis and nullable reference types compliance
- [ ] T032 [P] Update API documentation
- [ ] T033 Code quality review and refactoring
- [ ] T034 BDD scenario validation and acceptance testing

## Dependencies

- BDD Features (T004-T005) before step definitions (T006-T007)
- Step definitions and tests (T006-T010) before implementation (T011-T019)
- Domain entities (T011-T012) before application services (T013-T014)
- Application services before infrastructure (T015-T016)
- Infrastructure before presentation (T017)
- Implementation before integration (T020-T027)
- Integration before polish (T028-T034)
- Tasks may only be marked complete after their verifying tests pass and a full solution build succeeds

## Parallel Example

```
# Launch T004-T005 together (different feature files):
Task: "Feature file for user registration in Features/UserRegistration.feature"
Task: "Feature file for user authentication in Features/UserAuthentication.feature"

# Then launch T006-T010 together (different files):
Task: "Step definitions for user registration in StepDefinitions/UserRegistrationSteps.cs"
Task: "Step definitions for user authentication in StepDefinitions/UserAuthenticationSteps.cs"
Task: "Unit tests for User domain entity in Tests/Unit/Domain/UserTests.cs"
Task: "Unit tests for UserService application service in Tests/Unit/Application/UserServiceTests.cs"
```

## Notes

- [P] tasks = different files, no dependencies
- Verify BDD feature files are complete, step definitions are fully implemented, and they fail before implementing
- Run a full solution build and execute the entire suite at the start and end of every phase; record evidence linked to task IDs
- Maintain Clean Architecture layer boundaries
- Use Aspire for all service orchestration
- Commit after each task and include references to the verifying tests in commit notes when applicable
- Do not mark tasks complete until their verifying tests pass and the full suite is green
- Avoid: vague tasks, same file conflicts

## Task Generation Rules

_Applied during main() execution_

1. **From Contracts**:
   - Each contract file → contract test task [P]
   - Each endpoint → implementation task
2. **From Data Model**:
   - Each entity → domain entity creation task [P]
   - Each entity → application service task [P]
   - Relationships → use case implementation tasks
3. **From User Stories**:

   - Each story → BDD feature file [P]
   - Each story → step definitions [P]
   - Integration tests for end-to-end validation

4. **Ordering**:
   - Setup → BDD Features → Step Definitions → Unit Tests → Domain → Application → Infrastructure → Presentation → Integration → Polish
   - Dependencies block parallel execution
   - Clean Architecture layer dependencies must be respected

## Validation Checklist

_GATE: Checked by main() before returning_

- [ ] All user stories have BDD feature files
- [ ] All feature files have corresponding step definitions
- [ ] All entities have domain layer tasks
- [ ] All use cases have application layer tasks
- [ ] BDD features and tests come before implementation
- [ ] Clean Architecture dependencies respected
- [ ] Parallel tasks truly independent
- [ ] Each task specifies exact file path
- [ ] No task modifies same file as another [P] task
- [ ] Each task references verifying test(s)
- [ ] Full solution build/test gates recorded for every phase
