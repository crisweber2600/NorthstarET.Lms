Feature: Composite Role Authorization
    As a DistrictAdmin
    I want to assign and manage complex role hierarchies
    So that users have appropriate access based on their responsibilities

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the following role definitions exist:
        | Role Name        | Permissions                              | Scopes           |
        | Teacher          | ViewStudents, EditGrades                | Class            |
        | Principal        | ViewStaff, ManageClasses, ViewReports   | School           |
        | DistrictAdmin    | ManageUsers, ViewAllData, SystemConfig  | District         |
        | SubstituteTeacher | ViewStudents                           | Class            |

Scenario: Assign single role to user
    Given a staff member "john.teacher@oaklandschools.org" exists
    And a class "Math 101" exists at "Lincoln Elementary"
    When I assign the "Teacher" role to the user for class "Math 101"
    Then the user should have "ViewStudents" and "EditGrades" permissions for that class
    And the role assignment should be active
    And a RoleAssignedEvent should be raised
    And the assignment should be audited

Scenario: Assign multiple roles to user across different scopes
    Given a staff member "mary.principal@oaklandschools.org" exists
    And schools "Lincoln Elementary" and "Roosevelt High" exist
    When I assign the following roles:
        | Role Name     | Scope               |
        | Principal     | Lincoln Elementary  |
        | Teacher       | Roosevelt High - Science 201 |
    Then the user should have principal permissions at Lincoln Elementary
    And the user should have teacher permissions for Science 201 at Roosevelt High
    And the roles should not interfere with each other

Scenario: Delegate role assignment with expiration
    Given a principal "mary.principal@oaklandschools.org" has delegation permissions
    And a staff member "john.teacher@oaklandschools.org" exists
    When the principal delegates "SubstituteTeacher" role to the user for class "Math 101" expiring in 30 days
    Then the delegated role should be active immediately
    And the delegation should expire automatically after 30 days
    And a RoleDelegatedEvent should be raised

Scenario: Automatic role expiration cleanup
    Given a delegated role assignment expires today
    When the role expiration cleanup job runs
    Then the expired role assignment should be marked as "Expired"
    And the user should lose the associated permissions
    And a RoleExpiredEvent should be raised
    And the expiration should be audited

Scenario: Hierarchical permission inheritance
    Given a user has "DistrictAdmin" role at district level
    When I check their permissions for any school or class in the district
    Then they should inherit all district-level permissions
    And the permission check should succeed for all scopes within the district

Scenario: Role conflict detection
    Given a user has "Teacher" role for "Math 101"
    When I attempt to assign "Principal" role for the same class
    Then the assignment should be rejected with error "Role conflict: Cannot be both teacher and principal for same class"
    And no new role assignment should be created

Scenario: Bulk role assignment with validation
    When I perform a bulk role assignment operation:
        | User Email              | Role Name | Scope          |
        | teacher1@school.org     | Teacher   | Math 101       |
        | teacher2@school.org     | Teacher   | Science 201    |
        | invalid@email           | Teacher   | Math 101       |
        | principal@school.org    | Invalid   | Math 101       |
    Then 2 role assignments should succeed
    And 2 role assignments should fail with validation errors
    And a bulk operation report should be generated
    And all operations should be audited

Scenario: Revoke role assignment
    Given a user has an active "Teacher" role for "Math 101"
    When I revoke the role assignment
    Then the role assignment should be marked as "Revoked"
    And the user should lose all permissions for that class
    And a RoleRevokedEvent should be raised
    And the revocation should be audited