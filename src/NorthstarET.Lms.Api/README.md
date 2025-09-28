# NorthstarET.Lms.Api

## Purpose
Presentation layer providing REST API endpoints for the Learning Management System. This is the entry point for all external clients and handles HTTP requests/responses.

## Architecture Context
This is the Presentation layer in our Clean Architecture:
- **Dependencies**: Infrastructure layer (for service implementations)
- **Dependents**: External clients (web apps, mobile apps, integrations)
- **Responsibilities**: HTTP routing, request/response handling, API documentation

## Directory Structure

### Core API Components
- `Controllers/` - REST API controllers organized by domain
- `Middleware/` - Custom HTTP middleware components
- `Filters/` - Action filters, exception filters, authorization filters
- `Properties/` - Launch settings and environment configuration

## File Inventory
```
NorthstarET.Lms.Api/
├── Controllers/                  # REST API controllers
│   ├── StudentsController.cs    # Student management endpoints
│   ├── SchoolsController.cs     # School management endpoints
│   ├── ClassesController.cs     # Class management endpoints
│   └── UsersController.cs       # User management endpoints
├── Middleware/                   # Custom middleware
├── Filters/                      # Action and exception filters
├── Properties/                   # Launch settings
│   └── launchSettings.json      # Development server configuration
├── Program.cs                    # Application entry point
├── appsettings.json             # Application configuration
└── README.md                     # This file
```

## Usage Examples

### Running the API
```bash
# Development with hot reload
dotnet watch --project src/NorthstarET.Lms.Api

# Production build
dotnet run --project src/NorthstarET.Lms.Api --configuration Release
```

### API Endpoints
```http
# Student Management
GET    /api/v1/students
POST   /api/v1/students
GET    /api/v1/students/{id}
PUT    /api/v1/students/{id}
DELETE /api/v1/students/{id}

# School Management  
GET    /api/v1/schools
POST   /api/v1/schools
```

## Key Features

### Authentication & Authorization
- JWT-based authentication
- Role-based access control (RBAC)
- Multi-tenant request scoping

### API Documentation
- OpenAPI/Swagger documentation
- Comprehensive endpoint documentation
- Request/response examples

### Error Handling
- Standardized error responses
- Proper HTTP status codes
- Detailed error messages for debugging

### Validation
- Model validation with FluentValidation
- Custom validation attributes
- Request/response DTOs

## Configuration

### Development Settings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NorthstarLms_Dev;Trusted_Connection=true;"
  },
  "Authentication": {
    "JwtSecretKey": "development-key-change-in-production",
    "JwtIssuer": "NorthstarET.Lms",
    "JwtAudience": "NorthstarET.Lms.Api"
  }
}
```

## Dependencies
- **NorthstarET.Lms.Infrastructure** - Service implementations
- **Microsoft.AspNetCore** - Web API framework
- **Swashbuckle.AspNetCore** - Swagger/OpenAPI documentation
- **FluentValidation.AspNetCore** - Request validation

## Testing Strategy
- Controller unit tests with mocked dependencies
- Integration tests with TestServer
- API contract tests with Reqnroll scenarios
- Performance tests for critical endpoints

## Security Considerations
- All endpoints require authentication (except health checks)
- Tenant isolation enforced at request level
- Input validation and sanitization
- Rate limiting per tenant
- HTTPS enforced in production

## Recent Changes
- 2025-01-09: Added API layer foundation with controllers
- 2025-01-09: Implemented JWT authentication and RBAC
- 2025-01-09: Added comprehensive input validation

## Related Documentation
- See `Controllers/README.md` for controller implementation patterns
- See `../Infrastructure/README.md` for service implementations
- See `../../tests/NorthstarET.Lms.Api.Tests/README.md` for API testing approach
- See `/specs/` for API contract specifications