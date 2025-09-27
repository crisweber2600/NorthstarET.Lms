# Quickstart: LMS Roster Authority Baseline

## Prerequisites

- .NET SDK 8.0.x installed (`dotnet --version` ≥ 8.0.100)
- Docker Desktop running (for PostgreSQL + Kafka via Testcontainers)
- Node.js 20.x (for Spectral contract linting)
- `kubectl` configured for staging cluster (for later deployment validation)

## Environment Setup

```bash
# Restore dependencies and tool manifest
 dotnet restore

# Provision local infrastructure (PostgreSQL + Kafka)
 docker compose -f infra/docker-compose.yml up -d
```

## Running Tests (Contract-First)

```bash
# Contract tests are expected to fail until implementation
 dotnet test tests/ContractTests/ContractTests.csproj

# Domain + integration tests (should fail pre-implementation)
 dotnet test tests/DomainTests/DomainTests.csproj
```

## Linting Contracts

```bash
# Validate OpenAPI additions stay compliant with constitution gates
 npx @stoplight/spectral lint specs/002-init-spec-in/contracts/roster-api.v1.yaml

# Validate event schemas remain PII-free
 dotnet run --project tools/EventSchemaLint/EventSchemaLint.csproj
```

## Local Smoke Slice (post-implementation)

```bash
# Launch API with seeded data
 dotnet run --project src/Api/Api.csproj

# Issue vertical slice scenario
 http POST :8080/v1/tenants name="Demo District" region="us-east"
 http POST :8080/v1/schools name="North High" X-Tenant-Id:"{tenantId}" -A bearer
```

## Shutdown

```bash
 docker compose -f infra/docker-compose.yml down
```

> **Note**: Performance probes (k6) and chaos drills run via `/docs/perf/README.md` once implementation is complete.
