# Phase 0 Research Findings

## Technology Selections

### Decision: ASP.NET Core 8 with C#

- **Rationale**: Aligns with enterprise roster services already standardized on .NET in Northstar programs; strong first-class tooling for OpenAPI-first development, background services, and integration testing.
- **Alternatives Considered**:
  - **NestJS (TypeScript)**: Excellent contracts tooling, but increases cognitive load for teams currently staffed with .NET specialists.
  - **Go (Fiber/Gin)**: Lightweight and fast, yet lacks built-in entity framework + migration story demanded by rapid iteration on tenant schemas.

### Decision: PostgreSQL with per-tenant schema isolation

- **Rationale**: PostgreSQL supports schema-level isolation, rich concurrency controls, and native JSON for flexible external references while keeping operational overhead low.
- **Alternatives Considered**:
  - **Separate database per tenant**: Maximum isolation but inflates operational complexity for 2k tenants.
  - **Row-level security in shared schema**: Simpler migrations, but increases risk of cross-tenant leakage if policies drift.

### Decision: Kafka-backed outbox publisher using Confluent.Kafka client

- **Rationale**: Kafka provides durable ordered streams per topic; pairing with transactional outbox ensures at-least-once delivery and per-tenant ordering via partition key.
- **Alternatives Considered**:
  - **RabbitMQ**: Easier consumption semantics but trades off history retention and partition ordering guarantees.
  - **Azure Service Bus**: Strong DLQ story, yet introduces cloud lock-in; current deployment target is on-prem/self-hosted.

### Decision: Background bulk processor using Hangfire + PostgreSQL job storage

- **Rationale**: Hangfire integrates with ASP.NET Core, supports resumable jobs and per-row status tracking stored in PostgreSQL, matching FR-006 and FR-008 requirements.
- **Alternatives Considered**:
  - **Azure Functions**: Serverless fit, but contradicts hard-isolated tenancy requirement for self-managed districts.
  - **Custom hosted worker**: Maximum control but would require rebuilding scheduling, retry, and dashboarding.

### Decision: Observability stack with OpenTelemetry + Prometheus + Grafana dashboards

- **Rationale**: Constitution mandates OTel; Prometheus and Grafana align with existing platform operations runbooks and support SLO alerts.
- **Alternatives Considered**:
  - **Elastic APM**: Rich tracing, but licensing costs and complex tenancy separation concerns.
  - **Azure Monitor**: Cloud locked; organization currently standardizes on self-hosted monitoring.

## Process Notes

- Clarifications confirmed roster events are at-least-once with per-tenant ordering; Kafka topic partitioning strategy will use `{tenantId}` as key.
- Student bulk upsert flows must accept LMS `studentId` for updates and create new students when absent; Hangfire job payload will enforce this branching logic.
- Compliance guardrails: Roster events remain PII-free; audit trails capture actor and payload fingerprint for every mutation.
