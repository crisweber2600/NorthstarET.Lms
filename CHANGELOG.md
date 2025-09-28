# Changelog

All notable changes to the NorthstarET.Lms project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- TenantId property to TenantScopedEntity base class for multi-tenant data isolation
- GetCurrentTenantId method to ITenantContextAccessor interface and implementation
- MiddleName and WithdrawalReason properties to Student entity
- CompletionDate and CompletionReason properties to Enrollment entity
- Action, Details, IpAddress, and Hash properties to AuditRecord entity
- GetPermissions() method to RoleDefinition entity
- AssignedDate, AssignedBy, and IsActive properties to RoleAssignment entity
- CreatedAt property mapping to DistrictTenant entity
- Dual constructor support for AuditRecord (string and enum parameters)
- SetHash method to AuditRecord for hash chain management

### Changed
- Student constructor calls in StudentService to use proper enrollment dates instead of grade level
- DistrictService logic to properly construct DistrictTenant with required quotas parameter
- RoleAssignment constructor to support nullable SchoolId for district-level role assignments
- Student.Withdraw() method call to use correct parameter count
- RoleAuthorizationService nullable Guid handling for proper type safety
- Student.FullName property to include MiddleName when present
- Enrollment.Graduate() method to set both WithdrawalDate and CompletionDate for backward compatibility

### Fixed
- All 128 domain tests now passing (was 127 passing, 1 failing)
- 27 compilation errors in domain layer resolved
- Constructor parameter mismatches between DTOs and domain entities
- Missing property accessors causing compilation failures in application services
- Type conversion issues in audit record creation
- Null reference handling in role assignment validation

### Technical Debt Resolved
- Domain entity property completeness for application service integration
- Interface contract fulfillment between application and domain layers
- Constructor signature alignment across service boundaries
- Multi-tenant context accessibility throughout the application stack

## Test Results
- **Domain Tests**: 128/128 passing ✅
- **Application Tests**: Pending Moq package resolution
- **Integration Tests**: Not yet implemented
- **BDD Feature Tests**: Not yet implemented

## Architecture Compliance
- ✅ Clean Architecture dependency rules maintained
- ✅ Domain layer remains pure with zero external dependencies  
- ✅ Multi-tenant isolation preserved through TenantScopedEntity
- ✅ BDD-first testing structure preserved (tests passing for implemented features)
- ✅ No contamination of domain layer with infrastructure concerns