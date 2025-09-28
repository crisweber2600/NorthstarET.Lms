Feature: Create District
    As a PlatformAdmin
    I want to create new school districts
    So that I can provision tenant environments for educational organizations

Background:
    Given I am authenticated as a PlatformAdmin

Scenario: Create district with valid data
    When I create a district with the following details:
      | Field        | Value                          |
      | Slug         | oakland-unified                |
      | DisplayName  | Oakland Unified School District|
      | MaxStudents  | 50000                         |
      | MaxStaff     | 5000                          |
      | MaxAdmins    | 100                           |
    Then the district should be created successfully
    And I should automatically have DistrictAdmin rights for the district
    And the creation should be logged in the platform audit

Scenario: Reject duplicate district slug  
    Given a district with slug "existing-district" already exists
    When I create a district with slug "existing-district"
    Then the creation should be rejected with error "District slug already exists"

Scenario: Enforce quota limits
    When I create a district with MaxStudents of 150000
    Then the creation should be rejected with error "MaxStudents cannot exceed 100000"

Scenario: Validate district slug format
    When I create a district with the following invalid slugs:
      | InvalidSlug        | Reason                    |
      | UPPER-CASE        | Must be lowercase         |
      | spaces in slug    | Cannot contain spaces     |
      | invalid_underscore| Cannot contain underscores|
      | special#chars     | Cannot contain special chars|
    Then the creation should be rejected with appropriate format error

Scenario: Create district with minimum quota values
    When I create a district with the following details:
      | Field        | Value           |
      | Slug         | minimal-district|
      | DisplayName  | Minimal District|
      | MaxStudents  | 1               |
      | MaxStaff     | 1               |
      | MaxAdmins    | 1               |
    Then the district should be created successfully
    And the quotas should be set to the specified values

Scenario: Automatically create default retention policies
    When I create a district with slug "new-district"  
    Then the district should be created successfully
    And default retention policies should be created:
      | EntityType | RetentionYears |
      | Student    | 7              |
      | Staff      | 5              |
      | Assessment | 3              |

Scenario: Initialize audit chain for new tenant
    When I create a district with slug "new-district"
    Then the district should be created successfully
    And an audit chain genesis record should be created for the tenant
    And the record should have sequence number 1
    And the record hash should be properly initialized