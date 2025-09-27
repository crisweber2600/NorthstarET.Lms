# Feature Specification: LMS Roster Authority Baseline

**Feature Branch**: `002-init-spec-in`  
**Created**: 2025-09-26  
**Status**: Draft  
**Input**: User description: "init spec in #file:specify"

## Execution Flow (main)

```
1. Parse user description from Input
   → If empty: ERROR "No feature description provided"
2. Extract key concepts from description
   → Identify: actors, actions, data, constraints
3. For each unclear aspect:
   → Mark with [NEEDS CLARIFICATION: specific question]
4. Fill User Scenarios & Testing section
   → If no clear user flow: ERROR "Cannot determine user scenarios"
5. Generate Functional Requirements
   → Each requirement must be testable
   → Mark ambiguous requirements
6. Identify Key Entities (if data involved)
7. Run Review Checklist
   → If any [NEEDS CLARIFICATION]: WARN "Spec has uncertainties"
   → If implementation details found: ERROR "Remove tech details"
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines

- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, APIs, code structure)
- 👥 Written for business stakeholders, not developers

### Section Requirements

- **Mandatory sections**: Must be completed for every feature
- **Optional sections**: Include only when relevant to the feature
- When a section doesn't apply, remove it entirely (don't leave as "N/A")

### For AI Generation

When creating this spec from a user prompt:

1. **Mark all ambiguities**: Use [NEEDS CLARIFICATION: specific question] for any assumption you'd need to make
2. **Don't guess**: If the prompt doesn't specify something (e.g., "login system" without auth method), mark it
3. **Think like a tester**: Every vague requirement should fail the "testable and unambiguous" checklist item
4. **Common underspecified areas**:
   - User types and permissions
   - Data retention/deletion policies
   - Performance targets and scale
   - Error handling behaviors
   - Integration requirements
   - Security/compliance needs

---

## Clarifications

### Session 2025-09-26

- Q: How should the LMS guarantee delivery semantics for `lms.roster.v1.*` events? → A: At-least-once delivery with per-tenant ordering guarantees
- Q: Which attribute must every student row include so the LMS can deduplicate and idempotently upsert records within a tenant? → A: No natural key; caller must reuse the LMS `studentId`
- Q: How must the LMS handle a bulk student upsert row that doesn’t include an LMS `studentId`? → A: Treat it as a brand-new student create
- Q: When a registrar deactivates a student, how should the LMS treat that student’s currently active class enrollments? → A: Immediately end every active enrollment at the deactivation time

---

## User Scenarios & Testing _(mandatory)_

### Primary User Story

A district registrar uses the LMS to onboard a new school year: they provision the tenant, create schools and classes, load students and teachers, and rely on roster events so Assessment systems stay synchronized without manual coordination.

### Acceptance Scenarios

1. **Given** a Platform Admin has district metadata, **When** they submit a tenant provisioning request, **Then** the LMS activates an isolated tenant within 10 seconds p95, emits a `TenantProvisioned` event, and records audit history.
2. **Given** a School Admin needs a rostered class, **When** they create a school, define classes, assign teachers, and registrar uploads enrollments, **Then** the LMS persists entities, emits corresponding `SchoolCreated`, `ClassCreated`, `TeacherAssignedToClass`, and `EnrollmentAdded` events within 5 seconds p95, and exposes class membership queries under 300 ms p95.

### Edge Cases

- Tenant provisioning is blocked because regional capacity is exhausted → respond with 409 and retry guidance; no partial tenant artifacts exist; audit trail captures the denial.
- Bulk enrollment upload contains a mix of valid and invalid rows → system processes valid rows, marks failures with retriable error codes, preserves idempotency via provided keys, and surfaces a job summary.
- Teacher attempts to view a class outside their tenant → return 403, emit an audit entry, and no cross-tenant data leaks.
- Bulk student upsert row omits `studentId` → treat as new student creation, allocate identifier, record outcome in per-row summary, and ensure replays with the returned `studentId` remain idempotent.
- Student is deactivated while still enrolled in active classes → automatically end each active enrollment effective at the deactivation timestamp, emit corresponding `EnrollmentRemoved` events, and note the outcome in audit logs.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: The LMS MUST provision, read, suspend, and reactivate district tenants with hard isolation, recording audit and telemetry for every action.
- **FR-002**: The LMS MUST support CRUD lifecycle for Schools, Classes, Students, and Teachers with validation on tenant ownership, unique naming per tenant, and status transitions (active, archived, deactivated).
- **FR-003**: The LMS MUST manage Student↔Class enrollments and Teacher↔Class assignments with idempotent add/remove semantics, effective dating, and automatic termination of active enrollments when a student is deactivated.
- **FR-004**: The LMS MUST publish roster domain events (`lms.roster.v1.*`) for every state change via the outbox pattern, delivering within 5 seconds p95, ensuring at-least-once delivery with per-tenant ordering guarantees, and without PII.
- **FR-005**: The LMS MUST expose coarse-grained read surfaces (class membership, batch student lookup, roster exports) meeting p95 ≤ 300 ms for reads and ≤ 60 seconds for exports up to stated limits.
- **FR-006**: The LMS MUST support bulk upsert jobs (≤ 50k rows) with resumable status tracking, per-row result summaries, and completion within 10 minutes p95.
- **FR-007**: The LMS MUST enforce OAuth2-based authentication, role and scope authorization, required headers (`X-Tenant-Id`, correlation/causation IDs, idempotency keys), and block requests missing tenancy context.
- **FR-008**: The LMS MUST require bulk student upsert payloads to reference the LMS-issued `studentId` for existing students and treat rows without a `studentId` as requests to create new students, assigning new identifiers while retaining idempotent behavior for subsequent retries.
- **FR-009**: The LMS MUST maintain comprehensive telemetry (OpenTelemetry traces, key metrics, structured logs) and health endpoints reflecting database, broker, and outbox states.
- **FR-010**: The LMS MUST preserve immutable audit trails for all mutating operations, including actor, tenant, payload fingerprint, and outcome.
- **FR-011**: The LMS SHOULD provide policy-driven retention tooling (soft deletes, scrubbing pipelines, legal hold) to satisfy regulatory obligations.

### Key Entities _(include if feature involves data)_

- **Tenant**: Represents a district; holds isolation metadata (region, status, capabilities) and anchors all subordinate records.
- **School**: Tenant-scoped organization unit; includes identity fields and archival status controlling class creation.
- **Class**: Instructional section linked to a School; stores term, subject, external references, and drives membership queries.
- **Student**: Learner profile within a tenant; supports activation state, minimal PII, and linkage to enrollments.
- **Teacher**: Educator profile within a tenant; captures certification metadata and ties to class assignments.
- **Enrollment**: Join entity for Student↔Class with effective dates and status to support historical rosters.
- **ClassTeacher**: Association between Class and Teacher, ensuring unique pairings and assignment auditability.
- **BulkJob**: Represents bulk import/export operations, tracking submission parameters, per-row results, completion targets, and retry hints.
- **RosterEvent**: Outbox-backed message encapsulating domain changes, correlation identifiers, and tenant context for downstream systems.

---

## Review & Acceptance Checklist

_GATE: Automated checks run during main() execution_

### Content Quality

- [ ] No implementation details (languages, frameworks, APIs)
- [ ] Focused on user value and business needs
- [ ] Written for non-technical stakeholders
- [ ] All mandatory sections completed

### Requirement Completeness

- [ ] No [NEEDS CLARIFICATION] markers remain
- [ ] Requirements are testable and unambiguous
- [ ] Success criteria are measurable
- [ ] Scope is clearly bounded
- [ ] Dependencies and assumptions identified

---

## Execution Status

_Updated by main() during processing_

- [ ] User description parsed
- [ ] Key concepts extracted
- [ ] Ambiguities marked
- [ ] User scenarios defined
- [ ] Requirements generated
- [ ] Entities identified
- [ ] Review checklist passed

---

## Appendix: Context Reference Documents

### 00-Overview.md

```markdown
# LMS — Spec-Kit Outputs (/specify)

**Date:** 2025-09-26

This folder contains the specification artifacts for the **LMS service** prepared using the Spec‑Kit method (Gather → Summarize → Validate → Commit/Reconcile). Artifacts are structured and versioned for CI consumption.

**Contents**

- `01-Business-Requirements.md`
- `02-User-Journeys.md`
- `03-Domain-Model.md`
- `04-HTTP-API-v1.openapi.yml`
- `05-Event-Schemas/` (JSON Schemas, draft 2020-12)
- `06-Security-and-Tenancy.md`
- `07-Error-Model.md`
- `08-Non-Functional-Requirements.md`
- `09-Observability.md`
- `10-Glossary.md`
- `11-Traceability.md`

**Conventions**

- OpenAPI: 3.1.0 with global security and standardized headers.
- Events: topic namespace `lms.roster.v1.*`, envelope + payload per schema.
- All artifacts enforce **tenantId**, **correlationId**, idempotency and **problem+json**.
```

### 01-Business-Requirements.md

```markdown
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
```

### 02-User-Journeys.md

```markdown
# 02 — User Journeys (UI-agnostic)

- **Provision Tenant**: Admin posts metadata → tenant active → `TenantProvisioned` event.
- **Create School**: District Admin creates school → `SchoolCreated` event; duplicates → 409.
- **Create Class & Assign Teacher**: Create class → `ClassCreated`; assign teacher → `TeacherAssignedToClass`.
- **Enroll Students**: Bulk upload → idempotent upserts; per-row results; `StudentCreated`/`EnrollmentAdded` events.
- **View Class Roster**: Teacher fetches members in one call; permission enforced.
- **Deactivate Student**: Registrar deactivates → `StudentDeactivated`; new enrollments blocked.

Alternates cover validation errors, permission denials (403), conflict (409), and partial successes in bulk flows.
```

### 03-Domain-Model.md

```markdown
# 03 — Domain Model (high level)

**Entities**

- Tenant(District), School(tenantId), Class(schoolId), Student(tenantId), Teacher(tenantId), Enrollment(studentId,classId), ClassTeacher(classId,teacherId)

**Invariants**

- Enrollment requires active Student, active Class, same tenant.
- Deactivated Students cannot receive new Enrollments.
- ClassTeacher unique per (classId, teacherId).

**Indexes (illustrative)**

- IX_School_TenantId_Name; IX_Class_SchoolId_Name; IX_Student_TenantId_FamilyName; IX_Enrollment_ClassId; IX_Enrollment_StudentId.
```

### 04-HTTP-API-v1.openapi.yml

```yaml
openapi: 3.1.0
info:
  title: NorthStar LMS API
  version: 1.0.0
  description:
    Authoritative roster service. Coarse-grained queries; tenant-isolated;
    problem+json errors; idempotent POSTs.
servers:
  - url: https://{env}.northstar.local/api
    variables:
      env:
        default: dev
security:
  - bearerAuth: []
components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
  parameters:
    TenantId:
      name: X-Tenant-Id
      in: header
      required: true
      schema:
        type: string
        format: uuid
    CorrelationId:
      name: X-Correlation-Id
      in: header
      required: false
      schema:
        type: string
        format: uuid
    CausationId:
      name: X-Causation-Id
      in: header
      required: false
      schema:
        type: string
        format: uuid
    PageSize:
      name: pageSize
      in: query
      schema:
        type: integer
        minimum: 1
        maximum: 500
      required: false
    NextToken:
      name: nextToken
      in: query
      schema:
        type: string
      required: false
  headers:
    Deprecation:
      schema:
        type: string
    Sunset:
      schema:
        type: string
        format: date-time
    Retry-After:
      schema:
        type: string
  schemas:
    Problem:
      type: object
      properties:
        type:
          type: string
          format: uri
        title:
          type: string
        status:
          type: integer
        detail:
          type: string
        instance:
          type: string
          format: uri
        traceId:
          type: string
        correlationId:
          type: string
      required:
        - title
        - status
    Tenant:
      type: object
      properties:
        tenantId:
          type: string
          format: uuid
        name:
          type: string
        region:
          type: string
        status:
          type: string
      required:
        - tenantId
        - name
        - status
    School:
      type: object
      properties:
        schoolId:
          type: string
          format: uuid
        name:
          type: string
        status:
          type: string
        externalRefs:
          type: object
      required:
        - schoolId
        - name
        - status
    Class:
      type: object
      properties:
        classId:
          type: string
          format: uuid
        schoolId:
          type: string
          format: uuid
        name:
          type: string
        term:
          type: string
        subject:
          type: string
        status:
          type: string
        externalRefs:
          type: object
      required:
        - classId
        - schoolId
        - name
        - status
    StudentSummary:
      type: object
      properties:
        studentId:
          type: string
          format: uuid
        givenName:
          type: string
        familyName:
          type: string
        status:
          type: string
        externalRefs:
          type: object
      required:
        - studentId
        - givenName
        - familyName
        - status
    TeacherSummary:
      type: object
      properties:
        teacherId:
          type: string
          format: uuid
        givenName:
          type: string
        familyName:
          type: string
        status:
          type: string
      required:
        - teacherId
        - givenName
        - familyName
        - status
    EnrollmentRequest:
      type: object
      properties:
        studentId:
          type: string
          format: uuid
        classId:
          type: string
          format: uuid
        effectiveDate:
          type: string
          format: date
      required:
        - studentId
        - classId
    CreateSchoolRequest:
      type: object
      properties:
        name:
          type: string
        externalRefs:
          type: object
      required:
        - name
    CreateClassRequest:
      type: object
      properties:
        schoolId:
          type: string
          format: uuid
        name:
          type: string
        term:
          type: string
        subject:
          type: string
        externalRefs:
          type: object
      required:
        - schoolId
        - name
    CreateStudentRequest:
      type: object
      properties:
        givenName:
          type: string
        familyName:
          type: string
        externalRefs:
          type: object
      required:
        - givenName
        - familyName
    CreateTeacherRequest:
      type: object
      properties:
        givenName:
          type: string
        familyName:
          type: string
        externalRefs:
          type: object
      required:
        - givenName
        - familyName
    MembersResponse:
      type: object
      properties:
        classId:
          type: string
          format: uuid
        teachers:
          type: array
          items:
            $ref: "#/$defs/TeacherSummary"
        students:
          type: array
          items:
            $ref: "#/$defs/StudentSummary"
      required:
        - classId
        - teachers
        - students
  $defs:
    StudentSummary:
      $ref: "#/components/schemas/StudentSummary"
    TeacherSummary:
      $ref: "#/components/schemas/TeacherSummary"
  responses:
    ProblemResponse:
      description: Problem response
      content:
        application/problem+json:
          schema:
            $ref: "#/components/schemas/Problem"
paths:
  /v1/tenants:
    post:
      operationId: CreateTenant
      summary: Provision a new tenant
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                name:
                  type: string
                region:
                  type: string
                externalRefs:
                  type: object
              required:
                - name
      responses:
        "201":
          description: Created
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Tenant"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
  /v1/tenants/{tenantId}:
    get:
      operationId: GetTenant
      summary: Get tenant metadata
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: tenantId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Tenant"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/schools:
    post:
      operationId: CreateSchool
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/CreateSchoolRequest"
      responses:
        "201":
          description: Created
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/School"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
    get:
      operationId: ListSchools
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - $ref: "#/components/parameters/PageSize"
        - $ref: "#/components/parameters/NextToken"
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                type: object
                properties:
                  items:
                    type: array
                    items:
                      $ref: "#/components/schemas/School"
                  nextToken:
                    type: string
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/schools/{schoolId}:
    get:
      operationId: GetSchool
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: schoolId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/School"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
    patch:
      operationId: UpdateSchool
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: schoolId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              additionalProperties: true
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/School"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
    delete:
      operationId: ArchiveSchool
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: schoolId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "204":
          description: Archived
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/classes:
    post:
      operationId: CreateClass
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/CreateClassRequest"
      responses:
        "201":
          description: Created
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Class"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
    get:
      operationId: ListClasses
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - $ref: "#/components/parameters/PageSize"
        - $ref: "#/components/parameters/NextToken"
        - name: schoolId
          in: query
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                type: object
                properties:
                  items:
                    type: array
                    items:
                      $ref: "#/components/schemas/Class"
                  nextToken:
                    type: string
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/classes/{classId}:
    get:
      operationId: GetClass
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: classId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Class"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
    patch:
      operationId: UpdateClass
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: classId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              additionalProperties: true
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Class"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
    delete:
      operationId: ArchiveClass
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: classId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "204":
          description: Archived
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/students:
    post:
      operationId: CreateStudent
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/CreateStudentRequest"
      responses:
        "201":
          description: Created
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/StudentSummary"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
    get:
      operationId: ListStudents
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - $ref: "#/components/parameters/PageSize"
        - $ref: "#/components/parameters/NextToken"
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                type: object
                properties:
                  items:
                    type: array
                    items:
                      $ref: "#/components/schemas/StudentSummary"
                  nextToken:
                    type: string
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/students/{studentId}:
    get:
      operationId: GetStudent
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: studentId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/StudentSummary"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
    patch:
      operationId: UpdateStudent
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: studentId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              additionalProperties: true
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/StudentSummary"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/students/{studentId}:deactivate:
    post:
      operationId: DeactivateStudent
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: studentId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/StudentSummary"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/teachers:
    post:
      operationId: CreateTeacher
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/CreateTeacherRequest"
      responses:
        "201":
          description: Created
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/TeacherSummary"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
    get:
      operationId: ListTeachers
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - $ref: "#/components/parameters/PageSize"
        - $ref: "#/components/parameters/NextToken"
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                type: object
                properties:
                  items:
                    type: array
                    items:
                      $ref: "#/components/schemas/TeacherSummary"
                  nextToken:
                    type: string
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/teachers/{teacherId}:
    get:
      operationId: GetTeacher
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: teacherId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/TeacherSummary"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
    patch:
      operationId: UpdateTeacher
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: teacherId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              additionalProperties: true
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/TeacherSummary"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/enrollments:
    post:
      operationId: AddEnrollment
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/EnrollmentRequest"
      responses:
        "201":
          description: Created
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
    delete:
      operationId: RemoveEnrollment
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/EnrollmentRequest"
      responses:
        "204":
          description: Removed
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
  /v1/classes/{classId}/teachers:
    post:
      operationId: AssignTeacherToClass
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: classId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                teacherId:
                  type: string
                  format: uuid
                role:
                  type: string
              required:
                - teacherId
      responses:
        "201":
          description: Assigned
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/classes/{classId}/teachers/{teacherId}:
    delete:
      operationId: UnassignTeacherFromClass
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: classId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: teacherId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "204":
          description: Unassigned
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/roster/classes/{classId}/members:
    get:
      operationId: GetClassMembers
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: classId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: include
          in: query
          required: false
          schema:
            type: string
            enum:
              - students
              - teachers
              - all
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/MembersResponse"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
  /v1/lookup/students:batch:
    post:
      operationId: BatchStudentLookup
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                ids:
                  type: array
                  items:
                    type: string
                    format: uuid
                  maxItems: 5000
              required:
                - ids
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                type: object
                properties:
                  items:
                    type: array
                    items:
                      $ref: "#/components/schemas/StudentSummary"
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
  /v1/bulk/students:upsert:
    post:
      operationId: BulkStudentsUpsert
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                rows:
                  type: array
                  items:
                    $ref: "#/components/schemas/CreateStudentRequest"
                  maxItems: 50000
              required:
                - rows
      responses:
        "202":
          description: Accepted
          headers:
            Location:
              schema:
                type: string
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
  /v1/bulk/jobs/{jobId}:
    get:
      operationId: GetBulkJobStatus
      parameters:
        - $ref: "#/components/parameters/TenantId"
        - $ref: "#/components/parameters/CorrelationId"
        - $ref: "#/components/parameters/CausationId"
        - name: jobId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                type: object
                properties:
                  jobId:
                    type: string
                  status:
                    type: string
                  processed:
                    type: integer
                  failed:
                    type: integer
        "400":
          $ref: "#/components/responses/ProblemResponse"
        "401":
          $ref: "#/components/responses/ProblemResponse"
        "403":
          $ref: "#/components/responses/ProblemResponse"
        "404":
          $ref: "#/components/responses/ProblemResponse"
        "409":
          $ref: "#/components/responses/ProblemResponse"
        "422":
          $ref: "#/components/responses/ProblemResponse"
        "429":
          $ref: "#/components/responses/ProblemResponse"
        "500":
          $ref: "#/components/responses/ProblemResponse"
```

### 05-Event-Schemas

#### lms.roster.v1.ClassArchived.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.ClassArchived",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "classId": {
          "type": "string",
          "format": "uuid"
        }
      },
      "required": ["classId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.ClassCreated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.ClassCreated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "classId": {
          "type": "string",
          "format": "uuid"
        },
        "schoolId": {
          "type": "string",
          "format": "uuid"
        }
      },
      "required": ["classId", "schoolId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.ClassUpdated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.ClassUpdated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "classId": {
          "type": "string",
          "format": "uuid"
        },
        "changes": {
          "type": "object"
        }
      },
      "required": ["classId", "changes"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.EnrollmentAdded.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.EnrollmentAdded",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "studentId": {
          "type": "string",
          "format": "uuid"
        },
        "classId": {
          "type": "string",
          "format": "uuid"
        },
        "effectiveDate": {
          "type": "string",
          "format": "date"
        }
      },
      "required": ["studentId", "classId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.EnrollmentRemoved.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.EnrollmentRemoved",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "studentId": {
          "type": "string",
          "format": "uuid"
        },
        "classId": {
          "type": "string",
          "format": "uuid"
        }
      },
      "required": ["studentId", "classId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.SchoolArchived.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.SchoolArchived",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "schoolId": {
          "type": "string",
          "format": "uuid"
        }
      },
      "required": ["schoolId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.SchoolCreated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.SchoolCreated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "schoolId": {
          "type": "string",
          "format": "uuid"
        }
      },
      "required": ["schoolId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.SchoolUpdated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.SchoolUpdated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "schoolId": {
          "type": "string",
          "format": "uuid"
        },
        "changes": {
          "type": "object"
        }
      },
      "required": ["schoolId", "changes"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.StudentCreated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.StudentCreated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "studentId": {
          "type": "string",
          "format": "uuid"
        },
        "status": {
          "type": "string"
        },
        "externalRefs": {
          "type": "object"
        }
      },
      "required": ["studentId", "status"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.StudentUpdated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.StudentUpdated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "studentId": {
          "type": "string",
          "format": "uuid"
        },
        "changes": {
          "type": "object"
        }
      },
      "required": ["studentId", "changes"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.StudentDeactivated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.StudentDeactivated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "studentId": {
          "type": "string",
          "format": "uuid"
        },
        "reason": {
          "type": "string"
        }
      },
      "required": ["studentId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.TeacherCreated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.TeacherCreated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "teacherId": {
          "type": "string",
          "format": "uuid"
        },
        "status": {
          "type": "string"
        }
      },
      "required": ["teacherId", "status"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.TeacherUpdated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.TeacherUpdated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "teacherId": {
          "type": "string",
          "format": "uuid"
        },
        "changes": {
          "type": "object"
        }
      },
      "required": ["teacherId", "changes"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.TeacherDeactivated.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.TeacherDeactivated",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "teacherId": {
          "type": "string",
          "format": "uuid"
        }
      },
      "required": ["teacherId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.TeacherAssignedToClass.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.TeacherAssignedToClass",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "teacherId": {
          "type": "string",
          "format": "uuid"
        },
        "classId": {
          "type": "string",
          "format": "uuid"
        },
        "role": {
          "type": "string"
        }
      },
      "required": ["teacherId", "classId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

#### lms.roster.v1.TeacherUnassignedFromClass.schema.json

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "lms.roster.v1.TeacherUnassignedFromClass",
  "type": "object",
  "properties": {
    "eventId": {
      "type": "string",
      "format": "uuid"
    },
    "schemaVersion": {
      "type": "string"
    },
    "tenantId": {
      "type": "string",
      "format": "uuid"
    },
    "occurredAtUtc": {
      "type": "string",
      "format": "date-time"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "causationId": {
      "type": "string",
      "format": "uuid"
    },
    "producer": {
      "type": "string"
    },
    "type": {
      "type": "string"
    },
    "payload": {
      "type": "object",
      "properties": {
        "teacherId": {
          "type": "string",
          "format": "uuid"
        },
        "classId": {
          "type": "string",
          "format": "uuid"
        }
      },
      "required": ["teacherId", "classId"]
    }
  },
  "required": [
    "eventId",
    "schemaVersion",
    "tenantId",
    "occurredAtUtc",
    "producer",
    "type",
    "payload"
  ]
}
```

### 06-Security-and-Tenancy.md

```markdown
# 06 — Security & Tenancy

- **AuthN:** OAuth2 (JWT bearer), mTLS between services (optional).
- **AuthZ:** Role + scope checks at API; per-tenant ownership checks at application layer.
- **Tenancy:** `X-Tenant-Id` required on every request; enforced at DB level (separate DB or schema); included in every event.
- **Headers:** `X-Correlation-Id`, `X-Causation-Id`, `Idempotency-Key` (for POST/PUT where applicable).
- **PII:** No PII in events; field-level encryption for sensitive attributes at rest.
```

### 07-Error-Model.md

```markdown
# 07 — Error Model

- Use `application/problem+json` with fields: `type`, `title`, `status`, `detail`, `instance`, plus `traceId`, `correlationId`.
- Status codes: 400, 401, 403, 404, 409, 422, 429, 5xx.
- Include `Retry-After` for 429/503 where backoff is recommended.
- Conflicts (e.g., duplicate school name) → 409 with `conflictId` and links to the conflicting resource.
```

### 08-Non-Functional-Requirements.md

```markdown
# 08 — Non-Functional Requirements

- **Performance**: Reads p95 ≤ 300ms; Writes p95 ≤ 800ms; bulk 50k rows ≤ 10 min p95.
- **Availability**: ≥ 99.9% weekly; graceful degradation on broker/DB hiccups.
- **Scalability**: Up to 2k tenants; up to 5M enrollments per tenant.
- **Security**: JWT validation, scope checks, PII minimization; audit on all writes.
- **Versioning**: `/v1` path prefix; deprecation via `Deprecation` & `Sunset` headers.
```

### 09-Observability.md

```markdown
# 09 — Observability

- **OpenTelemetry** for traces/metrics/logs; propagate correlation/causation IDs across HTTP + broker.
- Key metrics: `http_server_duration`, `events_published_latency_ms`, `outbox_backlog`, `bulk_job_throughput`.
- Health endpoints: `/health/live`, `/health/ready`, `/health/degraded`.
```

### 10-Glossary.md

```markdown
# 10 — Glossary

- **Tenant**: A district with isolated data plane.
- **Outbox/Inbox**: Patterns for reliable event publication and idempotent consumption.
- **Roster**: The set of Students, Teachers, Classes, Enrollments within a Tenant.
- **BFF**: Backend for Frontend; aggregates per UI boundary.
```

### 11-Traceability.md

```markdown
# 11 — Traceability

- BR → API → Event mapping (excerpt)

| Business Requirement | HTTP Endpoint(s)                           | Event(s)                                          |
| -------------------- | ------------------------------------------ | ------------------------------------------------- |
| CRUD Schools         | POST/GET/PATCH/DELETE /v1/schools          | SchoolCreated/Updated/Archived                    |
| CRUD Classes         | POST/GET/PATCH/DELETE /v1/classes          | ClassCreated/Updated/Archived                     |
| Enrollments          | POST/DELETE /v1/enrollments                | EnrollmentAdded/Removed                           |
| Teacher Assign       | POST/DELETE /v1/classes/{classId}/teachers | TeacherAssignedToClass/TeacherUnassignedFromClass |
| Coarse Roster        | GET /v1/roster/classes/{classId}/members   | (query only)                                      |
| Batch Lookup         | POST /v1/lookup/students:batch             | (query only)                                      |
```
