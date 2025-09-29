# Phase 1 Data Model – Foundational LMS

## Overview

The LMS follows Clean Architecture with tenant-scoped aggregates. All entities derive from `TenantScopedEntity` (implicit `TenantSlug`) and, where applicable, maintain audit metadata (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`). IDs are GUIDs unless specified.

## DistrictTenant

- **Fields**: `Id`, `Slug` (unique), `DisplayName`, `Status` (Active/Suspended/Deleted), `QuotaStudents`, `QuotaStaff`, `QuotaAdmins`, `RetentionOverrides` (per entity type), `CreatedAt`, `ActivatedAt`, `SuspendedAt`, `SuspendedReason`
- **Relationships**: Owns Schools, SchoolYears, RoleDefinitions, Staff, Students, Assessments
- **Rules**:
  - Slug immutable post-creation
  - Cannot delete when legal holds or retention windows pending
  - Status transitions recorded as domain events

## SchoolYear

- **Fields**: `Id`, `DistrictId`, `Label`, `StartDate`, `EndDate`, `Status` (Draft/Active/Archived), `ArchivedAt`
- **Relationships**: Owns AcademicCalendar, Classes, RoleAssignments, Enrollments
- **Rules**:
  - Date range cannot overlap existing SchoolYears in same district
  - Archiving locks all mutable child aggregates

## AcademicCalendar

- **Fields**: `Id`, `SchoolYearId`, `Terms` (collection of `Term` value objects), `Closures` (collection of `Closure` value objects)
- **Value Objects**:
  - `Term`: `Id`, `Name`, `StartDate`, `EndDate`
  - `Closure`: `Id`, `Name`, `StartDate`, `EndDate`, `Reason`
- **Rules**:
  - Terms must not overlap and must be within SchoolYear window
  - Closures override instructional days (tracked for attendance integrations)

## School

- **Fields**: `Id`, `DistrictId`, `Name`, `ExternalCode`, `SchoolType`, `Status`, `Address`, `Phone`
- **Relationships**: Owns Classes, StaffAssignments
- **Rules**:
  - Status changes raise domain events for RBAC recalculation

## Class

- **Fields**: `Id`, `SchoolId`, `SchoolYearId`, `Name`, `Code`, `GradeBand`, `Capacity`, `Status`
- **Relationships**: Aggregates `StaffAssignment` and `Enrollment`
- **Rules**:
  - Unique `Code` per SchoolYear + School
  - Cannot exceed capacity without override flag

## Staff

- **Fields**: `Id`, `UserId`, `ExternalId`, `ExternalIssuer`, `FirstName`, `LastName`, `Email`, `EmploymentStatus`, `HireDate`, `EndDate`
- **Relationships**: Has many `RoleAssignment` aggregates; linked to Schools and Classes via assignments
- **Rules**:
  - External identity unique per issuer across platform
  - Suspension revokes active assignments but preserves history

## Student

- **Fields**: `Id`, `UserId`, `StudentNumber`, `FirstName`, `LastName`, `DateOfBirth`, `GradeLevel`, `ProgramFlags` (value object set), `AccommodationTags` (value object set), `Status`
- **Relationships**: Has `Enrollment` per Class/SchoolYear, `GuardianLink`
- **Rules**:
  - StudentNumber unique per district
  - Grade progression tracked per SchoolYear; archived years immutable

## Guardian

- **Fields**: `Id`, `FirstName`, `LastName`, `Email`, `Phone`, `Address`
- **Relationships**: `GuardianLink` to Students with relationship type and effective dates
- **Rules**:
  - Relationship history immutable after closure

## RoleDefinition

- **Fields**: `Id`, `DistrictId`, `Name`, `Description`, `Scopes` (allowed contexts), `Permissions` (collection of permission codes)
- **Relationships**: Linked to `RoleAssignment`
- **Rules**:
  - Permission sets versioned to support auditing changes

## RoleAssignment

- **Fields**: `Id`, `RoleDefinitionId`, `UserId`, `SchoolId?`, `ClassId?`, `SchoolYearId?`, `DelegatedBy?`, `DelegationExpiresAt?`, `Status`
- **Rules**:
  - At least one scope (School/SchoolYear/Class) must be set unless District-wide role
  - Delegations auto-expire via background job and trigger domain events

## Enrollment

- **Fields**: `Id`, `StudentId`, `ClassId`, `SchoolYearId`, `EnrollmentStatus`, `EntryDate`, `ExitDate?`
- **Rules**:
  - Supports mid-year transfers with history records (one row per enrollment period)
  - Cannot modify if SchoolYear archived

## AssessmentDefinition

- **Fields**: `Id`, `DistrictId`, `Title`, `Version`, `Subject`, `GradeLevels`, `Description`, `PinnedSchoolYearId?`, `StorageUri`, `FileSize`, `UploadDigest`
- **Rules**:
  - Immutable after publication; new revisions create new instance with incremented version
  - Upload limited to 100MB; total storage per district tracked for quotas

## IdentityMapping

- **Fields**: `Id`, `InternalUserId`, `ExternalId`, `Issuer`, `MappedAt`, `Status`
- **Rules**:
  - Enforce unique `(ExternalId, Issuer)` across platform
  - Conflict resolution raises audit events and suspends duplicate mappings

## RetentionPolicy

- **Fields**: `Id`, `EntityType`, `RetentionYears`, `GracePeriodDays`, `EffectiveDate`, `OverrideReason`
- **Rules**:
  - Defaults seeded per FERPA requirements; overrides require PlatformAdmin approval

## LegalHold

- **Fields**: `Id`, `EntityType`, `EntityId`, `Reason`, `IssuedBy`, `IssuedAt`, `ExpiresAt?`, `Status`
- **Rules**:
  - Overrides retention purge until released
  - Audit events capture release decisions

## AuditRecord

- **Fields**: `Id`, `Timestamp`, `ActorId`, `ActorRole`, `Action`, `EntityType`, `EntityId`, `Payload`, `PreviousHash`, `CurrentHash`, `CorrelationId`
- **Rules**:
  - `CurrentHash` computed from `PreviousHash + Payload`
  - Stored per tenant schema; replicated summary to PlatformAudit for cross-tenant visibility

## BulkJob

- **Fields**: `Id`, `JobType`, `RequestedBy`, `RequestedAt`, `Status`, `ErrorHandlingMode`, `Progress`, `TotalItems`, `SuccessCount`, `FailureCount`, `FailureThreshold`
- **Relationships**: Contains collection of `BulkJobItem` (value object with item payload + error info)
- **Rules**:
  - Preview jobs flagged as dry-run, produce report but do not mutate state
  - Threshold mode fails job when `FailureCount/TotalItems` exceeds threshold

## Domain Events (Sample)

- `DistrictProvisionedEvent`
- `RoleAssignmentDelegationExpiredEvent`
- `StudentPromotedEvent`
- `LegalHoldAppliedEvent`
- `BulkJobFailedEvent`

## Value Objects & Enumerations

- `TenantSlug`, `Quota`, `ProgramFlag`, `AccommodationTag`, `AuditAction`, `BulkJobStatus`, `RetentionEntityType`, `SecurityAlertTier`

## Cross-Cutting Concerns

- **Soft Delete**: Represented via status rather than deletion to maintain retention requirements.
- **Concurrency**: Optimistic concurrency tokens on mutable aggregates (e.g., `RowVersion`).
- **Audit Hooks**: Domain events captured for every mutation and forwarded to audit pipeline.
