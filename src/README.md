# Source Code Directory

## Purpose
Contains all production source code for the NorthstarET.Lms foundational Learning Management System organized by clean architecture layers.

## Architecture Context
This directory implements the Clean Architecture principle (Constitution Principle III) with strict dependency rules:
- **Domain Layer**: Pure business entities with zero external dependencies
- **Application Layer**: Use cases and abstractions, depends only on Domain  
- **Infrastructure Layer**: EF Core, external APIs, depends on Application
- **Presentation Layer**: ASP.NET Core API controllers, depends on Infrastructure

## Directory Structure

### Core Layers
- `NorthstarET.Lms.Domain/` - Domain entities, value objects, and domain services
- `NorthstarET.Lms.Application/` - Application services, DTOs, and interfaces
- `NorthstarET.Lms.Infrastructure/` - Data access, external service implementations
- `NorthstarET.Lms.Api/` - REST API controllers and presentation layer

### Support Projects  
- `NorthstarET.Lms.AppHost/` - .NET Aspire orchestration and service discovery

## File Inventory
```
src/
├── NorthstarET.Lms.Domain/           # Domain layer (no external dependencies)
├── NorthstarET.Lms.Application/      # Application layer (depends on Domain)
├── NorthstarET.Lms.Infrastructure/   # Infrastructure layer (depends on Application) 
├── NorthstarET.Lms.Api/             # API layer (depends on Infrastructure)
└── NorthstarET.Lms.AppHost/         # Aspire orchestration (depends on all)
```

## Usage Examples

### Building All Projects
```bash
dotnet build src/
```

### Running with Aspire Orchestration
```bash
dotnet run --project src/NorthstarET.Lms.AppHost
```

## Architecture Validation
The build process validates clean architecture dependency rules:
- Domain layer cannot reference external packages
- Application layer cannot reference Infrastructure or Presentation
- Each layer must maintain clear boundaries

## Recent Changes
- 2025-01-09: Added Application layer foundation with services, DTOs, and interfaces
- 2025-01-09: Domain layer complete with entities, events, and value objects
- 2024-12-19: Initial clean architecture project structure established

## Related Documentation
- See individual project README files for detailed component documentation
- See `../tests/` for corresponding test structure  
- See `../specs/` for feature specifications and contracts