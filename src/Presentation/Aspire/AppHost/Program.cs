// Aspire AppHost entry point - orchestrates the LMS application services
// This file is auto-generated and should not be modified directly

var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("lmsdb");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobs("assessments");

var apiService = builder.AddProject<Projects.Presentation_Api>("api")
    .WithReference(sqlServer)
    .WithReference(storage);

builder.Build().Run();