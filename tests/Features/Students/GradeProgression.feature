Feature: Grade Progression Management
    As a DistrictAdmin
    I want to promote students to the next grade level
    So that I can manage academic progression across school years

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the following school years exist:
      | Year | Status    |
      | 2023 | Completed |
      | 2024 | Active    |
      | 2025 | Planning  |
    And a student "John Smith" with number "STU-2023-001" exists
    And the student is enrolled in grade "Grade5" for school year "2023-2024"

Scenario: Promote student to next grade level
    When I promote student "STU-2023-001" from grade "Grade5" to "Grade6"
    And I set the promotion date to "2024-06-15"
    Then the student's grade level should be updated to "Grade6"
    And the promotion should be recorded in the student's academic history
    And the promotion should be audited with user "district-admin" and timestamp

Scenario: Promote student across school years
    When I promote student "STU-2023-001" for the transition:
      | From School Year | To School Year | From Grade | To Grade |
      | 2023-2024       | 2024-2025     | Grade5     | Grade6   |
    Then a new enrollment should be created for school year "2024-2025"
    And the previous enrollment should be marked as "Completed"
    And the student's current grade level should be "Grade6"

Scenario: Prevent invalid grade progression
    When I attempt to promote student "STU-2023-001" from "Grade5" to "Grade3"
    Then the promotion should be rejected with error "Invalid grade progression: cannot demote student"

Scenario: Prevent promotion without completing current grade
    Given the student's current enrollment status is "Active"
    And the school year "2023-2024" status is "Active"
    When I attempt to promote student "STU-2023-001" to the next grade
    Then the promotion should be rejected with error "Cannot promote student before current school year is completed"

Scenario: Handle graduation promotion
    Given a student "Sarah Johnson" with number "STU-2023-002" exists
    And the student is enrolled in grade "Grade12" for school year "2023-2024"
    When I promote student "STU-2023-002" from grade "Grade12" to "Graduated"
    Then the student's status should be updated to "Graduated"
    And the graduation date should be recorded
    And no new enrollment should be created
    And the graduation should be audited

Scenario: Mass grade promotion preview
    Given the following students are enrolled in "Grade5" for "2023-2024":
      | Student Number | Student Name    | Status |
      | STU-2023-003  | Mike Davis      | Active |
      | STU-2023-004  | Lisa Brown      | Active |
      | STU-2023-005  | Tom Wilson      | Withdrawn |
    When I request a grade promotion preview from "Grade5" to "Grade6"
    And I set the target school year to "2024-2025"
    Then the preview should show 2 eligible students for promotion
    And student "STU-2023-005" should be excluded due to "Withdrawn" status
    And the preview should include promotion validation results