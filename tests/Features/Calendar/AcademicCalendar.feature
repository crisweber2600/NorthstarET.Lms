Feature: Academic Calendar Management
    As a DistrictAdmin
    I want to manage academic calendars for school years
    So that I can define term schedules and school closures for my district

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And a school year "2024-2025" exists with dates "2024-08-15" to "2025-06-15"

Scenario: Create academic calendar for school year
    When I create an academic calendar for school year "2024-2025" with:
      | Field        | Value                |
      | Calendar Name| Standard Calendar    |
      | Term Type    | Semester             |
    Then the academic calendar should be created successfully
    And the calendar should be associated with school year "2024-2025"
    And the creation should be audited

Scenario: Add terms to academic calendar
    Given an academic calendar exists for school year "2024-2025"
    When I add the following terms:
      | Term Name     | Start Date | End Date   | Sequence |
      | Fall Semester | 2024-08-15 | 2024-12-20 | 1        |
      | Spring Semester| 2025-01-08| 2025-06-15 | 2        |
    Then both terms should be added successfully
    And the terms should be ordered by sequence number
    And each term addition should be audited

Scenario: Add school closure to academic calendar  
    Given an academic calendar exists for school year "2024-2025"
    When I add a closure with:
      | Field       | Value                    |
      | Name        | Thanksgiving Break       |
      | Start Date  | 2024-11-25              |
      | End Date    | 2024-11-29              |
      | Is Recurring| false                    |
    Then the closure should be added to the calendar
    And the closure should not overlap with any terms
    And the closure should be audited

Scenario: Validate term date ranges
    Given an academic calendar exists for school year "2024-2025"
    When I attempt to add a term with:
      | Term Name   | Start Date | End Date   |
      | Invalid Term| 2025-07-01 | 2025-08-15 |
    Then the term creation should be rejected with error "Term dates must fall within school year range"

Scenario: Prevent overlapping terms
    Given an academic calendar exists for school year "2024-2025"
    And a term "Fall Semester" exists from "2024-08-15" to "2024-12-20"
    When I attempt to add a term "Overlapping Term" from "2024-10-01" to "2025-02-01"
    Then the term creation should be rejected with error "Term dates cannot overlap with existing terms"

Scenario: Add recurring closure
    Given an academic calendar exists for school year "2024-2025"
    When I add a recurring closure with:
      | Field          | Value              |
      | Name           | Professional Development Day |
      | Start Date     | 2024-09-15        |
      | End Date       | 2024-09-15        |
      | Is Recurring   | true              |
      | Recurrence Rule| Monthly, 3rd Friday |
    Then the closure should be marked as recurring
    And future occurrences should be automatically generated
    And all occurrences should be within the school year bounds

Scenario: View calendar overview
    Given an academic calendar exists for school year "2024-2025"
    And the following terms exist:
      | Term Name       | Start Date | End Date   |
      | Fall Semester   | 2024-08-15 | 2024-12-20 |
      | Spring Semester | 2025-01-08 | 2025-06-15 |
    And the following closures exist:
      | Name              | Start Date | End Date   |
      | Thanksgiving Break| 2024-11-25 | 2024-11-29 |
      | Winter Break      | 2024-12-21 | 2025-01-07 |
    When I request the calendar overview for "2024-2025"
    Then I should see all terms and closures sorted chronologically
    And the overview should show instructional days count for each term
    And the overview should highlight any date conflicts

Scenario: Copy calendar from previous year
    Given an academic calendar exists for school year "2023-2024"
    And the calendar has 2 terms and 5 closures defined
    When I copy the calendar structure to school year "2024-2025"
    And I adjust all dates by 365 days forward
    Then a new calendar should be created for "2024-2025"
    And all terms should be copied with adjusted dates
    And all non-date-specific closures should be copied
    And the copy operation should be audited

Scenario: Validate calendar completeness
    Given an academic calendar exists for school year "2024-2025"
    And the calendar has no terms defined
    When I validate the calendar completeness
    Then the validation should fail with error "Academic calendar must have at least one term"
    And I should receive a validation report with missing requirements

Scenario: Lock calendar after school year starts
    Given an academic calendar exists for school year "2024-2025"
    And the school year status is "Active" 
    And today's date is after the school year start date
    When I attempt to modify the Fall Semester dates
    Then the modification should be rejected with error "Cannot modify calendar after school year has started"
    And the calendar should remain unchanged