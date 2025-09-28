Feature: Student Enrollment Management
    As a DistrictAdmin or SchoolUser
    I want to manage student class enrollments
    So that I can track academic placement and progress

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And a student "Maria Garcia" with number "STU-2024-001" exists
    And a class "Mathematics 6A" exists for school year "2024-2025"

Scenario: Enroll student in class
    When I enroll the student in class "Mathematics 6A" for grade level "Grade6"
    Then the enrollment should be created successfully
    And the enrollment status should be "Active" 
    And the enrollment date should be today
    And the enrollment should be audited

Scenario: Prevent duplicate enrollment in same class
    Given the student is already enrolled in "Mathematics 6A"
    When I attempt to enroll the student in "Mathematics 6A" again
    Then the enrollment should be rejected with error "Student already enrolled in this class"

Scenario: Allow enrollment in multiple classes
    Given the student is enrolled in "Mathematics 6A"
    When I enroll the student in "English 6A"
    Then the enrollment should be created successfully
    And the student should have active enrollments in both classes

Scenario: Transfer student between classes
    Given the student is enrolled in "Mathematics 6A"
    When I transfer the student from "Mathematics 6A" to "Mathematics 6B"
    Then the old enrollment should be marked as "Transferred"
    And a new enrollment should be created in "Mathematics 6B"
    And both actions should be audited with correlation ID

Scenario: Withdraw student from class
    Given the student is enrolled in "Mathematics 6A"
    When I withdraw the student with reason "Schedule conflict"
    Then the enrollment status should be "Withdrawn"
    And the withdrawal date should be recorded
    And the withdrawal reason should be saved
    And the withdrawal should be audited

Scenario: Validate grade level compatibility
    Given a class "Algebra II" exists for grade level "Grade11"
    When I attempt to enroll a Grade 6 student in "Algebra II"
    Then the enrollment should be rejected with error "Grade level mismatch: student Grade6 cannot enroll in Grade11 class"

Scenario: Respect class capacity limits
    Given the class "Mathematics 6A" has reached its capacity limit
    When I attempt to enroll a student in "Mathematics 6A"
    Then the enrollment should be rejected with error "Class has reached capacity"
    And a waitlist option should be offered

Scenario: School year enrollment scoping
    Given a class "Mathematics 6A" exists for school year "2023-2024"
    And current school year is "2024-2025"
    When I attempt to enroll the student in the "2023-2024" class
    Then the enrollment should be rejected with error "Cannot enroll in previous school year classes"

Scenario: Bulk enrollment processing
    Given I have a list of 50 students to enroll in "Mathematics 6A"
    When I submit a bulk enrollment request
    Then all eligible students should be enrolled successfully
    And any failures should be reported with specific reasons
    And the bulk operation should be tracked with a correlation ID

Scenario: School-scoped access control
    Given I am authenticated as a SchoolUser for "Lincoln Elementary"
    And a student exists at "Roosevelt Middle School"
    When I attempt to enroll the student in a class at "Lincoln Elementary"
    Then the operation should be rejected with error "Insufficient permissions: cannot enroll students from other schools"