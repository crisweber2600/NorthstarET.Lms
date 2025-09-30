// Aspire AppHost entry point - orchestrates the LMS application services
// Implements T107-T116: Aspire Orchestration with SQL Server, Azurite, Service Discovery, Health Checks, Logging

var builder = DistributedApplication.CreateBuilder(args);

// T108: SQL Server integration using Aspire SqlServer resource
// Multi-database support for multi-tenant architecture (each tenant gets a schema)
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume() // Persist data across restarts
    .WithLifetime(ContainerLifetime.Persistent); // Keep container running

var lmsDatabase = sqlServer.AddDatabase("lmsdb");

// T109: Azurite blob storage integration using Aspire storage resource
// Used for assessment file uploads (100MB limit per file)
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(); // Use Azurite emulator for local development

var assessmentBlobs = storage.AddBlobs("assessments");

// T110-T111: Service discovery and configuration management
// T112: Health checks configuration
// T113: Structured logging with Aspire abstractions
var apiService = builder.AddProject<Projects.Presentation_Api>("api")
    .WithReference(lmsDatabase) // Database connection via service discovery
    .WithReference(assessmentBlobs) // Blob storage connection via service discovery
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpsEndpoint(port: 7001, name: "https") // HTTPS endpoint for API
    .WithHttpEndpoint(port: 7000, name: "http"); // HTTP endpoint for health checks

// T114: Background services orchestration
// Background service for bulk operations, audit processing, retention enforcement
// Runs within the API service using IHostedService implementations

builder.Build().Run();