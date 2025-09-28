Feature: Audit Logging
    As a ComplianceOfficer
    I want to track all system changes
    So that I can maintain FERPA compliance and audit trails

Background:
    Given I am authenticated as a ComplianceOfficer for "oakland-unified"
    And audit logging is enabled for the district

Scenario: Student record changes are audited
    Given a student "Maria Garcia" exists in the system
    When a DistrictAdmin updates Maria's grade level from "Grade5" to "Grade6"
    Then an audit record should be created with:
      | Field        | Value                    |
      | EventType    | Update                   |
      | EntityType   | Student                  |
      | EntityId     | Maria's user ID          |
      | UserId       | Acting admin's ID        |
      | ChangeDetails| Grade5 → Grade6          |
      | Timestamp    | Current timestamp        |
    And the record should be tamper-evident with proper hash chaining

Scenario: Role assignment changes are audited
    When a DistrictAdmin assigns "Teacher" role to "John Smith" for class "Math 6A"
    Then an audit record should be created capturing:
      | Field           | Value                     |
      | EventType       | RoleAssigned              |
      | EntityType      | RoleAssignment            |
      | ActingUserId    | DistrictAdmin ID          |
      | TargetUserId    | John Smith's ID           |
      | RoleDetails     | Teacher role for Math 6A  |
    And the assignment should be linked to the audit chain

Scenario: District-level changes are audited in platform logs
    Given I am authenticated as a PlatformAdmin
    When I suspend the district "oakland-unified" 
    Then a platform audit record should be created with:
      | Field       | Value                |
      | EventType   | DistrictSuspended    |
      | TenantId    | oakland-unified      |
      | ActingUser  | PlatformAdmin ID     |
      | Details     | Suspension reason    |
    And the record should be stored in the platform audit schema

Scenario: Bulk operation audit with correlation ID
    When a DistrictAdmin performs a bulk student enrollment operation
    Then individual audit records should be created for each student
    And all records should share the same correlation ID
    And the bulk operation summary should be audited separately

Scenario: Audit chain integrity verification
    Given multiple audit records exist for the district
    When I verify the audit chain integrity
    Then each record should have a valid hash linking to the previous record
    And the chain should be unbroken from genesis to current
    And any tampering attempts should be detectable

Scenario: Access attempt logging
    When a user attempts to access a student record they don't have permission for
    Then a security audit record should be created with:
      | Field           | Value                    |
      | EventType       | UnauthorizedAccess       |
      | UserId          | Attempting user ID       |
      | TargetEntity    | Student record ID        |
      | AccessDeniedReason | Insufficient permissions |
      | IpAddress       | User's IP address        |
    And the attempt should be flagged for security monitoring

Scenario: Data retention audit logs
    When the automated retention process purges old student records
    Then audit records should be created documenting:
      | Field              | Value                     |
      | EventType          | DataPurged                |
      | PurgeJobId         | Job identifier            |
      | RecordsPurged      | Count of purged records   |
      | RetentionPeriod    | Applied retention policy  |
      | LegalHoldCheck     | Verification completed    |
    And the purge should be traceable for compliance reporting

Scenario: Audit query performance requirements
    Given the district has 1 million audit records
    When I query audit records for a specific student over 2 years
    Then the query should complete within 2 seconds
    And results should be properly paginated
    And query performance should meet compliance SLA requirements