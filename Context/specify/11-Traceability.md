# 11 — Traceability

Business Requirement to API to Event mapping with stable requirement IDs.

| Requirement ID | Business Requirement | HTTP Endpoint(s) | Event(s) |
|---|---|---|---|
| LMS-BR-001 | Provision tenant | POST /v1/tenants | TenantProvisioned |
| LMS-BR-002 | Read tenant metadata/status | GET /v1/tenants/{tenantId} | (query only) |
| LMS-BR-003 | Suspend/reactivate tenant | PATCH /v1/tenants/{tenantId} | TenantSuspended/TenantReactivated |
| LMS-BR-010 | CRUD Schools | POST/GET/PATCH/DELETE /v1/schools | SchoolCreated/Updated/Archived |
| LMS-BR-020 | CRUD Classes | POST/GET/PATCH/DELETE /v1/classes | ClassCreated/Updated/Archived |
| LMS-BR-030 | CRUD Students | POST/GET/PATCH/DELETE /v1/students | StudentCreated/Updated/Deactivated |
| LMS-BR-040 | CRUD Teachers | POST/GET/PATCH/DELETE /v1/teachers | TeacherCreated/Updated/Deactivated |
| LMS-BR-050 | Manage Enrollments | POST/DELETE /v1/enrollments | EnrollmentAdded/Removed |
| LMS-BR-060 | Assign Teachers to Classes | POST/DELETE /v1/classes/{classId}/teachers | TeacherAssignedToClass/TeacherUnassignedFromClass |
| LMS-BR-070 | Bulk upsert Students/Enrollments | POST /v1/bulk/students, POST /v1/bulk/enrollments | StudentCreated/Updated, EnrollmentAdded |
| LMS-BR-075 | Bulk export roster snapshots | GET /v1/export/schools/{schoolId}, GET /v1/export/classes/{classId} | (query only) |
| LMS-BR-080 | Get class membership | GET /v1/roster/classes/{classId}/members | (query only) |
| LMS-BR-085 | Batch student lookup | POST /v1/lookup/students:batch | (query only) |
| LMS-BR-090 | Flexible search | GET /v1/search/students | (query only) |
| LMS-BR-100 | Publish roster domain events | (all write operations) | (all domain events via Outbox) |
| LMS-BR-105 | Avoid synchronous chaining | (coarse query design) | (event-driven architecture) |
| LMS-BR-110 | Enforce tenant isolation | (all endpoints require X-Tenant-Id) | (all events include tenantId) |
| LMS-BR-115 | PII minimization in events | (no PII in event payloads) | (keys + minimal descriptors only) |
| LMS-BR-120 | Full audit trail | (audit logging on all writes) | (correlation/causation IDs) |
| LMS-BR-125 | Data retention & erasure | DELETE /v1/students/{studentId}?hard=true | (policy-driven scrubbing) |
| LMS-BR-130 | SLOs | (performance targets) | (monitoring/alerting) |
| LMS-BR-135 | Comprehensive telemetry | (OpenTelemetry instrumentation) | (traces, metrics, logs) |

## User Journey to Requirement Mapping

| Journey ID | User Journey | Requirements |
|---|---|---|
| LMS-UJ-001 | Provision tenant | LMS-BR-001, LMS-BR-110, LMS-BR-120 |
| LMS-UJ-010 | Create school | LMS-BR-010, LMS-BR-100, LMS-BR-110 |
| LMS-UJ-020 | Create class & assign teacher | LMS-BR-020, LMS-BR-060, LMS-BR-100 |
| LMS-UJ-030 | Enroll students | LMS-BR-030, LMS-BR-050, LMS-BR-070, LMS-BR-100 |
| LMS-UJ-040 | Teacher views roster | LMS-BR-080, LMS-BR-110, LMS-BR-120 |
| LMS-UJ-050 | Deactivate student | LMS-BR-030, LMS-BR-100, LMS-BR-110 |
