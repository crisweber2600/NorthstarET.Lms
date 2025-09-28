var builder = DistributedApplication.CreateBuilder(args);

// Configure Aspire service defaults for observability
builder.Services.AddServiceDefaults();

// Add SQL Server for multi-tenant data with comprehensive configuration
var sqlServerPassword = builder.AddParameter("SqlServerPassword", secret: true);
var sqlserver = builder.AddSqlServer("sqlserver", password: sqlServerPassword)
    .WithDataVolume("lms-sqlserver-data")
    .WithLifetime(ContainerLifetime.Persistent)  // Keep data between restarts
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_COLLATION", "SQL_Latin1_General_CP1_CI_AS")
    .WithEnvironment("MSSQL_MEMORY_LIMIT_MB", "2048");

// Main LMS database for platform operations and tenant metadata
var platformDb = sqlserver.AddDatabase("lmsdb", "LmsPlatform");

// Multi-tenant database for district data isolation
var tenantDb = sqlserver.AddDatabase("lmstenantdb", "LmsTenant");

// Add Redis for caching, session management, and distributed locks
var redis = builder.AddRedis("redis")
    .WithDataVolume("lms-redis-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("REDIS_MAXMEMORY", "512mb")
    .WithEnvironment("REDIS_MAXMEMORY_POLICY", "allkeys-lru");

// Add observability stack
var seq = builder.AddSeq("seq")
    .WithDataVolume("lms-seq-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

var prometheus = builder.AddContainer("prometheus", "prom/prometheus")
    .WithDataVolume("lms-prometheus-data")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithEndpoint(9090, 9090, "http");

// Add the main API with comprehensive configuration
var api = builder.AddProject<Projects.NorthstarET_Lms_Api>("api")
    .WithReference(platformDb)
    .WithReference(tenantDb) 
    .WithReference(redis)
    .WithReference(seq)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ConnectionStrings__DefaultConnection", platformDb)
    .WithEnvironment("ConnectionStrings__TenantConnection", tenantDb)
    .WithEnvironment("ConnectionStrings__Redis", redis)
    .WithEnvironment("Seq__ServerUrl", seq.GetEndpoint("http"))
    .WithEnvironment("HealthChecks__Enabled", "true")
    .WithEnvironment("HealthChecks__UI__Enabled", "true")
    .WithEnvironment("Logging__Seq__ServerUrl", seq.GetEndpoint("http"))
    .WithEnvironment("MultiTenant__Enabled", "true")
    .WithEnvironment("MultiTenant__IsolationLevel", "Schema")
    .WithEnvironment("Jwt__Issuer", "NorthstarET.Lms")
    .WithEnvironment("Jwt__Audience", "NorthstarET.Lms.Api")
    .WithReplicas(1); // Single instance for development, scale in production

// Background services for compliance and maintenance
var backgroundServices = builder.AddProject<Projects.NorthstarET_Lms_BackgroundServices>("background-services")
    .WithReference(platformDb)
    .WithReference(tenantDb)
    .WithReference(redis)
    .WithReference(seq)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("ConnectionStrings__DefaultConnection", platformDb)
    .WithEnvironment("ConnectionStrings__TenantConnection", tenantDb)
    .WithEnvironment("RetentionPolicies__Enabled", "true")
    .WithEnvironment("RetentionPolicies__RunIntervalHours", "24")
    .WithEnvironment("AuditChainProcessor__Enabled", "true")
    .WithEnvironment("AuditChainProcessor__BatchSize", "1000");

// Integration test database (separate from development)
if (builder.Environment.EnvironmentName == "Testing")
{
    var testDb = builder.AddSqlServer("test-sqlserver")
        .WithDataVolume("lms-test-sqlserver-data")
        .AddDatabase("lmstestdb");
    
    builder.AddProject<Projects.NorthstarET_Lms_Api_Tests>("integration-tests")
        .WithReference(testDb)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing");
}

// Health checks dashboard (development only)
if (builder.Environment.IsDevelopment())
{
    api.WithEnvironment("HealthChecksUI__HealthChecks__0__Name", "LMS API")
        .WithEnvironment("HealthChecksUI__HealthChecks__0__Uri", "https://localhost:7000/health");
}

// Aspire dashboard configuration
builder.Services.Configure<DashboardOptions>(options =>
{
    options.ApplicationName = "Northstar ET Learning Management System";
    options.TelemetryLimitOptions.MaxLogCount = 10000;
    options.TelemetryLimitOptions.MaxTraceCount = 2000;
    options.TelemetryLimitOptions.MaxMetricsCount = 5000;
});

var app = builder.Build();

// Run the distributed application
app.Run();
