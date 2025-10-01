# 01 — Business Requirements (LMS)

## A.0 Scope & non‑goals

**In‑scope (LMS as roster authority):** Tenants (districts), schools, classes, teachers, students, enrollments; query surfaces optimized for consumers; roster events for downstream services.

**Out of scope:** Assessment definitions/instances/scoring, content authoring, payments/billing, identity provider. (Assessment consumes LMS events and exposes its own APIs.)

## A.1 Roles

- **Platform Admin** (Northstar ops, system‑wide control plane access)
- **District Admin** (tenant provisioning & configuration within a district)
- **School Admin** (manages schools, classes within their school)
- **Registrar** (roster data entry at school/district)
- **Teacher** (read class rosters; limited enrollment actions if allowed)
- **Auditor/Support** (read‑only, audit trails)

## A.2 Business objectives

- Single source of truth for roster with low coupling to consumers (event‑first, coarse queries).
- Predictable SLOs for reads/writes; short eventual consistency windows.
- Hard‑tenant isolation per district and strong PII discipline.

## A.3 Requirements (MoSCoW, with acceptance criteria)

IDs are stable for traceability (e.g., LMS‑BR‑012). Metrics are targets to calibrate later.

### Tenant & topology

**LMS‑BR‑001 (MUST) Provision a new district tenant.**
AC: Platform Admin can POST /v1/tenants with minimal metadata; within < 10 s p95 tenant is active with isolated data plane; audit event recorded.

**LMS‑BR‑002 (MUST) Read tenant metadata/status.**
AC: GET /v1/tenants/{tenantId} returns status, createdAt, region, capabilities.

**LMS‑BR‑003 (SHOULD) Suspend/reactivate tenant.**
AC: Suspended tenants block writes; reads are allowed unless configured otherwise; all attempts are audited.

### Schools, classes, people, enrollments

**LMS‑BR‑010 (MUST) CRUD Schools.**
AC: Create/list/get/update/archive; archived schools cannot receive new classes. p95 read < 300 ms, write < 800 ms.

**LMS‑BR‑020 (MUST) CRUD Classes.**
AC: Create/list/get/update/archive; class has name, term, subject, schoolId, externalRefs[].

**LMS‑BR‑030 (MUST) CRUD Students.**
AC: Create/update/deactivate; PII fields minimal & redactable; hard delete is policy‑guarded and rare.

**LMS‑BR‑040 (MUST) CRUD Teachers.**
AC: Create/update/deactivate; may carry certification metadata; PII minimized.

**LMS‑BR‑050 (MUST) Manage Enrollments (Student↔Class M:N).**
AC: Add/remove enrollment with effective dates; idempotent; emits events (EnrollmentAdded/Removed).

**LMS‑BR‑060 (MUST) Assign Teachers to Classes.**
AC: Add/remove teacher assignments; emits TeacherAssignedToClass (and corresponding removal event).

### Bulk ops & lookup

**LMS‑BR‑070 (MUST) Bulk upsert Students/Enrollments.**
AC: Accepts CSV/JSON up to 50k rows per request, chunked server‑side; provides job status; p95 job completion for 50k rows < 10 min; emits per‑row results summary.

**LMS‑BR‑075 (SHOULD) Bulk export roster snapshots (per school/class).**
AC: CSV/JSON export within 60 s for classes up to 5k enrollments.

### Queries (coarse‑grained)

**LMS‑BR‑080 (MUST) Get class membership in one call.**
AC: GET /v1/roster/classes/{classId}/members?include=students,teachers returns both teachers + students; p95 < 300 ms.

**LMS‑BR‑085 (MUST) Batch student lookup.**
AC: POST /v1/lookup/students:batch accepts up to 5k IDs; returns summaries.

**LMS‑BR‑090 (COULD) Flexible search (name, externalRef, grade).**
AC: GET /v1/search/students?query=; limits, pagination.

### Events & consistency

**LMS‑BR‑100 (MUST) Publish roster domain events for all state changes (Outbox).**
AC: Events available on lms.roster.v1.* topics within < 5 s p95 of commit; strictly once‑per‑effect semantics to consumers.

**LMS‑BR‑105 (MUST) Consumers should operate without synchronous chaining.**
AC: Contract discourages multi‑hop reads; coarse queries provided for gaps.

### Security, tenancy, audit, compliance

**LMS‑BR‑110 (MUST) Enforce hard tenant isolation.**
AC: Every request/event contains tenantId; cross‑tenant access impossible; audit logged.

**LMS‑BR‑115 (MUST) PII minimization in events.**
AC: Events publish keys + minimal descriptors only; detailed PII accessible only via authorized queries.

**LMS‑BR‑120 (MUST) Full audit trail.**
AC: Who/what/when/why for all writes; immutable store; traceable via correlation IDs.

**LMS‑BR‑125 (SHOULD) Policy‑driven data retention & right‑to‑erasure (where lawful).**
AC: Soft delete + scrubbing pipelines; legal hold supported.

### Reliability & observability

**LMS‑BR‑130 (MUST) SLOs: Reads p95 ≤ 300 ms, Writes p95 ≤ 800 ms, Event publish p95 ≤ 5 s, Weekly availability ≥ 99.9%.**
(Targets; refine with telemetry.)

**LMS‑BR‑135 (MUST) Comprehensive telemetry.**
AC: Traces, metrics, logs with Correlation/Causation IDs via OpenTelemetry; dashboards per tenant.

## A.4 Primary user journeys (happy path + key alternates)

Journeys are intentionally UI‑agnostic; assume a BFF sits in front of LMS.

### LMS‑UJ‑001 — Provision a tenant (Platform Admin)

1. Admin submits district metadata.
2. LMS allocates isolated data plane, seeds system school/roles, emits TenantProvisioned.
3. Admin receives endpoint & tenantId; can now add schools.

**Alternate:** If region quota exceeded → return 409 with retry‑after; no partial tenant visible; no events published.

### LMS‑UJ‑010 — Create school (District Admin)

1. Admin selects tenant, posts new school.
2. LMS validates name uniqueness per tenant; creates school; emits SchoolCreated.
3. UI lands on school detail with autogenerated externalRef slot.

**Alternate:** Duplicate name → 409 with conflictId and existing school link.

### LMS‑UJ‑020 — Create class & assign teacher (School Admin)

1. Admin creates class in a school.
2. LMS persists class; emits ClassCreated.
3. Admin assigns teacher; LMS emits TeacherAssignedToClass.

**Alternate:** Teacher not in tenant → 422 validation with problem details.

### LMS‑UJ‑030 — Enroll students (Registrar)

1. Registrar uploads CSV of student IDs & class IDs (or posts JSON batch).
2. LMS validates rows; creates students if needed (optional setting), upserts enrollments; emits StudentCreated, EnrollmentAdded.
3. Registrar sees job status and per‑row results.

**Alternate:** Mixed valid/invalid rows → partial success; job report available; failed rows retriable with idempotency keys.

### LMS‑UJ‑040 — Teacher views roster (Teacher)

1. Teacher requests class members.
2. LMS returns one payload including students & teachers (no N+1).

**Alternate:** Teacher lacks permission → 403 with audit entry.

### LMS‑UJ‑050 — Deactivate student (Registrar)

1. Registrar deactivates a student.
2. LMS flags student inactive; emits StudentDeactivated; future enrollments blocked.

**Alternate:** Student currently in active assessment (FYI from Assessment via its read model) → still allowed; LMS is not orchestrator; notice included in response.
