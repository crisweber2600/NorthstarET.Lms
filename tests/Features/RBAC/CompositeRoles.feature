Feature: Composite Role Management
    As a DistrictAdmin
    I want to create and manage composite roles that combine multiple role definitions
    So that I can efficiently assign complex permission sets to staff members

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the following role definitions exist:
      | Role Name          | Scope     | Permissions                    |
      | Teacher            | Class     | ViewStudents, GradeAssignments |
      | DepartmentHead     | School    | ManageStaff, ViewReports       |
      | CurriculumLeader   | District  | ManageCurriculum, TrainStaff   |
      | DataAnalyst        | District  | ViewAllReports, ExportData     |

Scenario: Create composite role with multiple base roles
    When I create a composite role with:
      | Field           | Value                              |
      | Name            | MathDepartmentLead                |
      | Description     | Math Department Head with Teaching |
      | Base Roles      | Teacher, DepartmentHead           |
      | Scope           | School                            |
    Then the composite role should be created successfully
    And the role should inherit permissions from both base roles
    And the effective permissions should be "ViewStudents, GradeAssignments, ManageStaff, ViewReports"
    And the creation should be audited

Scenario: Assign composite role to staff member
    Given a composite role "MathDepartmentLead" exists with base roles "Teacher, DepartmentHead"
    And a staff member "Jane Smith" with employee number "EMP-2024-001" exists
    When I assign the composite role "MathDepartmentLead" to staff "EMP-2024-001"
    And I scope the assignment to school "Lincoln Elementary"
    Then the staff member should have all permissions from both base roles
    And the assignment should be scoped to "Lincoln Elementary"
    And the staff member should be able to perform both teaching and administrative functions
    And the assignment should be audited

Scenario: Resolve permission conflicts in composite roles
    Given the following role definitions exist:
      | Role Name     | Permissions                        | Access Level |
      | ClassTeacher  | ViewStudents:Class                 | Class        |
      | GradeLeader   | ViewStudents:Grade                 | Grade        |
    When I create a composite role "GradeTeamLead" with base roles:
      | Base Role     |
      | ClassTeacher  |
      | GradeLeader   |
    Then the role should resolve permission conflicts using the highest access level
    And the effective permission should be "ViewStudents:Grade"
    And the permission resolution should be logged

Scenario: Validate composite role scope compatibility
    Given the following role definitions exist:
      | Role Name      | Scope    |
      | ClassTeacher   | Class    |
      | DistrictAdmin  | District |
    When I attempt to create a composite role with base roles:
      | Base Role      |
      | ClassTeacher   |
      | DistrictAdmin  |
    Then the creation should be rejected with error "Cannot combine roles with incompatible scopes: Class and District"

Scenario: Update composite role definition
    Given a composite role "ScienceTeamLead" exists with base roles "Teacher, DepartmentHead"
    When I update the composite role to add base role "CurriculumLeader"
    Then the role should include permissions from all three base roles
    And existing staff assignments should automatically inherit the new permissions
    And the update should trigger permission recalculation for affected users
    And the update should be audited with before/after permission changes

Scenario: Remove base role from composite role
    Given a composite role "AdminTeacher" exists with base roles "Teacher, DepartmentHead, DataAnalyst"
    And staff member "John Doe" is assigned this composite role
    When I remove the "DataAnalyst" base role from the composite role
    Then the composite role should no longer include DataAnalyst permissions
    And staff member "John Doe" should lose the DataAnalyst permissions
    And the permission change should be audited
    And affected users should be notified of permission changes

Scenario: Delete composite role with active assignments
    Given a composite role "TempRole" exists with base roles "Teacher, DepartmentHead"
    And 3 staff members are currently assigned this composite role
    When I attempt to delete the composite role "TempRole"
    Then the deletion should be rejected with error "Cannot delete composite role with active assignments"
    And I should receive a list of staff members with active assignments
    
Scenario: Archive composite role safely
    Given a composite role "OutdatedRole" exists with base roles "Teacher, DepartmentHead"
    And 2 staff members are currently assigned this composite role
    When I archive the composite role "OutdatedRole"
    Then the role should be marked as archived
    And existing assignments should remain active but cannot be extended
    And no new assignments should be allowed for the archived role
    And the archival should be audited

Scenario: View composite role effective permissions
    Given a composite role "ComplexRole" exists with base roles:
      | Base Role        | Permissions                    |
      | Teacher          | ViewStudents, CreateGrades     |
      | Counselor        | ViewStudents, AccessRecords    |
      | DepartmentHead   | ManageStaff, ViewReports       |
    When I request the effective permissions for "ComplexRole"
    Then I should see the consolidated permission set:
      | Permission      | Source Roles            | Access Level |
      | ViewStudents    | Teacher, Counselor      | Highest      |
      | CreateGrades    | Teacher                 | Class        |
      | AccessRecords   | Counselor               | Student      |
      | ManageStaff     | DepartmentHead          | School       |
      | ViewReports     | DepartmentHead          | School       |
    And I should see any permission overlaps clearly identified

Scenario: Delegate composite role temporarily
    Given a staff member "Principal Smith" has composite role "SchoolAdmin"
    And the role includes permissions "ManageStaff, ViewReports, ApproveRequests"
    When Principal Smith delegates the composite role to "Assistant Principal Jones"
    And sets the delegation expiry to "2024-12-31"
    Then "Assistant Principal Jones" should receive all permissions from the composite role
    And the delegation should automatically expire on "2024-12-31"
    And the delegation should be audited with delegator and delegate information
    And Principal Smith should retain the original role permissions

Scenario: Audit composite role usage
    Given a composite role "DataManager" has been assigned to 5 staff members
    And the role has been active for 6 months
    When I request an audit report for composite role usage
    Then I should see:
      | Metric                    | Value |
      | Total Assignments         | 5     |
      | Active Assignments        | 4     |
      | Permissions Exercised     | 15    |
      | Security Events           | 2     |
    And I should see a breakdown of permission usage by staff member
    And I should see any security alerts related to the composite role