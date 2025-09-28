Feature: Role Assignment
    As a DistrictAdmin
    I want to assign roles to users
    So that I can control access permissions within my district

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the following role definitions exist:
      | RoleName     | Scope    | Permissions                    |
      | Teacher      | Class    | ViewStudents, UpdateGrades     |
      | Principal    | School   | ManageSchool, ViewAllStudents  |
      | Counselor    | School   | ViewStudents, UpdateSchedules  |

Scenario: Assign user to teacher role for specific class
    Given a staff member "John Smith" exists with user ID "staff-001"
    And a class "Mathematics 6A" exists with class ID "class-001"
    When I assign the "Teacher" role to "John Smith" for class "Mathematics 6A"
    Then the role assignment should be created successfully
    And the assignment should be scoped to class "class-001"
    And the assignment should be effective immediately
    And the assignment should be audited

Scenario: Assign user to principal role for entire school
    Given a staff member "Sarah Johnson" exists with user ID "staff-002"
    And a school "Lincoln Elementary" exists with school ID "school-001"
    When I assign the "Principal" role to "Sarah Johnson" for school "Lincoln Elementary"
    Then the role assignment should be created successfully
    And the assignment should be scoped to school "school-001"
    And Sarah should have access to all classes in the school

Scenario: Prevent duplicate role assignments
    Given "John Smith" already has "Teacher" role for class "Mathematics 6A"
    When I attempt to assign "Teacher" role to "John Smith" for class "Mathematics 6A" again
    Then the assignment should be rejected with error "User already has this role for the specified scope"

Scenario: Role assignment with expiration date
    Given a staff member "Mary Wilson" exists
    And a temporary assignment is needed for "substitute-teacher"
    When I assign the "Teacher" role to "Mary Wilson" with expiration date "2024-12-31"
    Then the role assignment should be created with expiration
    And the assignment should automatically expire on "2024-12-31"

Scenario: Revoke role assignment
    Given "John Smith" has "Teacher" role for class "Mathematics 6A"
    When I revoke the "Teacher" role from "John Smith" for class "Mathematics 6A"
    Then the role assignment should be deactivated
    And the revocation should be audited
    And John should immediately lose access to the class

Scenario: Verify hierarchical permissions
    Given "Sarah Johnson" has "Principal" role for school "Lincoln Elementary" 
    And "Mathematics 6A" is a class within "Lincoln Elementary"
    When I check Sarah's permissions for class "Mathematics 6A"
    Then she should have inherited permissions from her Principal role
    And she should be able to view all students in the class

Scenario: Role assignment requires appropriate permissions
    Given I am authenticated as a "Teacher" 
    And I attempt to assign a "Principal" role to another user
    Then the operation should be rejected with error "Insufficient permissions: only DistrictAdmin can assign Principal roles"

Scenario: Bulk role assignments
    Given I have a list of 10 new teachers to assign to various classes
    When I submit a bulk role assignment request
    Then all eligible assignments should be processed
    And each assignment should be individually audited
    And any failures should be reported with specific reasons