# Roster Event Contracts (Plan)

## Common Envelope

- `eventId` (UUID)
- `schemaVersion` (SemVer)
- `tenantId` (UUID, partition key)
- `occurredAtUtc` (RFC3339 timestamp)
- `correlationId` (UUID)
- `causationId` (UUID, optional)
- `producer` (string)
- `type` (string)

## Event Payloads

### `lms.roster.v1.TenantProvisioned`

- `tenantId`: UUID
- `name`: string
- `region`: string
- `capabilities`: array[string]

### `lms.roster.v1.SchoolCreated`

- `schoolId`: UUID
- `tenantId`: UUID
- `name`: string
- `status`: string (Active)

### `lms.roster.v1.ClassCreated`

- `classId`: UUID
- `tenantId`: UUID
- `schoolId`: UUID
- `term`: string
- `subject`: string

### `lms.roster.v1.StudentCreated`

- `studentId`: UUID
- `tenantId`: UUID
- `status`: string (Active)
- `externalRefs`: object (no PII)

### `lms.roster.v1.EnrollmentAdded`

- `enrollmentId`: UUID
- `tenantId`: UUID
- `studentId`: UUID
- `classId`: UUID
- `effectiveDate`: date

### `lms.roster.v1.BulkJobCompleted`

- `jobId`: UUID
- `tenantId`: UUID
- `jobType`: string (Students|Enrollments)
- `totalRows`: integer
- `processedRows`: integer
- `failedRows`: integer

## Delivery & Ordering Guarantees

- Partition key: `tenantId`
- Delivery semantics: At-least-once with per-tenant ordering per clarification.
- Consumers must use `eventId` + `tenantId` for idempotency.
