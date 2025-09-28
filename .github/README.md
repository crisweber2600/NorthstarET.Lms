# GitHub Configuration

## Purpose
Contains GitHub-specific configuration including workflows, issue templates, pull request templates, and development automation tools.

## Architecture Context
Development and CI/CD infrastructure:
- **Dependencies**: Repository source code and tests
- **Dependents**: GitHub Actions, development workflows
- **Responsibilities**: CI/CD, code quality, development automation

## Directory Structure

### Core Components
- `workflows/` - GitHub Actions CI/CD workflows
- `prompts/` - Development workflow automation prompts
- `instructions/` - Development guidelines and AI behavior configuration
- `PULL_REQUEST_TEMPLATE.md` - Standard PR template (if exists)
- `ISSUE_TEMPLATE/` - Issue templates (if exists)

## File Inventory
```
.github/
├── workflows/                    # GitHub Actions workflows
│   ├── ci.yml                   # Continuous integration
│   ├── cd.yml                   # Continuous deployment
│   └── quality.yml              # Code quality checks
├── prompts/                      # Development automation
│   ├── analyze.prompt.md        # Analysis automation
│   ├── implement.prompt.md      # Implementation automation
│   ├── constitution.prompt.md   # Constitution management
│   └── *.prompt.md             # Other workflow prompts
├── instructions/                 # Development configuration
│   └── instructions             # AI behavior guidelines
├── copilot-instructions.md       # GitHub Copilot configuration
└── README.md                     # This file
```

## Usage Examples

### Triggering CI/CD
```bash
# Push to main branch triggers full CI/CD
git push origin main

# Pull request triggers validation workflow
gh pr create --title "Feature: Add student management"
```

### Development Automation
The prompts system provides automated development workflows:
- `/analyze` - Cross-artifact consistency analysis
- `/implement` - Automated feature implementation
- `/constitution` - Project constitution management

## Key Features

### Continuous Integration
- Automated build and test execution
- Code quality validation with static analysis
- Multi-environment testing (unit, integration, BDD)
- Security scanning and vulnerability assessment

### Code Quality Gates
- All tests must pass (>90% coverage required)
- Static analysis with zero high-severity findings
- Architecture dependency validation
- Performance regression testing

### Development Workflows
- Automated README maintenance
- Constitution compliance checking
- Feature specification validation
- BDD-first development enforcement

## Workflow Configuration

### CI Workflow (ci.yml)
```yaml
name: Continuous Integration
on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

### Quality Gates
- Constitution compliance validation
- README file synchronization check
- Dependency rule validation
- Security vulnerability scanning

## Development Guidelines

### Pull Request Requirements
- [ ] BDD feature files exist and scenarios pass
- [ ] TDD red-green cycle evidence provided
- [ ] Clean architecture boundaries maintained
- [ ] README files updated for modified directories
- [ ] Constitution principles followed
- [ ] Tests maintain >90% coverage

### Automated Checks
- Architecture analyzer validates dependency rules
- Codacy integration for code quality assessment
- Automated README synchronization validation
- Performance regression detection

## Configuration Files

### Copilot Instructions
Custom GitHub Copilot behavior configured for:
- BDD-first development approach
- Clean architecture enforcement
- Multi-tenant development patterns
- Constitution compliance assistance

### AI Development Guidelines
- Structured prompts for consistent development
- Automated analysis and validation
- Constitution-based decision making
- Documentation-first architecture

## Recent Changes
- 2025-01-09: Added development workflow automation prompts
- 2025-01-09: Implemented constitution compliance checking
- 2025-01-09: Added automated README maintenance system
- 2025-01-09: Configured GitHub Copilot for LMS development patterns

## Related Documentation
- See `workflows/README.md` for CI/CD pipeline details
- See `prompts/README.md` for development automation documentation
- See `instructions/` for detailed development guidelines
- See `../copilot-instructions.md` for AI configuration details