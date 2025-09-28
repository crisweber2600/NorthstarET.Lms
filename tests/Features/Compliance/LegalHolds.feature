Feature: Legal Hold Management  
    As a DistrictAdmin
    I want to place and manage legal holds on student and staff records
    So that I can preserve data during legal proceedings and investigations

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the following records exist:
      | Type    | ID           | Name           | Status    |
      | Student | STU-2020-001 | Alice Johnson  | Active    |
      | Student | STU-2018-002 | Bob Martinez   | Graduated |
      | Staff   | EMP-2019-001 | Carol Davis    | Active    |
      | Staff   | EMP-2017-002 | David Brown    | Terminated|

Scenario: Place legal hold on student record
    When I place a legal hold with:
      | Field            | Value                    |
      | Entity Type      | Student                  |
      | Entity ID        | STU-2020-001            |
      | Case Reference   | CASE-2024-001           |
      | Reason           | Disciplinary investigation |
      | Requesting Party | District Legal Counsel   |
      | Expected Duration| 6 months                |
      | Scope            | Full Record             |
    Then the legal hold should be placed successfully
    And the student record should be protected from deletion
    And the hold should be audited with full justification
    And automatic notifications should be sent to data stewards

Scenario: Prevent data purge for held records
    Given student "STU-2018-002" has graduated and is eligible for purge
    And a legal hold "HOLD-2024-001" exists on student "STU-2018-002"
    When the automatic data retention job runs
    Then student "STU-2018-002" should be excluded from purge processing
    And the legal hold should be referenced in the purge exemption log
    And a hold impact report should be generated

Scenario: Place hold on multiple related records
    Given a legal case involves student "Alice Johnson" and staff member "Carol Davis"
    When I place a legal hold with:
      | Field               | Value                           |
      | Case Reference      | CASE-2024-002                  |
      | Entities            | STU-2020-001, EMP-2019-001     |
      | Reason              | Title IX Investigation         |
      | Hold Scope          | Communications, Disciplinary    |
      | Authorized By       | Title IX Coordinator           |
      | Review Date         | 2024-12-31                     |
    Then legal holds should be placed on both entities
    And cross-references should link the related holds
    And both records should be preserved with specified scope
    And the coordinated hold should be audited as a single action

Scenario: Validate legal hold authorization
    When I attempt to place a legal hold without proper authorization:
      | Field            | Value                    |
      | Entity Type      | Student                  |
      | Entity ID        | STU-2020-001            |
      | Reason           | Personal request         |
      | Authorized By    | teacher-user-123         |
    Then the hold request should be rejected with error "Insufficient authorization: Legal holds require District Legal Counsel or Title IX Coordinator approval"

Scenario: Update legal hold scope
    Given an active legal hold "HOLD-2024-001" exists on student "STU-2020-001"
    And the current scope is "Full Record"
    When I update the hold scope to:
      | Field       | Value                              |
      | New Scope   | Disciplinary Records, Assessments  |
      | Reason      | Narrowed investigation focus       |
      | Updated By  | district-legal-counsel             |
    Then the hold scope should be updated successfully
    And the change should be audited with before/after scope details
    And data stewards should be notified of the scope change

Scenario: Release legal hold
    Given an active legal hold "HOLD-2024-003" exists on staff "EMP-2019-001"
    And the case has been resolved
    When I release the legal hold with:
      | Field             | Value                    |
      | Release Reason    | Case closed - no action  |
      | Released By       | district-legal-counsel   |
      | Release Date      | 2024-08-15              |
      | Certificate Needed| true                     |
    Then the legal hold should be released successfully
    And the staff record should return to normal retention schedules
    And a release certificate should be generated
    And the release should be audited with full details

Scenario: Handle expired legal holds
    Given the following legal holds exist:
      | Hold ID        | Entity ID    | Review Date | Status  |
      | HOLD-2024-004  | STU-2019-001 | 2024-06-30  | Active  |
      | HOLD-2024-005  | EMP-2018-001 | 2024-07-15  | Active  |
    And today's date is "2024-08-01"
    When the legal hold review job runs
    Then both holds should be flagged for review
    And notifications should be sent to legal counsel
    And the holds should remain active until explicitly reviewed
    And expiration alerts should be audited

Scenario: Generate legal hold compliance report
    When I request a legal hold compliance report
    Then I should see:
      | Metric                    | Count |
      | Active Legal Holds        | 7     |
      | Holds Pending Review      | 2     |
      | Holds Released This Year  | 12    |
      | Records Under Hold        | 15    |
    And I should see a breakdown by case type:
      | Case Type              | Active Holds |
      | Disciplinary           | 3           |
      | Title IX               | 2           |
      | Employment Dispute     | 1           |
      | Student Injury         | 1           |
    And I should see upcoming review dates and expiration warnings

Scenario: Search records by legal hold status
    Given the following legal holds are active:
      | Entity ID    | Case Reference | Hold Scope        |
      | STU-2020-001 | CASE-2024-001 | Full Record       |
      | EMP-2019-001 | CASE-2024-002 | Communications    |
      | STU-2019-002 | CASE-2024-003 | Disciplinary Only |
    When I search for all records with active legal holds
    Then I should see all 3 held records with their hold details
    And I should be able to filter by case reference
    And I should be able to filter by hold scope
    And search results should include hold placement and review dates

Scenario: Audit legal hold chain of custody
    Given legal hold "HOLD-2024-007" was placed on student "STU-2020-005"
    And the hold has been updated twice and released once
    When I request the chain of custody for the hold
    Then I should see the complete audit trail:
      | Action      | Date       | User                   | Details              |
      | Placed      | 2024-03-15 | district-legal-counsel | CASE-2024-007        |
      | Updated     | 2024-05-20 | district-legal-counsel | Scope narrowed       |
      | Updated     | 2024-07-10 | title-ix-coordinator   | Extended review date |
      | Released    | 2024-08-05 | district-legal-counsel | Case concluded       |
    And each action should include full justification and authorization
    And the trail should be tamper-evident with cryptographic signatures

Scenario: Handle conflicting hold requests
    Given an active legal hold exists on student "STU-2020-006" for "CASE-2024-008"
    When I attempt to place a conflicting hold for "CASE-2024-009" with different scope
    Then the system should detect the conflict
    And I should receive a conflict resolution prompt:
      | Option                  | Description                        |
      | Merge Holds            | Combine scopes and references      |
      | Escalate to Supervisor | Require additional authorization   |
      | Override Previous      | Replace with new hold (dangerous)  |
    And no action should be taken until conflict is resolved

Scenario: Bulk legal hold operations
    Given a class-action lawsuit involves multiple students:
      | Student ID   | Name           | Graduation Year |
      | STU-2020-010 | Emma Wilson    | 2024           |
      | STU-2021-011 | Frank Miller   | 2025           |
      | STU-2022-012 | Grace Lee      | 2026           |
    When I place a bulk legal hold with:
      | Field            | Value                    |
      | Case Reference   | CASE-2024-CLASS-001     |
      | Entity Count     | 3 students              |
      | Hold Scope       | Full Record             |
      | Batch Operation  | true                    |
    Then legal holds should be placed on all 3 students simultaneously
    And a batch operation audit record should be created
    And individual hold records should reference the batch operation
    And a bulk hold summary report should be generated