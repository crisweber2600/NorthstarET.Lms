<!--
Sync Impact Report
Version change: 1.1.0 → 1.2.0
Modified principles:
- Operational Guardrails (refer to new lifecycle principle)
- Lifecycle Integrity Cascades (new core principle)
Added sections:
- Core Principle VI — Lifecycle Integrity Cascades
Removed sections:
- None
Templates requiring updates:
- ✅ .specify/templates/plan-template.md
- ✅ .specify/templates/tasks-template.md
Follow-up TODOs:
- None
-->

# Northstar LMS Roster Authority Constitution

## Core Principles

### I. Tenant Isolation Is Mandatory

- Every inbound or outbound interaction MUST carry `X-Tenant-Id`; requests missing it return HTTP 400 and events without it are rejected.
- Data persistence MUST isolate tenants (separate schema or database) and enforce tenant filters at repository and query boundaries.
- Published events MUST include `tenantId`, exclude PII, and remain tenant-scoped; cross-tenant reads or writes are prohibited.

Rationale: District data separation is the primary business promise and the foundation for compliance, breach containment, and contractual trust.

### II. Contracts Lead Delivery

- OpenAPI documents, event schemas, and CDC contracts MUST be authored or updated before code work begins on the corresponding change.
- CI MUST block merges on breaking contract changes unless a signed migration plan is linked in the pull request.
- Feature plans MUST reference the exact contract artifacts they intend to change and capture downstream consumer impact.

Rationale: The LMS is the roster authority; downstream systems depend on stable contracts, so versioned interfaces must precede implementation.

### III. Event Outbox Is the Source of Truth

- State changes MUST write to the transactional outbox in the same commit, and the outbox publisher MUST deliver events within 5 seconds p95.
- Event payloads MUST remain idempotent and include correlation and causation identifiers for downstream replay safety.
- Failures in publish MUST surface as degraded health until the backlog clears; silent drops are not permitted.

Rationale: Roster consumers rely on timely, reliable notifications; the outbox guarantees durability and traceable delivery.

### IV. Observable and Auditable by Default

- OpenTelemetry traces, metrics, and structured logs MUST wrap every API request, outbox publish, and bulk job with correlation IDs.
- All write operations MUST emit audit records capturing actor, tenant, payload fingerprint, and outcome within the same transaction scope.
- Health endpoints (`/health/live`, `/health/ready`, `/health/degraded`) MUST reflect data store, broker, and outbox status.

Rationale: Without unified telemetry and audit trails, SLO breaches and compliance investigations become guesswork; observability is required for trust.

### V. Test-Driven SLO Discipline

- Work MUST start with executable tests (unit, contract, integration) that fail before implementation and explicitly cover SLO thresholds.
- Every merge MUST confirm p95 targets (reads ≤ 300ms, writes ≤ 800ms, events publish ≤ 5s) either via automated checks or recorded benchmark evidence.
- Performance regressions exceeding 10% of the budget MUST block release until mitigated or jointly waived by Product and Platform leads.

Rationale: The LMS promises reliability and scale; enforcing tests-first kinetics keeps features aligned with performance commitments.

### VI. Lifecycle Integrity Cascades

- Lifecycle state changes (deactivation, archival, reinstatement) MUST execute within a single transactional boundary that also updates dependent aggregates and junction entities.
- Cascading updates MUST emit compensating events (`EnrollmentRemoved`, `TeacherUnassignedFromClass`, etc.) matching the domain impact and preserving downstream ordering guarantees.
- Lifecycle transitions MUST leave an auditable trail with actor, reason, timestamp, and affected entity identifiers so reinstatements or rollbacks are traceable.

Rationale: District rosters are interconnected; lifecycle drift creates data leaks and downstream failures. Cascading policies keep dependent systems in sync and ensure historical accountability.

## Operational Guardrails

- **Security & Tenancy**: OAuth2 JWT authentication with role + scope authorization, mandatory mTLS for service-to-service calls where supported, field-level encryption for sensitive data, and tenant enforcement at every persistence boundary.
- **Data & Events**: Roster entities (District, School, Class, Teacher, Student, Enrollment) MUST store authoritative state; all mutating actions MUST emit corresponding `lms.roster.v1.*` events without PII.
- **SLO Envelope**: Adhere to reads p95 ≤ 300ms, writes p95 ≤ 800ms, bulk upsert (≤ 50k rows) ≤ 10 minutes p95, availability ≥ 99.9%, and event publication p95 ≤ 5 seconds.
- **Resilience**: Bulk operations MUST support retry with idempotency; degraded modes MUST surface via `/health/degraded` and alerting tied to backlog size, broker connectivity, and tenancy violations.
- **Observability Baseline**: Key metrics (`http_server_duration`, `events_published_latency_ms`, `outbox_backlog`, `bulk_job_throughput`) MUST be emitted and alert thresholds maintained in operations runbooks.
- **Lifecycle Integrity Execution**: Implementations MUST satisfy Principle VI by cascading lifecycle changes, emitting compensating events, and persisting audit evidence without manual reconciliation steps.

## Delivery Workflow & Quality Gates

1. **Plan & Contracts First**: Feature work begins with updates to specifications, OpenAPI, and event schemas; CI contract gates must pass before implementation tasks start.
2. **Research & Design**: Constituents document unknowns in `research.md`, resolve clarifications, design data models, and ensure tenant rules and event obligations are explicit.
3. **Tests Before Code**: Contract, unit, integration, and performance probes are authored and executed (failing) before implementation. Test coverage MUST include outbox, audit, and observability touchpoints.
4. **Implementation Sequence**: Apply TDD—repositories and services after models, then APIs; ensure outbox wiring, telemetry, audit hooks, and health reporting are implemented before feature completion.
5. **Readiness Validation**: Definition of Done requires passing CI (lint, tests, contract checks), verifying telemetry dashboards for new signals, updating runbooks if metrics change, and documenting consumer impact.

## Governance

- **Adoption**: This constitution supersedes prior informal practices. All plans, specs, and task lists MUST reference this document and explicitly acknowledge the relevant principles.
- **Amendments**: Proposed changes require an ADR with impact analysis, approval by Product, Architecture, and Platform leads, and a migration plan for affected artifacts. Once merged, update the version per semantic rules.
- **Versioning Policy**: Use semantic versioning—MAJOR for breaking principle changes, MINOR for new principles or major expansions, PATCH for clarifications. Record the version in all dependent templates.
- **Compliance Reviews**: Conduct fortnightly audits of active features to ensure tenancy, contract-first delivery, observability, and SLO evidence are present. Document findings in the decision log.
- **Exceptions**: Temporary deviations MUST include a time-bound waiver signed by Product and Platform leads and tracked in the risk register until resolved.

**Version**: 1.2.0 | **Ratified**: 2025-09-26 | **Last Amended**: 2025-09-26
