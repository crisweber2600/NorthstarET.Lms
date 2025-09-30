Feature: Legal Hold Retention
    As a DistrictAdmin or ComplianceOfficer
    I want to apply legal holds to prevent data deletion
    So that records are preserved during legal proceedings or investigations

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the following retention policies are active:
        | Entity Type | Retention Years | Grace Period Days |
        | Student     | 7               | 90                |
        | Staff       | 5               | 60                |
        | Assessment  | 3               | 30                |

Scenario: Apply legal hold to student record
    Given a student "STU-2024-001" exists with graduation date "2018-06-15"
    And the student record is eligible for retention purge
    When I apply a legal hold with reason "Title IX Investigation Case #2024-001"
    Then the legal hold should be created successfully
    And the student record should be protected from deletion
    And a LegalHoldAppliedEvent should be raised
    And the hold application should be audited

Scenario: Legal hold prevents automatic retention purge
    Given a student record has an active legal hold
    When the retention purge job runs
    Then the student record should be skipped from purge
    And the skip should be logged with legal hold reference
    And a RetentionPurgeSkippedEvent should be raised
    And the purge job should continue with other eligible records

Scenario: Release legal hold when case concludes
    Given a student record has an active legal hold
    When I release the legal hold with reason "Investigation concluded - no findings"
    Then the legal hold should be marked as "Released"
    And the student record should become eligible for retention processing
    And a LegalHoldReleasedEvent should be raised
    And the release should be audited

Scenario: Bulk apply legal holds for class action case
    Given multiple student records exist for a class action lawsuit:
        | Student Number | Name        | Graduation Year |
        | STU-2024-001   | John Smith  | 2018           |
        | STU-2024-002   | Mary Johnson| 2019           |
        | STU-2024-003   | David Brown | 2020           |
    When I apply bulk legal holds with reason "Class Action Lawsuit ABC vs District"
    Then all specified records should have legal holds applied
    And the holds should share the same case reference number
    And a BulkLegalHoldAppliedEvent should be raised

Scenario: Legal hold extends beyond normal retention period
    Given a student record has been under legal hold for 10 years
    And the normal retention period is 7 years
    When I check the record's retention status
    Then the record should still be protected by the legal hold
    And the extended retention should be justified by the hold
    And compliance reports should reflect the legal hold extension

Scenario: Automatic legal hold expiration
    Given a legal hold was applied with expiration date "2024-12-31"
    When the current date exceeds the expiration date
    And the automatic cleanup job runs
    Then the legal hold should be automatically released
    And the record should become eligible for normal retention processing
    And a LegalHoldExpiredEvent should be raised

Scenario: Legal hold audit trail immutability
    Given a legal hold exists on a student record
    When I attempt to modify the hold creation timestamp
    Then the modification should be rejected
    And the original audit record should remain unchanged
    And an attempted tampering event should be logged

Scenario: Compliance report includes legal hold data
    Given multiple records have active legal holds
    When I generate a compliance report for data retention
    Then the report should include:
        | Field                    | Description                                      |
        | Total Records on Hold    | Count of records with active legal holds        |
        | Oldest Hold Date         | Date of the oldest active legal hold            |
        | Hold Reasons             | Summary of hold reasons by category              |
        | Extended Retention Count | Records held beyond normal retention period      |

Scenario: Legal hold prevents data export restriction
    Given a student record has an active legal hold
    When an automated system attempts to restrict data export due to retention expiry
    Then the restriction should be blocked
    And the legal hold should take precedence
    And the system should log the precedence decision