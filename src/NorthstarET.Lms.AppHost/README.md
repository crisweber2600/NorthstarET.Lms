# NorthstarET.Lms.AppHost

## Purpose
.NET Aspire AppHost project that orchestrates all services, databases, and external dependencies for the Learning Management System. Provides service discovery, configuration management, and development environment setup.

## Architecture Context
This is the Aspire Orchestration layer:
- **Dependencies**: All other projects (Domain, Application, Infrastructure, API)
- **Dependents**: Development tools, deployment environments
- **Responsibilities**: Service orchestration, configuration, health monitoring

## File Inventory
```
NorthstarET.Lms.AppHost/
├── Properties/                   # Launch settings and configuration
│   └── launchSettings.json      # Development environment settings
├── Program.cs                    # Aspire app host configuration
├── appsettings.json             # Orchestration configuration
└── README.md                     # This file
```

## Usage Examples

### Running the Orchestrated Application
```bash
# Start all services with Aspire dashboard
dotnet run --project src/NorthstarET.Lms.AppHost

# The Aspire dashboard will be available at: https://localhost:15000
```

### Service Configuration
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server with persistent storage
var sqlServer = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("northstar-lms");

// Add the API service with SQL dependency
var api = builder.AddProject<Projects.NorthstarET_Lms_Api>("api")
    .WithReference(sqlServer);

// Add external services
var redis = builder.AddRedis("cache");
api.WithReference(redis);

builder.Build().Run();
```

## Key Features

### Service Discovery
- Automatic service registration and discovery
- Dynamic endpoint resolution
- Health check orchestration

### Development Environment
- One-command startup for entire system
- Consistent configuration across services
- Real-time service monitoring

### External Dependencies
- SQL Server database with seeded data
- Redis cache for session management
- Message queue for background processing
- External API simulators for testing

## Configuration

### Service Dependencies
```json
{
  "Services": {
    "Api": {
      "ConnectionStrings": {
        "DefaultConnection": "{sql.connectionString}",
        "Redis": "{cache.connectionString}"
      }
    }
  }
}
```

### Health Checks
- Database connectivity monitoring
- External service availability
- Application-specific health endpoints

## Development Workflow

### Local Development
1. Start Aspire AppHost: `dotnet run --project src/NorthstarET.Lms.AppHost`
2. Access Aspire Dashboard at https://localhost:15000
3. Monitor service logs, metrics, and health status
4. All services automatically configured and connected

### Service Communication
- Services communicate through service discovery
- Configuration automatically injected
- Observability built-in with OpenTelemetry

## Dependencies
- **Microsoft.Extensions.Hosting** - Generic host
- **Aspire.Hosting** - Aspire orchestration framework
- **All project references** - Domain, Application, Infrastructure, API

## Observability

### Metrics & Logging
- Centralized logging through Aspire dashboard
- Distributed tracing with OpenTelemetry
- Performance metrics collection
- Custom business metrics

### Monitoring
- Service health status
- Resource utilization
- Request tracing across services
- Error rate monitoring

## Deployment

### Container Support
- Docker container orchestration
- Kubernetes deployment manifests
- Azure Container Apps integration

### Environment Configuration
- Development: Local SQL Server and Redis
- Testing: Containerized dependencies
- Production: Managed Azure services

## Security

### Service-to-Service Communication
- TLS encryption between services
- Service identity and authentication
- Network isolation and firewall rules

## Recent Changes
- 2025-01-09: Created Aspire AppHost for service orchestration
- 2025-01-09: Added SQL Server and Redis dependencies
- 2025-01-09: Implemented health monitoring and observability

## Related Documentation
- See `../NorthstarET.Lms.Api/README.md` for API service details
- See `../NorthstarET.Lms.Infrastructure/README.md` for data access configuration
- See `.NET Aspire documentation` for orchestration patterns
- See `../../tests/README.md` for testing with Aspire services