
# Implementation Plan: Foundational LMS with Tenant Isolation and Compliance

**Branch**: `001-foundational-lms-with` | **Date**: December 19, 2024 | **Spec**: [spec.md](/Users/cris/git/NorthstarET.Lms/specs/001-foundational-lms-with/spec.md)
**Input**: Feature specification from `/specs/001-foundational-lms-with/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → ✅ COMPLETE: Feature spec loaded with 54 functional requirements
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → ✅ COMPLETE: .NET 9 web application with Aspire orchestration
   → ✅ COMPLETE: Multi-tenant LMS with RBAC, compliance, and integration requirements
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, .github/copilot-instructions.md
7. Re-evaluate Constitution Check section
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Foundational Learning Management System with strict tenant isolation by District, comprehensive RBAC supporting flexible staff roles, academic calendar and enrollment lifecycle management, compliance features (audit, retention, legal holds), bulk operations, and Entra External ID integration. Technical approach: .NET 9 web application with Clean Architecture, Aspire orchestration, multi-tenant data isolation, and BDD-first development.

## Technical Context
**Language/Version**: .NET 9 with C# 13  
**Primary Dependencies**: ASP.NET Core, Entity Framework Core, .NET Aspire, Reqnroll, xUnit  
**Storage**: Multi-tenant SQL Server with strict data isolation per District  
**Testing**: Reqnroll for BDD, xUnit for unit/integration tests, Testcontainers for infrastructure tests  
**Target Platform**: Cloud-hosted web application (Azure/AWS containers)
**Project Type**: web - ASP.NET Core API with potential future web UI  
**Performance Goals**: <200ms p95 for CRUD operations, <120s for bulk operations (10k records), <2s for audit queries (1M records)  
**Constraints**: FERPA compliance, strict tenant isolation, tamper-evident audit logs, deny-by-default RBAC  
**Scale/Scope**: Multi-district (hundreds), multi-school (thousands), students/staff (tens of thousands per district)

## Bulk Operations Architecture

### Error Handling Strategies (FR-033)
The system implements four user-selectable strategies for bulk import operations:

1. **All-or-Nothing Strategy**
   - Implementation: Single database transaction wrapping entire import
   - Rollback: Complete rollback on any validation failure
   - Use Case: Critical data imports requiring perfect consistency

2. **Best-Effort Strategy**
   - Implementation: Individual record transactions with error collection
   - Rollback: Per-record rollback, continue with valid records
   - Use Case: Daily data synchronization with acceptable partial imports

3. **Threshold-Based Strategy**
   - Implementation: Running error rate calculation with configurable limits
   - Rollback: Complete rollback if error rate exceeds threshold (default 5%)
   - Use Case: Large imports where some errors are expected but not excessive

4. **Preview Mode**
   - Implementation: Validation-only pass with detailed change report
   - Rollback: No data changes, comprehensive preview generation
   - Use Case: Pre-import validation and change impact assessment

### Technical Implementation
- **Service**: `BulkOperationService` in Application layer
- **Strategy Pattern**: Pluggable error handlers per import type
- **Progress Tracking**: Real-time progress reports with correlation IDs
- **Audit Trail**: All strategies generate audit records for compliance

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**BDD-First Testing**: ✅ COMPLIANT - Complete Reqnroll feature files will be written mapping all 54 functional requirements to Given-When-Then scenarios before any code implementation.

**TDD Red-Green Cycle**: ✅ COMPLIANT - All domain entities, application services, and infrastructure components will follow strict TDD with failing tests written first and >90% coverage maintained.

**Clean Architecture**: ✅ COMPLIANT - Four-layer architecture with Domain (entities, value objects), Application (use cases, interfaces), Infrastructure (EF Core, external APIs), and Presentation (ASP.NET Core controllers). Domain layer has zero external dependencies.

**Aspire Orchestration**: ✅ COMPLIANT - All services orchestrated using .NET Aspire with service discovery for multi-tenant database connections, external API integrations (Entra ID), and background services for audit/retention jobs.

**Feature Specification Completeness**: ✅ COMPLIANT - Complete specification exists with 7 acceptance scenarios, 54 functional requirements, 4 performance requirements, edge cases defined, and 5 clarification sessions completed.

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
src/
├── NorthstarET.Lms.Domain/              # Clean Architecture Domain Layer
│   ├── Entities/                        # Aggregate roots and entities
│   │   ├── DistrictTenant.cs
│   │   ├── SchoolYear.cs
│   │   ├── AcademicCalendar.cs
│   │   ├── School.cs
│   │   ├── Class.cs
│   │   ├── Staff.cs
│   │   ├── Student.cs
│   │   ├── Guardian.cs
│   │   ├── AssessmentDefinition.cs
│   │   ├── IdentityMapping.cs
│   │   ├── RetentionPolicy.cs
│   │   ├── LegalHold.cs
│   │   └── AuditRecord.cs
│   ├── ValueObjects/                    # Value objects and enums
│   │   ├── UserId.cs
│   │   ├── ExternalId.cs
│   │   ├── EnrollmentStatus.cs
│   │   ├── GradeLevel.cs
│   │   └── SecurityAlert.cs
│   ├── Services/                        # Domain services
│   │   ├── ITenantIsolationService.cs
│   │   ├── IRoleAuthorizationService.cs
│   │   └── IAuditChainService.cs
│   └── Events/                          # Domain events
│       ├── DistrictProvisionedEvent.cs
│       ├── UserLifecycleEvent.cs
│       └── SecurityAlertEvent.cs
├── NorthstarET.Lms.Application/         # Clean Architecture Application Layer
│   ├── UseCases/                        # Feature-specific use cases
│   │   ├── Districts/
│   │   ├── Schools/
│   │   ├── Staff/
│   │   ├── Students/
│   │   ├── Assessments/
│   │   ├── Enrollment/
│   │   ├── Audit/
│   │   └── Compliance/
│   ├── Interfaces/                      # Abstractions for infrastructure
│   │   ├── IDistrictRepository.cs
│   │   ├── IIdentityProvider.cs
│   │   ├── IAuditStore.cs
│   │   └── IBulkOperationService.cs
│   ├── DTOs/                           # Data transfer objects
│   └── Validators/                      # Input validation logic
├── NorthstarET.Lms.Infrastructure/      # Clean Architecture Infrastructure Layer
│   ├── Data/                           # Entity Framework Core
│   │   ├── LmsDbContext.cs
│   │   ├── Configurations/             # Entity configurations
│   │   └── Migrations/                 # EF migrations
│   ├── Repositories/                   # Repository implementations
│   ├── ExternalServices/               # Entra ID, file storage
│   │   ├── EntraIdentityService.cs
│   │   └── AssessmentFileService.cs
│   ├── BackgroundServices/             # Hosted services
│   │   ├── RetentionJobService.cs
│   │   └── AuditProcessorService.cs
│   └── Security/                       # RBAC and tenant isolation
│       ├── TenantContextService.cs
│       └── RoleAuthorizationHandler.cs
├── NorthstarET.Lms.Api/                # Clean Architecture Presentation Layer
│   ├── Controllers/                    # REST API controllers
│   │   ├── DistrictsController.cs
│   │   ├── SchoolsController.cs
│   │   ├── StaffController.cs
│   │   ├── StudentsController.cs
│   │   ├── AssessmentsController.cs
│   │   ├── EnrollmentController.cs
│   │   ├── AuditController.cs
│   │   └── ComplianceController.cs
│   ├── Middleware/                     # Cross-cutting concerns
│   │   ├── TenantIsolationMiddleware.cs
│   │   ├── AuditLoggingMiddleware.cs
│   │   └── SecurityMonitoringMiddleware.cs
│   ├── Authentication/                 # Auth configuration
│   ├── Program.cs                      # Application entry point
│   └── appsettings.json               # Configuration
├── NorthstarET.Lms.AppHost/            # .NET Aspire Orchestration
│   ├── Program.cs                      # Aspire app host
│   └── appsettings.json               # Orchestration config
└── tests/                              # Testing structure
    ├── Features/                       # Reqnroll BDD feature files
    │   ├── Districts/
    │   ├── Schools/
    │   ├── Staff/
    │   ├── Students/
    │   ├── Assessments/
    │   ├── Enrollment/
    │   ├── Audit/
    │   └── Compliance/
    ├── StepDefinitions/                # Reqnroll step definitions
    ├── NorthstarET.Lms.Domain.Tests/  # Domain unit tests
    ├── NorthstarET.Lms.Application.Tests/ # Application unit tests
    ├── NorthstarET.Lms.Infrastructure.Tests/ # Infrastructure tests
    └── NorthstarET.Lms.Api.Tests/     # Integration tests
```

**Structure Decision**: Web application selected - .NET 9 Clean Architecture with multi-tenant ASP.NET Core API, Aspire orchestration, and comprehensive BDD testing framework.

## Complexity Tracking
*All constitutional requirements met - no violations to justify*

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| BDD-First Testing | ✅ COMPLIANT | Reqnroll features written first, step definitions fail initially |
| TDD Red-Green Cycle | ✅ COMPLIANT | Unit tests written before domain/application implementations |
| Clean Architecture | ✅ COMPLIANT | Four-layer separation, dependency direction enforced |
| Aspire Orchestration | ✅ COMPLIANT | Multi-tenant services with service discovery |
| Feature Specification Completeness | ✅ COMPLIANT | Complete spec with clarifications |

## Progress Tracking

**Phase Status**:
- ✅ Phase 0: Research complete (/plan command) - research.md created
- ✅ Phase 1: Design complete (/plan command) - data-model.md, contracts/, quickstart.md, copilot-instructions.md created  
- ✅ Phase 2: Task planning approach described (/plan command) - Ready for /tasks execution
- ⏳ Phase 3: Tasks generation (/tasks command) - NEXT STEP
- ⏳ Phase 4: Implementation execution
- ⏳ Phase 5: Validation and testing

**Gate Status**:
- ✅ Initial Constitution Check: PASS - All requirements compliant
- ✅ Post-Design Constitution Check: PASS - Architecture maintains constitutional principles
- ✅ All NEEDS CLARIFICATION resolved - Technical context fully specified
- ✅ Complexity deviations documented - None required

**Artifacts Generated**:
- ✅ research.md - Technical decisions and architecture patterns
- ✅ data-model.md - Complete domain model with multi-tenant EF Core configurations
- ✅ contracts/districts-api.md - Platform administration API contracts
- ✅ contracts/students-api.md - Student management API contracts
- ✅ quickstart.md - Complete development environment setup guide
- ✅ .github/copilot-instructions.md - Domain-specific AI guidance

**Ready for Phase 3**: Run `/tasks` command to generate implementation tasks from design artifacts.

---

*Phase 1 Complete - Implementation Plan Ready*  
*Based on Constitution v1.0.0 - See `.specify/memory/constitution.md`*
