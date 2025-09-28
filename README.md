# NorthstarET.Lms - Foundational Learning Management System

## Purpose
A foundational Learning Management System (LMS) for K-12 education with strict multi-tenant isolation, comprehensive RBAC, and FERPA compliance requirements. The system supports school districts as tenants with complex hierarchical relationships between districts, schools, classes, staff, and students.

## Architecture Context
Built on Clean Architecture principles with strict dependency rules:
- **Domain Layer**: Pure business entities with zero external dependencies
- **Application Layer**: Use cases and abstractions, depends only on Domain
- **Infrastructure Layer**: EF Core, external APIs, depends on Application
- **Presentation Layer**: ASP.NET Core API controllers, depends on Infrastructure

## Directory Structure

### Core Directories
- `src/` - All production source code organized by clean architecture layers
- `tests/` - Comprehensive test suite including BDD tests, unit tests, and integration tests
- `specs/` - Feature specifications and contracts following BDD-first approach
- `.github/` - GitHub configuration, workflows, and development guidance
- `.specify/` - Spec-driven development tooling and templates

### Configuration & Build
- `Directory.Build.props` - MSBuild configuration shared across all projects
- `NorthstarET.Lms.sln` - Visual Studio solution file
- `.editorconfig` - Code formatting and style configuration
- `.gitignore` - Git exclusion patterns

## File Inventory
```
NorthstarET.Lms/
├── src/                          # Production source code (Clean Architecture)
├── tests/                        # Test suite (BDD, unit, integration)  
├── specs/                        # Feature specifications and contracts
├── .github/                      # GitHub configuration and workflows
├── .specify/                     # Spec-driven development tooling
├── Context/                      # Legacy context documentation
├── Directory.Build.props         # MSBuild configuration
├── NorthstarET.Lms.sln          # Solution file
└── README.md                     # This file
```

## Usage Examples

### Development Workflow
```bash
# Build all projects
dotnet build

# Run with Aspire orchestration
dotnet run --project src/NorthstarET.Lms.AppHost

# Run all tests
dotnet test

# Run BDD tests specifically
dotnet test tests/ --filter "Category=BDD"
```

### Spec-Driven Development
```bash
# Create new feature specification
cd .specify/scripts/bash && ./create-new-feature.sh "my-feature"

# Check prerequisites for implementation
./check-prerequisites.sh --json --require-tasks
```

## Technology Stack
- **.NET 9** - Latest framework with modern C# features
- **ASP.NET Core** - Web API and hosting
- **.NET Aspire** - Orchestration and service discovery
- **Entity Framework Core** - Data access layer
- **Reqnroll** - BDD testing framework
- **xUnit** - Unit testing framework
- **FluentAssertions** - Readable test assertions

## Architecture Principles
This project follows strict constitutional principles:

1. **BDD-First Testing (NON-NEGOTIABLE)** - All features begin with Reqnroll scenarios
2. **TDD Red-Green Cycle (NON-NEGOTIABLE)** - Tests written before implementation  
3. **Clean Architecture (NON-NEGOTIABLE)** - Strict dependency rules enforced
4. **Aspire Orchestration** - All services managed through .NET Aspire
5. **Feature Specification Completeness** - Complete specs before implementation
6. **Documentation-First Architecture (NON-NEGOTIABLE)** - Every directory has README.md

## Getting Started

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 or VS Code with C# extensions
- Git

### Quick Start
```bash
# Clone the repository
git clone <repository-url>
cd NorthstarET.Lms

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project src/NorthstarET.Lms.AppHost
```

## Contributing
All contributions must follow the constitutional principles and BDD-first development approach. See `.github/CONTRIBUTING.md` for detailed guidelines.

## Recent Changes
- 2025-01-09: Added Documentation-First Architecture principle requiring README files
- 2025-01-09: Implemented constitution compliance validation in CI/CD
- 2025-01-09: Added comprehensive spec-driven development tooling
- 2024-12-19: Initial clean architecture project structure established

## Related Documentation
- See `src/README.md` for source code architecture details
- See `tests/README.md` for testing strategy and test organization
- See `specs/README.md` for feature specification guidelines  
- See `.github/prompts/` for development workflow automation
- See `.specify/memory/constitution.md` for complete constitutional principles