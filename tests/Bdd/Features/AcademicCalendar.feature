Feature: Academic Calendar Validation
    As a DistrictAdmin
    I want to define and validate academic calendars
    So that school years have proper term and closure definitions

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And a school year "2024-2025" exists with dates from "2024-08-15" to "2025-06-15"

Scenario: Create academic calendar with valid terms
    When I create an academic calendar for the school year with terms:
        | Term Name     | Start Date | End Date   |
        | Fall Semester | 2024-08-15 | 2024-12-20 |
        | Spring Semester | 2025-01-08 | 2025-06-15 |
    Then the academic calendar should be created successfully
    And the terms should not overlap
    And all terms should be within the school year dates
    And an AcademicCalendarCreatedEvent should be raised

Scenario: Reject overlapping terms
    When I attempt to create an academic calendar with overlapping terms:
        | Term Name      | Start Date | End Date   |
        | Fall Semester  | 2024-08-15 | 2024-12-20 |
        | Winter Session | 2024-12-15 | 2025-01-15 |
        | Spring Semester | 2025-01-08 | 2025-06-15 |
    Then the calendar creation should fail with error "Terms cannot overlap"
    And no academic calendar should be created

Scenario: Reject terms outside school year
    When I attempt to create an academic calendar with terms:
        | Term Name     | Start Date | End Date   |
        | Summer Prep   | 2024-07-01 | 2024-08-10 |
        | Fall Semester | 2024-08-15 | 2024-12-20 |
    Then the calendar creation should fail with error "All terms must be within school year dates"
    And no academic calendar should be created

Scenario: Add school closures to calendar
    Given an academic calendar exists for the school year
    When I add school closures:
        | Closure Name      | Start Date | End Date   | Reason        |
        | Thanksgiving Break | 2024-11-25 | 2024-11-29 | Holiday       |
        | Winter Break      | 2024-12-21 | 2025-01-07 | Holiday       |
        | Spring Break      | 2025-03-24 | 2025-03-28 | Holiday       |
    Then the closures should be added to the calendar
    And the closures should override instructional days
    And a CalendarClosuresAddedEvent should be raised

Scenario: Calculate instructional days excluding closures
    Given an academic calendar exists with terms and closures
    When I request the instructional days count for "Fall Semester"
    Then the count should exclude weekends and closure days
    And the calculation should be accurate for attendance reporting

Scenario: Validate calendar completeness before school year activation
    Given a school year "2024-2025" is in "Draft" status
    And an incomplete academic calendar exists (missing spring term)
    When I attempt to activate the school year
    Then the activation should fail with error "Academic calendar must be complete"
    And the school year should remain in "Draft" status

Scenario: Archive calendar when school year is archived
    Given a school year "2023-2024" with a complete academic calendar
    When the school year is archived
    Then the academic calendar should be marked as archived
    And no modifications should be allowed to the archived calendar
    And an AcademicCalendarArchivedEvent should be raised