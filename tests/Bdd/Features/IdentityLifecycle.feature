Feature: Identity Lifecycle
    As a DistrictAdmin
    I want to manage identity mappings for users
    So that external identities are properly mapped to internal users

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the district "oakland-unified" is active

Scenario: Map new external identity to internal user
    Given a user "john.smith@oaklandschools.org" exists in the system
    When I map the external identity "john.smith@contoso.com" from issuer "entra-external-id" to the user
    Then the identity mapping should be created successfully
    And the mapping status should be "Active"
    And an IdentityMappedEvent should be raised
    And the mapping should be audited

Scenario: Map external identity with conflict detection
    Given a user "john.smith@oaklandschools.org" exists in the system
    And the external identity "john.smith@contoso.com" is already mapped to another user
    When I attempt to map the external identity "john.smith@contoso.com" from issuer "entra-external-id" to the user
    Then the mapping should fail with error "External identity already mapped to another user"
    And a IdentityConflictDetectedEvent should be raised
    And the conflict should be audited

Scenario: Update identity mapping after external change
    Given a user "john.smith@oaklandschools.org" has an active identity mapping
    And the external identity subject has changed in the issuer
    When the identity synchronization process runs
    Then the existing mapping should be updated
    And the old mapping should be marked as "Superseded"
    And an IdentityUpdatedEvent should be raised

Scenario: Suspend identity mapping
    Given a user "john.smith@oaklandschools.org" has an active identity mapping
    When I suspend the identity mapping with reason "Security review"
    Then the mapping status should change to "Suspended"
    And the user should not be able to authenticate via external identity
    And an IdentitySuspendedEvent should be raised

Scenario: Remove identity mapping on user deletion
    Given a user "john.smith@oaklandschools.org" has an active identity mapping
    When the user is deleted from the system
    Then the identity mapping should be marked as "Deleted"
    And the external identity should be available for remapping
    And an IdentityDeletedEvent should be raised

Scenario: Bulk identity import with error handling
    When I import identity mappings from a file with 100 records
    And 5 records have invalid external identities
    And 3 records have conflict with existing mappings
    Then 92 mappings should be created successfully
    And 8 mappings should fail with appropriate error messages
    And a bulk import report should be generated
    And all operations should be audited