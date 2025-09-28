<!--
Sync Impact Report:
- Version change: 1.0.0 → 1.1.0 (minor version bump - new principle added)
- Modified principles: None
- Added principles: VI. Documentation-First Architecture (NON-NEGOTIABLE)
- Added sections: README Management Requirements, Documentation Synchronization Rules
- Removed sections: None
- Templates requiring updates: ✅ All templates validated for README requirements
- Follow-up TODOs: Generate README files for all directories missing them
-->

# NorthstarET.Lms Constitution

## Core Principles

### I. BDD-First Testing (NON-NEGOTIABLE)
Every functional requirement MUST be fully described in Reqnroll feature files with complete scenarios before any code is written. Step definition files MUST be implemented first and MUST be shown to fail as part of the red-green cycle. All business logic MUST be testable through Given-When-Then scenarios that map directly to user value.

### II. TDD Red-Green Cycle (NON-NEGOTIABLE)
All implementation MUST follow strict Test-Driven Development: Write failing test → Make test pass → Refactor. Every layer of clean architecture MUST have unit tests written before implementation. Integration tests MUST fail before services are connected. Contract tests MUST fail before APIs are implemented.

### III. Clean Architecture (NON-NEGOTIABLE)
Application MUST be organized in layers: Domain (entities, value objects), Application (use cases, interfaces), Infrastructure (data access, external services), and Presentation (API controllers, UI). Dependencies MUST point inward only. Domain layer MUST have zero external dependencies. Each layer MUST be independently testable.

### IV. Aspire Orchestration
All services and dependencies MUST be orchestrated using .NET Aspire. Service discovery, configuration, and health checks MUST use Aspire abstractions. Local development environment MUST be fully reproducible through Aspire app host. All external dependencies (databases, message queues, etc.) MUST be managed through Aspire components.

### V. Feature Specification Completeness
Every feature MUST start with a complete specification including user scenarios, acceptance criteria, and edge cases. All requirements MUST be testable and unambiguous. No implementation MUST begin until feature specification is reviewed and approved. Feature files MUST map directly to specification requirements.

### VI. Documentation-First Architecture (NON-NEGOTIABLE)
Every directory MUST contain a README.md file describing its purpose, contents, and relationships. Any time a file is created, modified, or deleted, the corresponding directory's README.md MUST be updated to reflect current state. README files MUST provide architectural context, usage examples, and navigation guidance. Documentation MUST be maintained as code with the same rigor as implementation.

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
- Step definitions organized by domain in StepDefinitions/ directory
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

### README Management Requirements
- Every directory with source files MUST have a README.md file
- README files MUST be updated immediately when directory contents change
- README files MUST include: Purpose, Architecture context, File inventory, Usage examples
- README files MUST follow standard template with consistent formatting
- Missing or outdated README files block PR approval

## Development Workflow

### Feature Development Process
1. Create feature specification with complete scenarios
2. Write Reqnroll feature files mapping to specification
3. Implement step definitions that fail (red phase)
4. Write unit tests for domain/application layers that fail
5. Implement domain entities and value objects
6. Implement application services and use cases
7. Implement infrastructure services
8. Implement presentation layer
9. All tests pass (green phase)
10. Refactor while maintaining test coverage

### Code Review Requirements
- All PRs MUST include feature files and step definitions
- Unit test coverage MUST not decrease
- Architecture dependency rules MUST be validated
- Performance impact MUST be assessed
- Security considerations MUST be documented

## Governance

This constitution supersedes all other development practices. All code changes MUST comply with these principles. Deviations require explicit justification and approval documented in the PR description.

### Amendment Process
- Minor clarifications: Single maintainer approval + 48h review period
- Major principle changes: Team consensus + migration plan + breaking change protocol
- Emergency exceptions: Document in `CONSTITUTION_EXCEPTIONS.md` with timeline for compliance

### Compliance Verification
All pull requests MUST verify:
- [ ] BDD feature files exist and scenarios pass
- [ ] TDD red-green cycle evidence provided (screenshots/test output)
- [ ] Clean architecture boundaries maintained (dependency analysis)
- [ ] Aspire orchestration properly configured
- [ ] Complete feature specifications linked
- [ ] README files updated for all modified directories
- [ ] Documentation matches current code structure
- [ ] Performance requirements met (<200ms p95 for APIs)
- [ ] Security scan passes with zero high-severity findings

### Definition of Done
A feature is complete when:
- All BDD scenarios pass in CI/CD
- Test coverage >90% for domain/application layers
- API documentation updated
- Performance benchmarks meet SLA
- Security review completed

**Version**: 1.1.0 | **Ratified**: 2025-01-09 | **Last Amended**: 2025-01-09