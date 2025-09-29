<!--
Sync Impact Report:
- Version change: 1.1.0 → 1.2.0 (minor version bump - strengthened phased testing governance)
- Modified principles: I. BDD-First Testing → I. BDD-First Testing Discipline, II. TDD Red-Green Cycle → II. TDD Phase Gates
- Added principles: VII. Phase-Gated Test Discipline (NON-NEGOTIABLE)
- Added sections: Phased Test Gate Workflow
- Removed sections: None
- Templates requiring updates: ✅ .specify/templates/plan-template.md, ✅ .specify/templates/tasks-template.md, ✅ .github/prompts/implement.prompt.md
- Follow-up TODOs: None
-->

# NorthstarET.Lms Constitution

## Core Principles

### I. BDD-First Testing Discipline (NON-NEGOTIABLE)

Every functional requirement MUST be fully described in Reqnroll feature files with complete scenarios before any code is written. At the beginning of each delivery phase the corresponding feature files and step definitions MUST be authored, MUST be fully implemented (no `PendingStep()` placeholders), and MUST execute into a failing state until their backing implementation is completed. All business logic MUST remain traceable to Given-When-Then scenarios that map directly to user value and to the tasks that fulfil them.

### II. TDD Phase Gates (NON-NEGOTIABLE)

All implementation MUST follow strict, phase-gated Test-Driven Development: at the start of every phase, author the relevant unit, integration, and contract tests that describe the intended behaviour; execute them to confirm they fail; only then implement code to make them pass; finally refactor while maintaining green status. Every clean architecture layer MUST have its tests written at the outset of that layer’s phase, and no implementation task may begin without an associated failing test. Integration and contract tests MUST be executed prior to wiring services and MUST return to passing before the phase can close.

### III. Clean Architecture (NON-NEGOTIABLE)

Application MUST be organized in layers: Domain (entities, value objects), Application (use cases, interfaces), Infrastructure (data access, external services), and Presentation (API controllers, UI). Dependencies MUST point inward only. Domain layer MUST have zero external dependencies. Each layer MUST be independently testable.

### IV. Aspire Orchestration

All services and dependencies MUST be orchestrated using .NET Aspire. Service discovery, configuration, and health checks MUST use Aspire abstractions. Local development environment MUST be fully reproducible through Aspire app host. All external dependencies (databases, message queues, etc.) MUST be managed through Aspire components.

### V. Feature Specification Completeness

Every feature MUST start with a complete specification including user scenarios, acceptance criteria, and edge cases. All requirements MUST be testable and unambiguous. No implementation MUST begin until feature specification is reviewed and approved. Feature files MUST map directly to specification requirements.

### VI. Documentation-First Architecture (NON-NEGOTIABLE)

Every directory MUST contain a README.md file describing its purpose, contents, and relationships. Any time a file is created, modified, or deleted, the corresponding directory's README.md MUST be updated to reflect current state. README files MUST provide architectural context, usage examples, and navigation guidance. Documentation MUST be maintained as code with the same rigor as implementation.

### VII. Phase-Gated Test Discipline (NON-NEGOTIABLE)

Each task MUST reference the specific test or suite that proves it complete. A phase cannot advance, nor can tasks be marked complete, until the entire solution builds successfully and all authored tests pass together. If any test fails, work MUST return to the phase where the failure originated, and additional tests MUST be added at the start of that phase if gaps are discovered before returning to green.

## Technology Standards

### .NET 9 and Modern Practices

- Target framework: .NET 9 with latest language features
- Dependency injection using Microsoft.Extensions.DependencyInjection
- Configuration through IConfiguration with strong typing
- Logging through ILogger with structured logging
- HTTP clients through IHttpClientFactory
- Background services through IHostedService

### Testing Framework Requirements

- Reqnroll for BDD testing with feature files in Features/ directory
- Step definitions organized by domain in StepDefinitions/ directory and fully implemented before implementation begins
- xUnit for unit and integration tests
- FluentAssertions for readable test assertions
- Testcontainers for integration testing with real dependencies
- Aspire testing framework for service orchestration tests

### Code Quality Gates

- All code MUST pass static analysis (nullable reference types enabled)
- Test coverage MUST be >90% for domain and application layers
- Integration tests MUST cover all API endpoints and workflows
- Performance tests MUST validate SLA requirements
- Security analysis MUST pass with zero high-severity findings
- No task may be closed without referencing the proving test(s) and without a successful solution build covering the entire suite

### README Management Requirements

- Every directory with source files MUST have a README.md file
- README files MUST be updated immediately when directory contents change
- README files MUST include: Purpose, Architecture context, File inventory, Usage examples
- README files MUST follow standard template with consistent formatting
- Missing or outdated README files block PR approval

## Development Workflow

### Feature Development Process

1. Create feature specification with complete scenarios.
2. Phase 0 – BDD Authoring Gate: Write Reqnroll feature files and fully implemented step definitions; run full solution build and execute the suite to capture failing steps.
3. Phase 1 – Domain Test Gate: Author domain-layer unit tests for upcoming entities/use cases; execute solution build and confirm new tests fail while existing suites remain green.
4. Phase 2 – Application & Contract Gate: Write application-layer unit tests, contract tests, and integration scaffolds; run full build and ensure failures document the missing behaviour.
5. Phase 3 – Implementation Cycle: Implement domain, application, infrastructure, and presentation code to satisfy authored tests, one phase at a time, rerunning the entire suite (including previously green tests) after each phase to confirm return to green before advancing.
6. Phase 4 – Refine & Refactor: With all suites green, refactor safely while keeping the build green and coverage targets intact.

### Phased Test Gate Workflow

For every phase described above:

- **Author tests first**: Capture intent in the appropriate test suites, link each test to its owning task ID, and commit the red state.
- **Run the entire solution build**: Ensure the solution compiles from a clean state before proceeding.
- **Execute all tests**: Record failing evidence for newly added tests while confirming previously green suites remain unaffected.
- **Implement to green**: Write the minimal code required to make the phase’s tests pass, then re-run the full suite to verify green status before any task or phase is marked complete.
- **Document gating**: Update plan/task artifacts with links to the tests and evidence of the build/test run for auditability.

### Code Review Requirements

- All PRs MUST include feature files and step definitions
- Unit test coverage MUST not decrease
- Architecture dependency rules MUST be validated
- Performance impact MUST be assessed
- Security considerations MUST be documented
- Reviewers MUST verify every task references explicit tests and that the latest solution build plus full suite execution is attached as evidence for the phase being closed

## Governance

This constitution supersedes all other development practices. All code changes MUST comply with these principles. Deviations require explicit justification and approval documented in the PR description.

### Amendment Process

- Minor clarifications: Single maintainer approval + 48h review period
- Major principle changes: Team consensus + migration plan + breaking change protocol
- Emergency exceptions: Document in `CONSTITUTION_EXCEPTIONS.md` with timeline for compliance

### Compliance Verification

All pull requests MUST verify:

- [ ] BDD feature files exist and scenarios pass
- [ ] TDD red-green cycle evidence provided (screenshots/test output) for each phase, including initial failing runs
- [ ] Clean architecture boundaries maintained (dependency analysis)
- [ ] Aspire orchestration properly configured
- [ ] Complete feature specifications linked
- [ ] README files updated for all modified directories
- [ ] Documentation matches current code structure
- [ ] Performance requirements met (<200ms p95 for APIs)
- [ ] Security scan passes with zero high-severity findings
- [ ] Solution build artifacts and full test suite results attached for the phase being marked complete

### Definition of Done

A feature is complete when:

- All BDD scenarios pass in CI/CD
- Test coverage >90% for domain/application layers
- API documentation updated
- Performance benchmarks meet SLA
- Security review completed
- Every task has linked tests and the latest full solution build/test run succeeded

**Version**: 1.2.0 | **Ratified**: 2025-01-09 | **Last Amended**: 2025-09-29
