# Phase 0 Research – Foundational LMS

## Decision 1: Tenant-Isolated Persistence Strategy

- **Decision**: Use SQL Server 2022 with per-district schemas managed via EF Core schema translation based on `ITenantContextAccessor`.
- **Rationale**: Matches constitution requirement for tenant isolation without exposing tenant IDs in domain models. SQL Server supports schema-level security, row versioning, and performant cross-schema maintenance. EF Core 9 enables context-level schema substitution through model cache keys.
- **Alternatives Considered**:
  - _Separate databases per tenant_: Strong isolation but multiplies connection pools and complicates Aspire orchestration. Chosen approach balances isolation with maintainability.
  - _Row-level security in a shared schema_: Simpler to manage but violates requirement to avoid tenant IDs in domain entities and increases risk of cross-tenant leaks.

## Decision 2: Audit Trail Integrity

- **Decision**: Implement append-only audit tables with SHA-256 hash chaining (`current_hash = SHA256(previous_hash + payload)`) and store chain tips in PlatformAudit for tamper detection.
- **Rationale**: Meets spec demand for tamper-evident logs and supports platform-level verification. Hash chaining is storage-efficient and easy to validate during compliance exports.
- **Alternatives Considered**:
  - _External blockchain ledger_: Provides immutability but adds infrastructure cost and latency. Overkill for internal compliance scenario.
  - _Database temporal tables only_: Offers change history but no tamper-evident guarantee.

## Decision 3: Bulk Operation Execution Model

- **Decision**: Run bulk imports/rollovers through background worker services coordinated by Aspire. Operations enqueue `BulkJob` aggregates persisted in Infrastructure. Users choose error-handling mode stored alongside job metadata.
- **Rationale**: Keeps API responsive, supports progress tracking, and enables configurable strategies (all-or-nothing, best-effort, threshold). Background jobs can leverage transactional batches and retries.
- **Alternatives Considered**:
  - _Synchronous API processing_: Simple but violates performance constraints and user experience for long-running jobs.
  - _External workflow engine_: Powerful but introduces new dependency and governance overhead.

## Decision 4: Identity Integration with Entra External ID

- **Decision**: Use Microsoft Graph SDK via Infrastructure Identity module to synchronize external identities. Store `IdentityMapping` aggregates referencing issuer and subject, with conflict detection enforcing uniqueness per issuer.
- **Rationale**: Aligns with requirement to map all users to Entra External ID and supports lifecycle events. Graph SDK integrates cleanly with Aspire-managed secrets.
- **Alternatives Considered**:
  - _Custom REST integration_: More control but duplicates Graph SDK capabilities and increases maintenance.
  - _Deferred integration_: Violates specification and delays critical identity mapping.

## Decision 5: Retention & Legal Hold Enforcement

- **Decision**: Schedule retention purge jobs via Aspire background service using Hangfire-compatible abstraction hosted in Infrastructure. Legal holds stored in Domain prevent deletion; purge runs evaluate retention windows and skip held records while auditing decisions.
- **Rationale**: Meets FERPA defaults, legal holds, and audit requirements with reliable scheduling and retries. Hangfire-style abstraction fits .NET ecosystem and can be swapped for hosted service implementation.
- **Alternatives Considered**:
  - _SQL Agent jobs_: Locks solution into SQL Server-only scheduling and complicates container orchestration.
  - _Manual cron scripts_: Harder to monitor and audit within Clean Architecture boundaries.

## Decision 6: Assessment Document Storage

- **Decision**: Store assessment PDFs in Azure Blob Storage emulator locally (Azurite) orchestrated by Aspire; production uses Azure Blob Storage with scoped SAS tokens generated through Infrastructure service validating RBAC.
- **Rationale**: Satisfies requirements for scoped URLs, expiry, and quota enforcement (100MB file / 10GB per district) while keeping blobs outside main database.
- **Alternatives Considered**:
  - _Database-stored BLOBs_: Simplifies transactional consistency but inflates database size and reduces streaming performance.
  - _File system storage_: Non-portable and weak for multi-instance deployments.
