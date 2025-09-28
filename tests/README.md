# Tests Directory

## Purpose
Contains all test code for the NorthstarET.Lms system organized by test type and architectural layer, following BDD-First Testing and TDD principles.

## Architecture Context
Implements Constitution Principles I and II:
- **BDD-First Testing**: Feature files with Reqnroll scenarios written before implementation
- **TDD Red-Green Cycle**: Unit and integration tests written before production code

## Directory Structure

### BDD Feature Tests
- `Features/` - Reqnroll feature files organized by business domain
- `StepDefinitions/` - Step definition implementations for BDD scenarios

### Unit & Integration Tests
- `NorthstarET.Lms.Domain.Tests/` - Domain entity and value object tests
- `NorthstarET.Lms.Application.Tests/` - Application service and use case tests  
- `NorthstarET.Lms.Infrastructure.Tests/` - Repository and external service tests
- `NorthstarET.Lms.Api.Tests/` - Controller and API integration tests

## File Inventory
```
tests/
├── Features/                              # BDD feature files (Reqnroll)
│   ├── Districts/                        # District management scenarios
│   ├── Students/                         # Student lifecycle scenarios
│   ├── RBAC/                            # Role-based access control scenarios
│   ├── Compliance/                       # Audit and compliance scenarios
│   ├── Assessments/                      # Assessment workflow scenarios
│   └── Calendar/                        # Academic calendar scenarios
├── StepDefinitions/                       # BDD step implementations
├── NorthstarET.Lms.Domain.Tests/        # Domain layer unit tests
│   ├── Entities/                        # Entity behavior tests
│   └── ValueObjects/                    # Value object tests
├── NorthstarET.Lms.Application.Tests/   # Application layer unit tests
│   └── Services/                        # Application service tests
├── NorthstarET.Lms.Infrastructure.Tests/ # Infrastructure integration tests
└── NorthstarET.Lms.Api.Tests/           # API integration tests
```

## Usage Examples

### Run All Tests
```bash
dotnet test tests/
```

### Run BDD Feature Tests Only
```bash
dotnet test tests/ --filter Category=BDD
```

### Run Unit Tests for Specific Layer
```bash
dotnet test tests/NorthstarET.Lms.Domain.Tests/
```

### Generate Test Coverage Report
```bash
dotnet test tests/ --collect:"XPlat Code Coverage" --results-directory TestResults/
```

## Test Strategy

### BDD Testing (Principle I)
- All business requirements expressed as Gherkin scenarios
- Feature files written before any implementation code
- Step definitions implement test automation
- Covers complete user journeys and acceptance criteria

### TDD Implementation (Principle II)
- Unit tests written before production code (RED phase)
- Implementation makes tests pass (GREEN phase)  
- Refactor with test coverage maintained
- Target >90% coverage for Domain and Application layers

## Current Test Status
- ✅ **18 Feature Files**: Complete BDD scenarios for all domains
- ✅ **31 Application Service Tests**: All services have comprehensive test coverage
- ✅ **Domain Entity Tests**: Core business logic validated
- 🔄 **Integration Tests**: In progress for infrastructure and API layers

## Recent Changes  
- 2025-01-09: Added comprehensive application service tests (TDD RED phase)
- 2025-01-09: Completed BDD feature files for all business domains
- 2024-12-19: Initial test project structure and BDD framework setup

## Related Documentation
- See `../src/` for corresponding production code structure
- See `../specs/001-foundational-lms-with/` for detailed feature specifications
- See `.specify/memory/constitution.md` for testing principles and requirements