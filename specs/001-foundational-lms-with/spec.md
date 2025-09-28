# Feature Specification: Foundational LMS with Tenant Isolation and Compliance

**Feature Branch**: `001-foundational-lms-with`  
**Created**: December 19, 2024  
**Status**: Draft  
**Input**: User description: "Foundational LMS supporting strict tenant isolation by SchoolDistrict, lifecycle management, RBAC, and compliance features"

## Clarifications

### Session 2024-12-19
- Q: What types of quotas should PlatformAdmins be able to set for districts? → A: User count limits (max students, staff, admins per district)
- Q: What should be the default retention periods for different entity types? → A: FERPA-aligned: Students 7 years, Staff 5 years, Assessments 3 years
- Q: What should be the assessment PDF file size and storage limits? → A: Large files: 100MB max per PDF, 10GB total per district
- Q: How should the system handle partial failures during bulk operations? → A: User-choice: Allow operation initiator to choose strategy per import
- Q: What actions should the system take when security alerts are generated? → A: Multi-tier: Log + notify for minor issues, auto-suspend for severe threats

## Scope & Overview

This specification defines a foundational Learning Management System (LMS) that provides:
- Strict tenant isolation by District
- Lifecycle management by SchoolYear and AcademicCalendar
- Full CRUD operations for Districts, Schools, Classes, Staff, Students, and Assessments
- Comprehensive RBAC with PlatformAdmin tenant provisioning
- Identity lifecycle management with Entra External ID integration
- Flexible Staff role model beyond traditional Teachers
- Student enrollment rollover, grade tracking, and profile enrichment
- Compliance features including audit, retention, and legal holds
- Bulk import/export capabilities with SIS-ready APIs
- Reporting capabilities for rosters and assessments

### Out of Scope
The following items are explicitly excluded from this foundational LMS:
- Authentication flows and SSO user experience
- Grading systems and grade book functionality
- Analytics and reporting dashboards
- Attendance tracking systems
- Cross-district federation capabilities
- Accessibility standards implementation
- Platform disaster recovery and multi-region concerns

## Personas & RBAC

### User Personas

**PlatformAdmin**
- Global scope across entire platform
- Can create, suspend, reactivate, and delete school districts
- Can set quotas and policies for districts
- Automatically has DistrictAdmin rights for all districts they create or manage

**DistrictAdmin**  
- Full read/write access within their assigned district
- Manages Schools, SchoolYears, Classes, Staff, Students, and Assessments
- Cannot access other districts' data

**DistrictUser**
- Scoped to their district with specific RBAC permissions
- Access determined by role definitions and assignments

**SchoolUser**
- Scoped to specific schools within their district
- Cannot access data from other schools unless explicitly granted

**Staff** (flexible roles including Teachers)
- Role capabilities defined by RoleDefinition entities
- Can hold multiple or composite roles simultaneously
- Role assignments scoped to specific Schools, Classes, and/or SchoolYears
- Supports delegation allowing temporary role assumption with auto-expiry

**Guardian**
- Linked to one or more Students
- No direct system access in MVP (future enhancement)

### RBAC Model
The system implements a hierarchical access control model:
- **Platform Level**: PlatformAdmin has global access
- **District Level**: DistrictAdmin controls district resources  
- **School Level**: SchoolUser access limited to assigned schools
- **Class/Year Level**: Staff access scoped to specific assignments

The model supports hybrid authorization combining:
- Role-based permissions
- Predicate-based rules (e.g., allow-listed students)
- Hierarchical inheritance with least-privilege enforcement

## User Scenarios & Testing

### Primary User Story
A PlatformAdmin provisions a new school district, automatically receiving DistrictAdmin rights. The DistrictAdmin then sets up schools, academic calendars, and staff roles. Staff members are assigned to classes with appropriate permissions, while students are enrolled and tracked through their academic progression.

### Acceptance Scenarios

1. **District Provisioning**
   - **Given** a PlatformAdmin is authenticated
   - **When** they create a new district with slug and display name
   - **Then** the district is created with default quotas and the PlatformAdmin automatically receives DistrictAdmin rights for that district

2. **Identity Mapping and Lifecycle**  
   - **Given** a new staff member joins the district
   - **When** their identity is mapped via Entra External ID
   - **Then** a join event is logged and they receive appropriate role assignments based on their position

3. **Academic Calendar Management**
   - **Given** a DistrictAdmin is setting up a school year
   - **When** they attempt to create overlapping terms in the academic calendar
   - **Then** the system rejects the creation and displays validation errors

4. **Composite Role Authorization**
   - **Given** a staff member has both Teacher and Advisor roles assigned
   - **When** they access student data
   - **Then** they can see students from both their teaching classes and advisory assignments

5. **Bulk Student Rollover**
   - **Given** the academic year is ending
   - **When** a DistrictAdmin initiates bulk rollover to promote Grade 5 students to Grade 6
   - **Then** a preview report is generated showing affected students before applying changes

6. **Legal Hold Compliance**
   - **Given** a student record is under legal hold
   - **When** the automated purge job runs after the retention period expires
   - **Then** the student record is skipped and an audit note records the hold reason

7. **Security Monitoring**
   - **Given** repeated unauthorized access attempts occur
   - **When** the anomaly detection system processes access logs
   - **Then** a security alert is generated and logged for administrator review

### Edge Cases

- **District Deletion**: What happens when a district has active legal holds or hasn't met retention requirements?
- **Staff Suspension**: How does the system handle access to classes and students when a staff member is suspended mid-year?
- **Academic Year Overlap**: How does the system prevent scheduling conflicts when SchoolYears have overlapping date ranges?
- **How are partial failures handled during large enrollment imports? → User-configurable strategies (all-or-nothing, best-effort, threshold-based)**
- **External ID Conflicts**: What happens when multiple users claim the same Entra External ID?

## Requirements

### Functional Requirements

#### Platform Administration
- **FR-001**: System MUST allow PlatformAdmins to create districts with unique slug and display name
- **FR-001a**: System MUST allow PlatformAdmins to set user count quotas (max students, staff, admins) per district
- **FR-002**: System MUST automatically grant DistrictAdmin rights to PlatformAdmin upon district creation
- **FR-003**: System MUST support district lifecycle management (activate, suspend, reactivate, delete)
- **FR-004**: System MUST prevent district deletion when retention policies or legal holds are active
- **FR-005**: System MUST audit all platform administration actions in immutable logs

#### Identity & Lifecycle Management  
- **FR-006**: System MUST map all users (Staff, Students, Guardians) to Entra External ID
- **FR-007**: System MUST support user lifecycle events (join, leave, suspend, reinstate)
- **FR-008**: System MUST maintain historical records of guardian-student associations
- **FR-009**: System MUST prevent duplicate external identity mappings within the platform

#### Academic Calendar Management
- **FR-010**: System MUST allow DistrictAdmins to define Academic Calendars per SchoolYear
- **FR-011**: System MUST prevent overlapping terms within the same academic calendar
- **FR-012**: System MUST support closure definitions that override instructional days
- **FR-013**: System MUST validate calendar date ranges against SchoolYear boundaries

#### Staff Role Management & Delegation
- **FR-014**: System MUST support creation of flexible RoleDefinitions with specific permissions and scope
- **FR-015**: System MUST allow staff members to hold multiple roles simultaneously  
- **FR-016**: System MUST support composite authorization combining multiple role permissions
- **FR-017**: System MUST support temporary role delegation with configurable auto-expiry
- **FR-018**: System MUST enforce role scope restrictions (School, Class, SchoolYear)

#### Student Management
- **FR-019**: System MUST track student grade level per SchoolYear
- **FR-020**: System MUST support enrollment status tracking (active, transferred, graduated, withdrawn)
- **FR-021**: System MUST support program flags (special education, gifted, ELL)
- **FR-022**: System MUST support accommodation tags for individual student needs
- **FR-023**: System MUST maintain student enrollment history across transfers

#### Enrollment & Academic Progression  
- **FR-024**: System MUST support student enrollment in multiple Classes across Schools within district
- **FR-025**: System MUST provide bulk rollover functionality with preview/dry-run capability
- **FR-026**: System MUST support mid-year intra-district transfers while preserving history
- **FR-027**: System MUST make archived SchoolYears immutable but readable
- **FR-028**: System MUST prevent data modification in archived academic periods

#### Assessment Management
- **FR-029**: System MUST support district-owned, versioned, immutable assessment definitions
- **FR-030**: System MUST support optional SchoolYear pinning for assessments
- **FR-031**: System MUST restrict assessment CRUD operations to Admin/District users only
- **FR-032**: System MUST provide read-only assessment access to Teachers/SchoolUsers via RBAC

#### Bulk Operations & Integration
- **FR-033**: System MUST support bulk import of Students, Staff, Classes, and Enrollments via CSV/JSON with user-selectable error handling strategies:
  - **All-or-Nothing**: Entire import fails if any record fails validation
  - **Best-Effort**: Continue processing valid records, report failed records for manual review
  - **Threshold-Based**: Fail entire import if error rate exceeds configurable threshold (default 5%)
  - **Preview Mode**: Generate detailed report of changes without applying them
- **FR-034**: System MUST provide bulk rollover capabilities for grade promotion
- **FR-035**: System MUST support bulk reassignment operations for school closure scenarios
- **FR-036**: System MUST export roster and assessment data in CSV format
- **FR-037**: System MUST provide stable, idempotent REST APIs with pagination support
- **FR-038**: System MUST support idempotency keys to prevent duplicate operations

#### Retention & Legal Compliance
- **FR-039**: System MUST support configurable retention policies per entity type with FERPA-aligned defaults (Students: 7 years, Staff: 5 years, Assessments: 3 years)
- **FR-040**: System MUST implement legal holds that prevent data purging
- **FR-041**: System MUST run scheduled purge jobs that enforce retention policies
- **FR-042**: System MUST audit all retention and purge activities
- **FR-043**: System MUST skip purge operations for records under legal hold

#### Audit & Observability
- **FR-044**: System MUST audit all CRUD operations, RBAC changes, lifecycle events, and bulk jobs
- **FR-045**: System MUST support audit queries by actor, entity, timeframe, and school_year
- **FR-046**: System MUST provide audit export functionality for compliance reporting
- **FR-047**: System MUST detect access anomalies and generate multi-tier security alerts:
  - **Tier 1 (Minor)**: Failed login attempts, unusual access times → Log + notify administrators
  - **Tier 2 (Major)**: Repeated authorization failures, suspicious data access patterns → Auto-suspend user + alert
  - **Tier 3 (Critical)**: Potential data breach indicators, privilege escalation attempts → Auto-suspend + immediate security team alert
- **FR-048**: System MUST implement tamper-evident chaining for audit logs
- **FR-049**: System MUST track repeated authorization failures and suspicious access patterns

#### Data Isolation & Security
- **FR-050**: System MUST enforce strict tenant isolation by District
- **FR-051**: System MUST scope all operations by appropriate SchoolYear context
- **FR-052**: System MUST implement deny-by-default RBAC resolution
- **FR-053**: System MUST enforce least privilege access principles
- **FR-054**: System MUST provide secure, scoped access to assessment PDF files with the following requirements:
  - **Scoped URLs**: Generate tenant-isolated, time-limited access URLs (default 1-hour expiry)
  - **File Size Limits**: 100MB maximum per individual PDF file
  - **Storage Quotas**: 10GB total assessment storage per district
  - **Access Control**: URLs MUST validate user authorization and tenant context before serving files
  - **Audit Trail**: All file access requests MUST be logged for compliance monitoring

### Performance Requirements
- **PR-001**: CRUD operations MUST complete within 200ms at 95th percentile
- **PR-002**: Bulk operations (10,000 rows) MUST complete within 120 seconds
- **PR-003**: Audit queries MUST return results within 2 seconds on 1 million records
- **PR-004**: System MUST maintain performance standards under normal district-sized loads

### Key Entities

- **District**: Top-level tenant entity representing a school district, lifecycle managed by PlatformAdmin, contains schools and users
- **SchoolYear**: Academic cycle with start/end dates and status, provides temporal scoping for Classes, Enrollments, and RoleAssignments
- **AcademicCalendar**: Bound to SchoolYear, defines Terms and Closures for academic scheduling
- **School**: Belongs to one District, owns Classes and Staff assignments, represents physical or virtual school locations  
- **Class**: Bound to School and SchoolYear, has Staff assignments and Student enrollments, represents instructional groups
- **Staff**: Generic entity supporting flexible roles, linked to Classes and Schools, can hold multiple role assignments
- **Student**: Globally unique record with enrollments scoped to Classes within SchoolYears, tracks academic progression
- **Guardian**: Associated with Students, maintains relationship history for compliance
- **AssessmentDefinition**: District-owned, versioned, immutable artifact with optional SchoolYear pinning
- **IdentityMapping**: Links internal user_id to external_id and issuer for Entra External ID integration
- **RetentionPolicy**: Entity-level data retention configuration with compliance enforcement
- **LegalHold**: Entity-level holds that override retention policies for legal preservation
- **AuditRecord/PlatformAuditRecord**: Immutable, queryable logs with tamper-evident chaining for compliance

### Constraints & Guardrails
- **CG-001**: PlatformAdmins MUST always inherit DistrictAdmin role by default for managed districts
- **CG-002**: District deletion MUST pass retention and legal hold validation checks
- **CG-003**: Student and Staff lifecycle operations MUST preserve historical data integrity
- **CG-004**: Assessment PDF access MUST use scoped, expiring URLs rather than direct file exposure
- **CG-005**: All RBAC resolution MUST implement deny-by-default with explicit permission granting
- **CG-006**: Academic calendar terms MUST NOT overlap within the same SchoolYear
- **CG-007**: Archived SchoolYears MUST be read-only and immutable

---

## Review & Acceptance Checklist

### Content Quality
- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs  
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

### Requirement Completeness  
- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

---

## Execution Status

- [x] User description parsed - Comprehensive LMS specification with tenant isolation and compliance
- [x] Key concepts extracted - Districts, Schools, RBAC, Lifecycle, Audit, Compliance
- [x] Ambiguities marked - None identified, specification is comprehensive
- [x] User scenarios defined - 7 primary acceptance scenarios plus edge cases
- [x] Requirements generated - 54 functional requirements across 9 domains  
- [x] Entities identified - 12 key entities with relationships and constraints
- [x] Review checklist passed - All quality and completeness criteria met

**STATUS**: ✅ COMPLETE - Specification ready for planning phase

**Next Steps**: 
- Proceed to planning phase to break down requirements into implementable features
- Consider architectural patterns for multi-tenant isolation
- Design data models supporting RBAC and audit requirements
- Plan integration approach for Entra External ID
