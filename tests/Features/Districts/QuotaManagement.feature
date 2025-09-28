Feature: District Quota Management
    As a PlatformAdmin  
    I want to manage district quotas
    So that I can control resource utilization and prevent system overload

Background:
    Given I am authenticated as a PlatformAdmin
    And a district "test-district" exists with default quotas

Scenario: View current quota utilization
    Given the district "test-district" has:
      | Type     | Limit | Current |
      | Students | 50000 | 12543   |
      | Staff    | 5000  | 1205    |
      | Admins   | 100   | 23      |
    When I request quota status for "test-district"
    Then I should see:
      | Type     | Limit | Current | Available | Utilization |
      | Students | 50000 | 12543   | 37457     | 25.1%       |
      | Staff    | 5000  | 1205    | 3795      | 24.1%       |
      | Admins   | 100   | 23      | 77        | 23.0%       |

Scenario: Increase district quotas
    When I update quotas for "test-district":
      | Type     | NewLimit |
      | Students | 75000    |
      | Staff    | 7500     |
      | Admins   | 200      |
    Then the quotas should be updated successfully
    And I should receive confirmation with new limits
    And the change should be audited with my user ID

Scenario: Decrease quotas when current usage allows
    Given the district "test-district" currently has:
      | Type     | Current |
      | Students | 1000    |
      | Staff    | 500     |
      | Admins   | 20      |
    When I decrease quotas to:
      | Type     | NewLimit |
      | Students | 2000     |
      | Staff    | 1000     |
      | Admins   | 50       |
    Then the quotas should be updated successfully

Scenario: Reject quota decrease below current usage
    Given the district "test-district" currently has:
      | Type     | Current |
      | Students | 15000   |
      | Staff    | 1500    |
      | Admins   | 75      |
    When I attempt to decrease quotas to:
      | Type     | NewLimit |
      | Students | 10000    |
      | Staff    | 1000     |
      | Admins   | 50       |
    Then the update should be rejected
    And I should see validation errors:
      | Type     | Error                                    |
      | Students | Cannot set limit below current usage: 15000 |
      | Staff    | Cannot set limit below current usage: 1500  |
      | Admins   | Cannot set limit below current usage: 75    |

Scenario: Enforce platform-wide quota limits
    When I attempt to set student quota to 500000
    Then the update should be rejected with error "Student quota cannot exceed platform maximum: 100000"

Scenario: Prevent quota manipulation by district admins
    Given I am authenticated as a DistrictAdmin for "test-district"
    When I attempt to update district quotas
    Then the operation should be rejected with error "Insufficient permissions: PlatformAdmin required"

Scenario: Monitor quota warnings and alerts
    Given the district "test-district" has quotas with 90% utilization:
      | Type     | Limit | Current |
      | Students | 50000 | 45000   |
      | Staff    | 5000  | 4500    |
    When I check quota status
    Then I should see warning indicators for high utilization
    And the system should have generated quota warning events

Scenario: Block operations at quota limits
    Given the district "test-district" has reached student quota:
      | Type     | Limit | Current |
      | Students | 50000 | 50000   |
    When a DistrictAdmin tries to create a new student
    Then the operation should fail with "Student quota exceeded"
    And the failure should be logged for quota monitoring

Scenario: Bulk quota adjustments for multiple districts
    Given I have multiple districts:
      | District    | CurrentStudentLimit |
      | district-a  | 10000              |
      | district-b  | 15000              |
      | district-c  | 25000              |
    When I apply bulk quota increase of 10000 students to all districts
    Then the quotas should be updated:
      | District    | NewStudentLimit |
      | district-a  | 20000          |
      | district-b  | 25000          |
      | district-c  | 35000          |
    And each update should be individually audited

Scenario: Quota history tracking
    Given the district "test-district" has had quota changes over time
    When I request quota change history
    Then I should see a chronological list of quota modifications
    And each entry should include:
      | Field           | Description                    |
      | Timestamp       | When the change was made       |
      | ChangedBy       | PlatformAdmin who made change  |
      | QuotaType       | Student, Staff, or Admin       |
      | OldValue        | Previous quota limit           |
      | NewValue        | Updated quota limit            |
      | Reason          | Optional reason for change     |

Scenario: Quota utilization reporting
    When I generate a quota utilization report for all districts
    Then I should receive aggregated data showing:
      | Metric                    | Value |
      | Total Districts           | 156   |
      | Average Student Utilization | 34.5% |
      | Average Staff Utilization   | 28.2% |
      | High Utilization Districts  | 12    |
      | Districts at Capacity       | 2     |