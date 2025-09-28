var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server with multi-tenant support
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("lms-platform")
    .AddDatabase("lms-audit");

// Add Redis for caching and distributed locks
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Add the main LMS API
var lmsApi = builder.AddProject<Projects.NorthstarET_Lms_Api>("lms-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WaitFor(sqlServer)
    .WaitFor(redis);

builder.Build().Run();
