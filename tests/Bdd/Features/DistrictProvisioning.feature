Feature: District Provisioning
    As a PlatformAdmin
    I want to provision new district tenants
    So that they can onboard their users and data

Background:
    Given I am authenticated as a PlatformAdmin

Scenario: Provision new district successfully
    When I provision a district with slug "oakland-unified" and display name "Oakland Unified School District"
    Then the district should be created with status "Active"
    And the district schema "oakland_unified" should be created in the database
    And the district should have default quotas assigned
    And a DistrictProvisionedEvent should be raised
    And the provisioning should be audited

Scenario: Provision district with invalid slug
    When I attempt to provision a district with slug "Oakland Unified" and display name "Oakland Unified School District"
    Then the provisioning should fail with validation error "Slug must contain only lowercase letters, numbers, and hyphens"
    And no district should be created

Scenario: Provision duplicate district slug
    Given a district with slug "oakland-unified" already exists
    When I attempt to provision a district with slug "oakland-unified" and display name "Another District"
    Then the provisioning should fail with error "District slug must be unique"
    And no new district should be created

Scenario: Provision district with custom quotas
    When I provision a district with slug "san-jose-unified" and custom quotas:
        | Quota Type | Value |
        | Students   | 75000 |
        | Staff      | 7500  |
        | Admins     | 150   |
    Then the district should be created with the specified quotas
    And the custom quotas should override the default values

Scenario: Suspend district
    Given a district "oakland-unified" exists with status "Active"
    When I suspend the district with reason "Policy violation"
    Then the district status should change to "Suspended"
    And the suspension reason should be recorded
    And all active role assignments should be revoked
    And a DistrictSuspendedEvent should be raised

Scenario: Activate suspended district
    Given a district "oakland-unified" exists with status "Suspended"
    When I activate the district
    Then the district status should change to "Active"
    And previously suspended role assignments should not be automatically restored
    And a DistrictActivatedEvent should be raised