Feature: Bulk Student Rollover
    As a DistrictAdmin
    I want to perform bulk student rollovers to the next school year
    So that I can efficiently transition students between academic years

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the following school years exist:
      | Year | Status    | Start Date | End Date   |
      | 2023 | Completed | 2023-08-15 | 2024-06-15 |
      | 2024 | Active    | 2024-08-15 | 2025-06-15 |
    And the following schools exist:
      | School Code | School Name        | Type        |
      | ELEM-001   | Lincoln Elementary | Elementary  |
      | ELEM-002   | Washington Elementary | Elementary |

Scenario: Preview bulk rollover for school
    Given the following students are enrolled at "Lincoln Elementary" for "2023-2024":
      | Student Number | Name           | Current Grade | Status    |
      | STU-2023-001  | Alice Johnson  | Grade3       | Active    |
      | STU-2023-002  | Bob Martinez   | Grade4       | Active    |
      | STU-2023-003  | Carol Davis    | Grade5       | Withdrawn |
      | STU-2023-004  | David Brown    | Grade5       | Active    |
    When I request a bulk rollover preview with the following configuration:
      | Field              | Value       |
      | From School Year   | 2023-2024   |
      | To School Year     | 2024-2025   |
      | School Filter      | ELEM-001    |
      | Exclude Withdrawn  | true        |
      | Auto Grade Advance | true        |
    Then the preview should show:
      | Student Number | Current Grade | Target Grade | Status    | Action   |
      | STU-2023-001  | Grade3       | Grade4       | Eligible  | Promote  |
      | STU-2023-002  | Grade4       | Grade5       | Eligible  | Promote  |
      | STU-2023-004  | Grade5       | Grade6       | Eligible  | Promote  |
    And student "STU-2023-003" should be excluded with reason "Withdrawn status"
    And the preview should be valid for 30 minutes

Scenario: Execute bulk rollover with preview
    Given I have a valid rollover preview "PREVIEW-2024-001"
    And the preview contains 150 eligible students
    When I execute the bulk rollover with preview "PREVIEW-2024-001"
    And I provide confirmation token "CONFIRM-ROLLOVER-2024-001"
    Then the rollover job should be queued with job ID
    And the job status should be "Processing"
    And I should receive an estimated completion time
    And all changes should be wrapped in a single audit correlation ID

Scenario: Monitor bulk rollover progress
    Given a bulk rollover job "JOB-ROLLOVER-2024-001" is processing
    When I check the job status for "JOB-ROLLOVER-2024-001"
    Then I should see:
      | Field              | Value    |
      | Status            | Processing |
      | Total Students    | 150      |
      | Processed         | 75       |
      | Successful        | 73       |
      | Failed            | 2        |
      | Progress Percent  | 50       |
    And I should see any error details for failed promotions

Scenario: Handle rollover errors gracefully
    Given the following students exist for rollover:
      | Student Number | Current Grade | Issue                    |
      | STU-2023-010  | Grade5       | Already enrolled in 2024 |
      | STU-2023-011  | Grade12      | No Grade13 exists        |
      | STU-2023-012  | Grade4       | None                     |
    When I execute a bulk rollover including these students
    Then student "STU-2023-010" should fail with error "Duplicate enrollment detected"
    And student "STU-2023-011" should be marked for graduation instead of promotion
    And student "STU-2023-012" should be promoted successfully
    And the rollover should continue processing despite individual failures

Scenario: Rollover with custom grade transitions
    Given I configure custom grade transitions:
      | From Grade | To Grade  | Special Rule |
      | PreK       | K         | Standard     |
      | K          | Grade1    | Standard     |
      | Grade5     | Grade6    | Middle School Transition |
      | Grade8     | Grade9    | High School Transition   |
    When I execute bulk rollover with these custom transitions
    Then students should be promoted according to the custom rules
    And middle school transitions should include additional metadata
    And high school transitions should trigger enrollment verification

Scenario: Rollover with enrollment capacity limits
    Given the following class capacity constraints for "2024-2025":
      | Grade Level | Available Seats | Current Enrollments |
      | Grade1      | 25             | 22                 |
      | Grade2      | 30             | 28                 |
      | Grade6      | 120            | 115                |
    And I have 28 students to promote from K to Grade1
    When I execute the bulk rollover
    Then 25 students should be successfully enrolled in Grade1
    And 3 students should be placed on a waiting list
    And I should receive a capacity warning report

Scenario: Validate rollover prerequisites
    Given the target school year "2024-2025" has status "Planning"
    And some required classes for "2024-2025" are not yet created
    When I attempt to execute a bulk rollover to "2024-2025"
    Then the rollover should be rejected with error "Target school year is not ready for enrollment"
    And I should receive a list of missing prerequisites:
      | Prerequisite Type | Description              |
      | School Year Status| Must be Active or Ready  |
      | Class Schedules   | 5 grade levels missing   |
      | Staff Assignments | Teachers not assigned    |