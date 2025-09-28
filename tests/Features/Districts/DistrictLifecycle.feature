Feature: District Lifecycle Management
    As a PlatformAdmin
    I want to manage district lifecycle states
    So that I can enforce compliance and operational policies

Background:
    Given I am authenticated as a PlatformAdmin
    And a district "test-district" exists with status "Active"

Scenario: Suspend district for policy violation
    When I suspend the district "test-district" with reason "Data breach investigation"
    Then the district status should be "Suspended"
    And all district users should lose access immediately
    And the suspension should be logged in the platform audit
    And the suspension reason should be recorded

Scenario: Reactivate suspended district
    Given the district "test-district" has status "Suspended"
    When I reactivate the district "test-district" with reason "Investigation completed"
    Then the district status should be "Active"
    And district users should regain access
    And the reactivation should be logged in the platform audit

Scenario: Cannot perform operations on suspended district
    Given the district "test-district" has status "Suspended"
    When a DistrictAdmin attempts to create a student in "test-district"
    Then the operation should be rejected with error "District is suspended"

Scenario: Schedule district for deletion
    Given the district "test-district" has no active legal holds
    And all retention periods have been met
    When I delete the district "test-district"
    Then the district status should be "PendingDeletion"
    And a deletion job should be scheduled
    And the deletion should be logged in the platform audit

Scenario: Cannot delete district with active retention requirements
    Given the district "test-district" has students with retention periods not yet met
    When I attempt to delete the district "test-district"
    Then the deletion should be rejected
    And I should receive details about retention requirements:
      | Type           | Count | RemainingDays |
      | Student records| 1250  | 245          |
      | Staff records  | 145   | 67           |

Scenario: Cannot delete district with legal holds
    Given the district "test-district" has 12 student records under legal hold
    When I attempt to delete the district "test-district"
    Then the deletion should be rejected with error "Cannot delete district with active legal holds"
    And I should receive details about the legal hold constraints

Scenario: Monitor quota utilization  
    Given the district "test-district" has quotas:
      | Type     | Limit | Current |
      | Students | 50000 | 45000   |
      | Staff    | 5000  | 4800    |
      | Admins   | 100   | 95      |
    When I check the district quota status
    Then I should see utilization percentages:
      | Type     | Utilization |
      | Students | 90%         |
      | Staff    | 96%         |
      | Admins   | 95%         |

Scenario: Prevent operations when quota exceeded
    Given the district "test-district" has reached its student quota limit
    When a DistrictAdmin attempts to create a new student
    Then the operation should be rejected with error "Student quota exceeded"
    And the rejection should be logged in the audit trail

Scenario: Update district quotas
    When I update the district "test-district" quotas:
      | Type     | NewLimit |
      | Students | 75000    |
      | Staff    | 7500     |
      | Admins   | 150      |
    Then the quotas should be updated successfully
    And the change should be logged in the platform audit
    And existing records above old limits should remain valid

Scenario: Archive completed district
    Given the district "test-district" has status "PendingDeletion"
    And all retention periods have been processed
    And no legal holds remain active
    When the automated archival process runs
    Then the district should be archived
    And all tenant-scoped data should be purged
    And the archival should be logged in the platform audit