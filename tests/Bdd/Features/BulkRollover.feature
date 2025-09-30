Feature: Bulk Student Rollover
    As a DistrictAdmin
    I want to perform bulk student rollovers between school years
    So that students advance grades efficiently with proper data integrity

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And school year "2023-2024" is archived
    And school year "2024-2025" is active
    And the following students exist in "2023-2024":
        | Student Number | First Name | Last Name | Current Grade |
        | STU-2024-001   | John       | Smith     | 5             |
        | STU-2024-002   | Mary       | Johnson   | 5             |
        | STU-2024-003   | David      | Brown     | 8             |
        | STU-2024-004   | Sarah      | Davis     | 12            |

Scenario: Preview bulk rollover operation
    When I request a rollover preview for Grade 5 students to Grade 6
    Then the preview should show:
        | Student Number | Current Grade | Target Grade | Action    |
        | STU-2024-001   | 5             | 6            | Promote   |
        | STU-2024-002   | 5             | 6            | Promote   |
    And no actual data changes should occur
    And the preview should include impact summary
    And a BulkRolloverPreviewEvent should be raised

Scenario: Execute bulk rollover with all-or-nothing error handling
    Given I have reviewed the rollover preview
    When I execute the bulk rollover with "all-or-nothing" error handling
    And all validations pass
    Then all Grade 5 students should be promoted to Grade 6 in the new school year
    And enrollments should be created for the new school year
    And the previous year enrollments should remain unchanged
    And a BulkRolloverCompletedEvent should be raised
    And all operations should be audited

Scenario: Execute bulk rollover with best-effort error handling
    Given the rollover includes students with validation issues:
        | Student Number | Issue                    |
        | STU-2024-003   | Missing required documents |
    When I execute the bulk rollover with "best-effort" error handling
    Then valid students should be promoted successfully
    And invalid students should be skipped with error records
    And the operation should complete with partial success
    And a detailed error report should be generated

Scenario: Execute bulk rollover with threshold error handling
    Given I set the failure threshold to 10%
    When I execute the bulk rollover and 15% of students fail validation
    Then the entire operation should be rolled back
    And no students should be promoted
    And a BulkRolloverFailedEvent should be raised
    And the failure should be audited with threshold exceeded reason

Scenario: Handle graduating students in rollover
    When I execute a rollover that includes Grade 12 students
    Then Grade 12 students should be marked as "Graduated" instead of promoted
    And their final transcripts should be flagged for processing
    And they should not receive enrollments in the new school year
    And graduation records should be created

Scenario: Monitor bulk rollover progress
    Given a bulk rollover operation is in progress
    When I check the operation status
    Then I should see:
        | Field          | Value    |
        | Status         | Running  |
        | Progress       | 45%      |
        | Success Count  | 450      |
        | Failure Count  | 5        |
        | Estimated Time | 2 minutes |

Scenario: Cancel running bulk rollover
    Given a bulk rollover operation is in progress
    When I cancel the operation
    Then the operation should stop processing new records
    And completed promotions should remain in place
    And a BulkRolloverCancelledEvent should be raised
    And the cancellation should be audited

Scenario: Retry failed bulk rollover
    Given a bulk rollover operation failed due to system error
    When I retry the operation
    Then only the failed records should be reprocessed
    And previously successful records should be skipped
    And the retry should be tracked as a continuation of the original operation