Feature: Create Student
    As a DistrictAdmin
    I want to create student records
    So that I can manage student enrollment and academic data

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the district has available student quota

Scenario: Create student with valid basic data
    When I create a student with the following details:
      | Field          | Value        |
      | StudentNumber  | STU-2024-001 |
      | FirstName      | Maria        |
      | LastName       | Garcia       |
      | DateOfBirth    | 2010-06-15   |
      | EnrollmentDate | 2024-08-15   |
    Then the student should be created successfully
    And the student should have a unique UserId
    And the student status should be "Active"
    And the creation should be audited

Scenario: Create student with program participation flags
    When I create a student with the following details:
      | Field                    | Value        |
      | StudentNumber            | STU-2024-002 |
      | FirstName                | James        |
      | LastName                 | Wilson       |
      | DateOfBirth              | 2009-03-22   |
      | IsSpecialEducation       | true         |
      | IsGifted                 | false        |
      | IsEnglishLanguageLearner | true         |
    Then the student should be created successfully
    And the program flags should be set correctly
    And appropriate accommodation tags should be available for assignment

Scenario: Create student with accommodation tags
    When I create a student with accommodation tags:
      | StudentNumber | STU-2024-003          |
      | FirstName     | Sarah                 |
      | LastName      | Johnson               |
      | Tags          | extended-time, large-print, quiet-space |
    Then the student should be created successfully
    And the accommodation tags should be stored as an array
    And the tags should be searchable for reporting

Scenario: Create student with guardian information
    When I create a student with guardian details:
      | Field                    | Value                     |
      | StudentNumber            | STU-2024-004              |
      | FirstName                | Michael                   |
      | LastName                 | Brown                     |
      | GuardianFirstName        | Jennifer                  |
      | GuardianLastName         | Brown                     |
      | GuardianEmail            | jennifer.brown@email.com |
      | GuardianPhone            | +1-510-555-0123           |
      | GuardianRelationship     | Parent                    |
      | GuardianIsPrimary        | true                      |
      | GuardianCanPickup        | true                      |
    Then the student should be created successfully
    And a guardian relationship should be established
    And the guardian should be set as primary contact

Scenario: Reject duplicate student number in same district
    Given a student with number "STU-2024-005" already exists in "oakland-unified"
    When I attempt to create another student with number "STU-2024-005"
    Then the creation should be rejected with error "Student number already exists"
    And no duplicate student record should be created

Scenario: Allow same student number in different districts
    Given a student with number "STU-2024-006" exists in "berkeley-unified"
    When I create a student with number "STU-2024-006" in "oakland-unified"
    Then the student should be created successfully
    And both districts should have distinct student records

Scenario: Validate student number format
    When I attempt to create students with invalid student numbers:
      | StudentNumber | ExpectedError                    |
      |               | Student number is required       |
      | X             | Student number too short         |
      | STU-2024-001234567890123456789012345678901234567890 | Student number too long |
    Then each creation should be rejected with the appropriate error

Scenario: Validate date of birth constraints
    When I attempt to create a student with date of birth "2025-01-01"
    Then the creation should be rejected with error "Date of birth cannot be in the future"

    When I attempt to create a student with date of birth "1900-01-01"
    Then the creation should be rejected with error "Date of birth is unreasonably old"

Scenario: Enforce district quota limits
    Given the district has reached its student quota limit
    When I attempt to create a new student
    Then the creation should be rejected with error "District student quota exceeded"
    And the rejection should be audited

Scenario: Auto-generate unique UserId
    When I create multiple students simultaneously:
      | StudentNumber | FirstName | LastName |
      | STU-2024-007  | Alice     | Smith    |
      | STU-2024-008  | Bob       | Jones    |
      | STU-2024-009  | Carol     | Davis    |
    Then all students should be created successfully
    And each student should have a unique UserId
    And no UserId conflicts should occur

Scenario: Tenant isolation verification
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    When I create a student in my district
    Then the student should only be visible to users in "oakland-unified"
    And users from "berkeley-unified" should not see the student
    And the student should be scoped to the correct tenant schema

Scenario: Create student with future enrollment date
    When I create a student with enrollment date "2024-12-01" (future)
    Then the student should be created successfully
    And the student status should be "Active"
    And the enrollment date should be preserved for future enrollment processing

Scenario: Multiple guardian relationships
    When I create a student with multiple guardians:
      | GuardianType | FirstName | LastName | Relationship | IsPrimary | CanPickup |
      | Guardian1    | John      | Smith    | Parent       | true      | true      |
      | Guardian2    | Jane      | Smith    | Parent       | false     | true      |
      | Guardian3    | Robert    | Johnson  | Grandparent  | false     | false     |
    Then the student should be created successfully
    And all guardian relationships should be established
    And only one guardian should be marked as primary
    And appropriate pickup permissions should be set

Scenario: Create student triggers domain events
    When I create a student with number "STU-2024-010"
    Then the student should be created successfully
    And a "StudentCreated" domain event should be raised
    And the event should contain all relevant student data
    And audit processors should handle the event appropriately