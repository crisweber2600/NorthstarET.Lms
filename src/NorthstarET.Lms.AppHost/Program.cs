var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server with multi-tenant support
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume();

var platformDb = sqlServer.AddDatabase("lms-platform");
var auditDb = sqlServer.AddDatabase("lms-audit");

// Add Redis for caching and distributed locks
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Note: Cannot add API project directly due to framework mismatch (net9.0 vs net8.0)
// Add the main LMS API when needed:
// var lmsApi = builder.AddProject<Projects.NorthstarET_Lms_Api>("lms-api")
//     .WithReference(platformDb)
//     .WithReference(auditDb)
//     .WithReference(redis)
//     .WaitFor(sqlServer)
//     .WaitFor(redis);

builder.Build().Run();
