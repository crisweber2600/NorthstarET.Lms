# 01 — Business Requirements (LMS)

## Scope
The LMS is the authoritative system for **District, School, Class, Teacher, Student, Enrollment**. It publishes roster events for downstream services; provides coarse read surfaces; avoids synchronous call chains.

## Roles
Platform Admin, District Admin, School Admin, Registrar, Teacher, Auditor (read-only).

## Requirements (MoSCoW)
- **MUST** Provision tenant; read tenant status.
- **MUST** CRUD Schools, Classes, Students, Teachers; manage Enrollments and Teacher↔Class assignments.
- **MUST** Publish events for all state changes (Outbox); publish within 5s p95 of commit.
- **MUST** Enforce hard tenancy; PII minimized in events; full audit trail.
- **MUST** Coarse queries to avoid N+1: class members (students + teachers), batch student lookup.
- **SHOULD** Bulk upsert (≤ 50k rows), bulk export.
- **SHOULD** Suspend/reactivate tenant.
- **COULD** Flexible search by name/externalRef/grade.

## SLOs (initial targets)
Reads p95 ≤ 300ms; Writes p95 ≤ 800ms; Event publish p95 ≤ 5s; Availability ≥ 99.9%.
