Feature: Assessment Management
    As a DistrictAdmin
    I want to manage district assessment definitions
    So that I can provide standardized assessments across schools

Background:
    Given I am authenticated as a DistrictAdmin for "oakland-unified"
    And the current school year is "2024-2025"

Scenario: Create new assessment definition
    When I create an assessment with the following details:
      | Field          | Value                    |
      | Name           | Math Grade 6 Midterm    |
      | Version        | 1.0                      |
      | Subject        | Mathematics              |
      | GradeLevel     | Grade6                   |
      | MaxFileSize    | 50MB                     |
    Then the assessment should be created successfully
    And the assessment should be assigned a unique identifier
    And the assessment should be marked as "Active"
    And the creation should be audited

Scenario: Assessment versioning and immutability
    Given an assessment "Math Grade 6 Midterm v1.0" exists
    When I attempt to modify the assessment name directly
    Then the operation should be rejected with error "Assessment definitions are immutable"
    When I create a new version "Math Grade 6 Midterm v2.0"
    Then both versions should exist in the system
    And v1.0 should remain accessible for historical records

Scenario: School year pinning for assessments
    Given an assessment "State Reading Test" exists
    When I pin the assessment to school year "2024-2025"
    Then the assessment should only be visible during that school year
    And teachers should be able to access it for the entire academic year
    And it should be archived when the school year ends

Scenario: District-scoped assessment access
    Given assessments exist in multiple districts:
      | District        | Assessment Name       |
      | oakland-unified | Oakland Math Test     |
      | san-jose-unified| San Jose Math Test    |
    When I query assessments as a user from "oakland-unified"
    Then I should only see "Oakland Math Test"
    And I should not have access to "San Jose Math Test"

Scenario: Assessment PDF file management
    Given an assessment "Science Lab Report Template" exists
    When I upload a PDF file of 75MB
    Then the upload should be rejected with error "File size exceeds 100MB limit"
    When I upload a PDF file of 25MB
    Then the file should be stored successfully
    And I should receive a scoped, expiring URL for file access
    And the URL should expire after 24 hours

Scenario: RBAC enforcement for assessment operations
    Given I am authenticated as a "Teacher" for "Lincoln Elementary"
    When I attempt to create a new assessment definition
    Then the operation should be rejected with error "Insufficient permissions: only DistrictAdmin can create assessments"
    When I attempt to view existing assessment "Math Grade 6 Midterm"
    Then I should have read-only access to the assessment details
    And I should be able to access associated PDF files

Scenario: Assessment deletion with constraint checking
    Given an assessment "History Final Exam" is being used in active classes
    When I attempt to delete the assessment
    Then the operation should be rejected with error "Cannot delete assessment: currently in use"
    Given the assessment is not being used in any classes
    When I delete the assessment
    Then it should be marked as "Deleted" rather than removed
    And historical references should remain intact

Scenario: District storage quota enforcement
    Given the district "oakland-unified" has used 9.8GB of the 10GB assessment storage limit
    When I attempt to upload a 500MB assessment PDF
    Then the upload should be rejected with error "District storage quota exceeded"
    And I should be provided with current usage statistics
    And suggestions for freeing up space should be displayed