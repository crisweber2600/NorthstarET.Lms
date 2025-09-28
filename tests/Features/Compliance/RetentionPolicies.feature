Feature: Data Retention Policy Management
    As a DistrictAdmin
    I want to manage data retention policies for my district
    So that I can ensure FERPA compliance and proper data lifecycle management

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the default FERPA retention periods are:
      | Entity Type     | Retention Years |
      | Student         | 7              |
      | Staff           | 5              |
      | Assessment      | 3              |
      | AuditRecord     | 10             |

Scenario: View default retention policies
    When I request the current retention policies
    Then I should see the following policies:
      | Entity Type     | Retention Years | Is Default | Effective Date |
      | Student         | 7              | true       | 2020-01-01     |
      | Staff           | 5              | true       | 2020-01-01     |
      | Assessment      | 3              | true       | 2020-01-01     |
      | AuditRecord     | 10             | true       | 2020-01-01     |
    And all policies should show "FERPA Compliance" as the source

Scenario: Create custom retention policy
    When I create a custom retention policy with:
      | Field              | Value                    |
      | Entity Type        | Student                  |
      | Retention Years    | 10                       |
      | Effective Date     | 2024-07-01              |
      | Reason             | Extended retention for research |
      | Supersedes Default | true                     |
    Then the policy should be created successfully
    And the policy should supersede the default policy effective "2024-07-01"
    And the creation should be audited with justification
    And I should receive a policy impact assessment

Scenario: Validate retention policy changes
    When I attempt to create a retention policy with:
      | Field              | Value      |
      | Entity Type        | Student    |
      | Retention Years    | 2          |
    Then the creation should be rejected with error "Retention period cannot be less than FERPA minimum of 7 years for Student records"

Scenario: Schedule automatic data purge
    Given a custom retention policy exists for "Student" records with 10 years retention
    And student "John Smith" graduated on "2014-06-15"
    And today's date is "2024-07-01"
    When the retention policy enforcement job runs
    Then student "John Smith" should be marked eligible for purge
    And a purge notification should be generated
    And the student record should not be immediately deleted

Scenario: Prevent purge for active legal hold
    Given student "Jane Doe" graduated on "2010-06-15"
    And the retention period for students is 7 years
    And an active legal hold exists on student "Jane Doe" for case "CASE-2023-001"
    When the retention policy enforcement job runs
    Then student "Jane Doe" should NOT be marked eligible for purge
    And the legal hold should be noted in the retention log
    And a legal hold reminder should be generated

Scenario: Execute approved data purge
    Given the following records are eligible for purge:
      | Entity Type | Entity ID    | Graduation Date | Days Overdue |
      | Student     | STU-2010-001 | 2010-06-15     | 365          |
      | Student     | STU-2011-002 | 2011-06-15     | 30           |
      | Staff       | EMP-2015-001 | 2015-12-31     | 180          |
    When I approve the purge for all eligible records
    And I provide purge authorization "PURGE-AUTH-2024-001"
    Then all approved records should be permanently deleted
    And purge confirmations should be generated for each record
    And the purge action should be audited with authorization details
    And purge certificates should be created for compliance records

Scenario: Handle purge errors gracefully
    Given student "STU-2012-001" is eligible for purge
    But the student has active enrollment records in another district
    When I attempt to purge the student record
    Then the purge should fail with error "Cannot purge: Active cross-district references exist"
    And the error should be logged in the retention management log
    And manual review should be flagged for the record

Scenario: Generate retention compliance report
    When I request a retention compliance report for the current year
    Then I should see:
      | Metric                    | Count |
      | Records Eligible for Purge| 234   |
      | Records with Legal Holds  | 12    |
      | Records Purged YTD        | 189   |
      | Policy Exceptions         | 5     |
    And I should see a breakdown by entity type
    And I should see upcoming purge eligibility dates
    And I should see any compliance risks or violations

Scenario: Create entity-specific retention exceptions
    Given student "Alex Brown" has special circumstances requiring extended retention
    When I create a retention exception with:
      | Field              | Value                    |
      | Entity Type        | Student                  |
      | Entity ID          | STU-2015-003            |
      | Extended Years     | 15                      |
      | Reason             | Ongoing legal case      |
      | Authorized By      | district-legal-counsel  |
      | Review Date        | 2025-12-31             |
    Then the exception should be created successfully
    And student "Alex Brown" should be excluded from standard purge schedules
    And the exception should be scheduled for review on the specified date
    And the exception should be audited

Scenario: Monitor retention policy changes impact
    Given I want to change the Student retention policy from 7 to 12 years
    When I request an impact assessment for this change
    Then I should see:
      | Metric                           | Current | After Change |
      | Records Currently Eligible       | 145     | 89          |
      | Storage Impact (GB)              | -2.1    | -1.3        |
      | Compliance Risk Score            | Low     | Lower       |
      | Annual Purge Volume Change       | 0       | -56         |
    And I should see affected record counts by year
    And I should see cost implications of the change

Scenario: Handle policy transition periods
    Given a new retention policy takes effect on "2024-07-01"
    And the policy extends Student retention from 7 to 10 years
    And some students were already eligible for purge under the old policy
    When the policy transition occurs
    Then previously eligible students should be re-evaluated under the new policy
    And students no longer eligible should be removed from purge queues
    And the policy transition should be audited
    And affected records should be tagged with transition metadata

Scenario: Validate policy hierarchy and conflicts
    Given the following retention policies exist:
      | Policy ID | Entity Type | Retention Years | Scope      | Priority |
      | POL-001   | Student     | 7              | Default    | 1        |
      | POL-002   | Student     | 10             | District   | 2        |
      | POL-003   | Student     | 12             | SpecialEd  | 3        |
    And a student "Maria Garcia" has special education records
    When I evaluate the effective retention policy for "Maria Garcia"
    Then the policy "POL-003" should be applied with 12 years retention
    And the policy hierarchy should be documented in the evaluation log
    And all applicable policies should be listed with their precedence