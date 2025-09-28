var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server for multi-tenant data
var sqlserver = builder.AddSqlServer("sqlserver")
    .WithDataVolume("lms-sqlserver-data")
    .AddDatabase("lmsdb");

// Add Redis for caching and session management
var redis = builder.AddRedis("redis")
    .WithDataVolume("lms-redis-data");

// Add the main API with dependencies
var api = builder.AddProject<Projects.NorthstarET_Lms_Api>("api")
    .WithReference(sqlserver)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

builder.Build().Run();
