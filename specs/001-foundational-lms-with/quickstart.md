# Quickstart – Foundational LMS Validation

This quickstart exercises the planned BDD-first workflow and validates multi-tenant isolation, RBAC, bulk jobs, and compliance features before implementation.

## 1. Environment Setup (Aspire)

1. Install .NET 9 SDK and .NET Aspire workload.
2. Run the Aspire AppHost project: `dotnet run --project src/Presentation/Aspire/AppHost/AppHost.csproj`.
3. Confirm services:
   - API service reachable at `https://localhost:7200`
   - SQL Server container running with seeded platform metadata
   - Azurite storage emulator running for assessment documents

## 2. Seed Reference Data

1. Execute onboarding script (to be implemented) that provisions a sample district `oakland-unified` with defaults.
2. Verify schema `oakland_unified` appears in SQL Server and contains empty tables for domain aggregates.

## 3. Run BDD Suites (Red Phase)

1. Navigate to `tests/Bdd` project and execute `dotnet test`.
2. Observe failing scenarios for:
   - District provisioning
   - Identity mapping lifecycle
   - Academic calendar validation
   - Composite role authorization
   - Bulk student rollover preview
   - Legal hold retention skip
   - Security anomaly detection
3. Capture failure output to satisfy red phase documentation.

## 4. Contract Test Smoke (Red Phase)

1. Run contract test project `tests/Presentation/Contracts.Tests`: `dotnet test`.
2. Confirm each test fails with `Pending implementation` to enforce TDD sequencing.

## 5. Tenant Isolation Sanity Check (Post-Implementation)

1. After implementing repositories, run integration smoke script `dotnet run --project tools/TenantSmoke/TenantSmoke.csproj`.
2. Script should:
   - Create districts `oakland-unified` and `san-jose`.
   - Write sample student to each schema.
   - Attempt cross-tenant read (must fail with `Forbidden`).
   - Output success summary.

## 6. Bulk Job Dry-Run

1. Execute REST call `POST /api/v1/bulk/rollover/preview` with Grade 5→6 payload.
2. Inspect response: preview list, zero mutations, audit entry recorded.
3. Trigger full rollover and confirm job transitions to `Completed` under threshold constraints.

## 7. Security Alert Simulation

1. Run script to simulate repeated unauthorized access attempts.
2. Verify system generates Tier 2 alert, suspends offending identity, and records audit chain hash.

## 8. Retention Enforcement Validation

1. Seed retention policy overrides (e.g., Student retention = 1 minute) in non-production environment.
2. Wait for purge job to execute.
3. Confirm legal holds prevent deletion and audit log records skip entries.

## 9. Cleanup

1. Stop Aspire host.
2. Drop local SQL Server schemas using `dotnet run --project tools/Admin/AdminCli.csproj purge --tenant oakland-unified`.
3. Remove Azurite containers and purge blob storage to avoid exceeding quotas.
