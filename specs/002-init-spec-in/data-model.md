# Data Model Design

## Tenant

- **Fields**: tenantId (UUID), name, region, status (Active|Suspended|Deactivated), capabilities (JSON), createdAt, updatedAt
- **Constraints**: `tenantId` immutable; name unique globally; region required for latency routing.
- **Relationships**: 1→many Schools, Students, Teachers, Classes, BulkJobs.
- **State Transitions**: Active → Suspended (admin action), Suspended → Active, Active → Deactivated (hard stop).

## School

- **Fields**: schoolId (UUID), tenantId, name, status (Active|Archived), externalRefs (JSON), createdAt, updatedAt
- **Constraints**: Unique (tenantId, name) per FR-002; archived schools block new classes.
- **Relationships**: belongs to Tenant; 1→many Classes.
- **Audit**: Archive operations log actor and reason.

## Class

- **Fields**: classId (UUID), tenantId, schoolId, name, term, subject, status (Active|Archived), externalRefs (JSON), createdAt, updatedAt
- **Constraints**: Unique (schoolId, name); term required; archived classes block new enrollments.
- **Relationships**: belongs to School and Tenant; many-to-many with Teacher via ClassTeacher; many-to-many with Student via Enrollment.
- **Events**: Emits Created/Updated/Archived with tenant partition key.

## Student

- **Fields**: studentId (UUID), tenantId, givenName, familyName, status (Active|Deactivated), externalRefs (JSON), birthDate (optional), createdAt, updatedAt
- **Constraints**: `studentId` is primary natural identifier for upsert; Deactivated state blocks new enrollments.
- **Relationships**: belongs to Tenant; many Enrollments.
- **Privacy**: PII fields stored encrypted at rest, excluded from events.

## Teacher

- **Fields**: teacherId (UUID), tenantId, givenName, familyName, status (Active|Deactivated), certification (JSON), externalRefs (JSON), createdAt, updatedAt
- **Constraints**: Unique (tenantId, externalRefs.sisId) when provided.
- **Relationships**: many ClassTeacher assignments; audit on role changes.

## Enrollment

- **Fields**: enrollmentId (UUID), tenantId, studentId, classId, status (Active|Withdrawn), effectiveStart, effectiveEnd (nullable), createdAt, updatedAt
- **Constraints**: Unique (studentId, classId, effectiveStart); enforce same tenant for student/class.
- **Relationships**: belongs to Student, Class, Tenant.
- **Invariants**: Student and Class must be Active.

## ClassTeacher

- **Fields**: classTeacherId (UUID), tenantId, classId, teacherId, role (Lead|Assistant), assignedAt, removedAt (nullable)
- **Constraints**: Unique (classId, teacherId, role, assignedAt); removal uses soft-close via removedAt.
- **Relationships**: belongs to Class, Teacher, Tenant.

## BulkJob

- **Fields**: jobId (UUID), tenantId, jobType (Enrollments|Students|Teachers), submittedBy, submittedAt, status (Pending|Running|Completed|Failed), totalRows, processedRows, failedRows, checksum, hangfireId
- **Constraints**: jobType determines payload schema; checksum ensures idempotent replay.
- **Relationships**: belongs to Tenant; references row-level job results table.
- **Behavior**: Completed jobs persist row-level results for 30 days.

## BulkJobResult

- **Fields**: resultId (UUID), jobId, rowNumber, status (Succeeded|Failed|Skipped), studentId (optional), errorCode (optional), errorMessage (optional), createdAt
- **Constraints**: (jobId, rowNumber) unique; storing `studentId` when created during job.
- **Usage**: Supports per-row summary requirement and idempotent replays.

## AuditTrail

- **Fields**: auditId, tenantId, actorId, actorType, action, entityType, entityId, payloadFingerprint, outcome, createdAt
- **Purpose**: Satisfies FR-009; stored immutably.
