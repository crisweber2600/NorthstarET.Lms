using NorthstarET.Lms.ServiceDefaults;
using NorthstarET.Lms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, etc.)
builder.AddServiceDefaults();

// Add existing LMS services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add SQL Server with Aspire integration
builder.AddSqlServerDbContext<LmsDbContext>("lms-platform");

// Add Redis with Aspire integration  
builder.AddRedisClient("redis");

// Add Azure Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Build and configure the app
var app = builder.Build();

// Map Aspire service defaults (health checks, etc.)
app.MapDefaultEndpoints();

// Use existing LMS middleware and endpoints
app.UseInfrastructure();

app.Run();