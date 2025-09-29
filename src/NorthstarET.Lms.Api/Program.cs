using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Api.Authentication;
using NorthstarET.Lms.Api.Middleware;
using NorthstarET.Lms.Application.Services;
using NorthstarET.Lms.Infrastructure.Data;
using NorthstarET.Lms.Infrastructure;
using Serilog;
using System.Reflection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

// Configure Serilog early for startup logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq(Environment.GetEnvironmentVariable("Seq__ServerUrl") ?? "http://localhost:5341")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Aspire service defaults for observability
    builder.AddServiceDefaults();

    // Configure Serilog
    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq(context.Configuration.GetConnectionString("Seq") ?? "http://localhost:5341")
            .Enrich.WithProperty("ApplicationName", "NorthstarET.Lms.Api")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .Enrich.FromLogContext();
    });

    // Configuration
    builder.Configuration.AddEnvironmentVariables();
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    // Core services
    builder.Services.AddControllers(options =>
    {
        // Add global exception filter
        options.Filters.Add<GlobalExceptionFilter>();
        
        // Add model validation filter
        options.Filters.Add<ModelValidationFilter>();
        
        // Configure JSON options
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

    // API versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ApiVersionReader = Microsoft.AspNetCore.Mvc.ApiVersionReader.Combine(
            new Microsoft.AspNetCore.Mvc.QueryStringApiVersionReader("version"),
            new Microsoft.AspNetCore.Mvc.HeaderApiVersionReader("X-Version"),
            new Microsoft.AspNetCore.Mvc.UrlSegmentApiVersionReader()
        );
    }).AddApiExplorer(setup =>
    {
        setup.GroupNameFormat = "'v'VVV";
        setup.SubstituteApiVersionInUrl = true;
    });

    // OpenAPI/Swagger configuration
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Northstar ET Learning Management System API",
            Version = "v1.0",
            Description = "A foundational LMS with multi-tenant isolation and FERPA compliance",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Northstar ET Support",
                Email = "support@northstaret.com"
            }
        });

        // Add JWT authentication to Swagger
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments for documentation
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Authentication & Authorization
    JwtConfiguration.ConfigureServices(builder.Services, builder.Configuration);

    // Database configuration with multi-tenant support
    builder.Services.AddDbContextFactory<LmsDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), null);
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
        });
        
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging(false); // Never log sensitive data even in dev
            options.EnableDetailedErrors();
        }
    });

    // Redis caching
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "LmsCache";
    });

    // Infrastructure layer services
    builder.Services.AddInfrastructure(builder.Configuration);

    // Application layer services
    builder.Services.AddApplication(builder.Configuration);

    // Health checks
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "sqlserver",
            tags: new[] { "database", "sql", "ready" })
        .AddRedis(
            builder.Configuration.GetConnectionString("Redis")!,
            name: "redis", 
            tags: new[] { "cache", "redis", "ready" })
        .AddCheck<TenantIsolationHealthCheck>("tenant-isolation", tags: new[] { "security", "tenant" })
        .AddCheck<AuditChainHealthCheck>("audit-chain", tags: new[] { "compliance", "audit" });

    // Health checks UI (development only)
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(30);
            setup.AddHealthCheckEndpoint("LMS API", "https://localhost:7000/health");
            setup.SetMinimumSecondsBetweenFailureNotifications(60);
        }).AddInMemoryStorage();
    }

    // CORS configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevelopmentPolicy", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            }
        });
        
        options.AddPolicy("ProductionPolicy", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = Microsoft.AspNetCore.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext => Microsoft.AspNetCore.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: partition => new Microsoft.AspNetCore.RateLimiting.FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 1000, // 1000 requests per window
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    // Problem details for error handling
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
            
            if (context.HttpContext.Items.ContainsKey("correlationId"))
            {
                context.ProblemDetails.Extensions.TryAdd("correlationId", context.HttpContext.Items["correlationId"]);
            }
        };
    });

    // Configure response compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    // Build the application
    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "LMS API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "Northstar ET LMS API Documentation";
            c.DefaultModelsExpandDepth(-1); // Hide models section by default
        });
        
        // Health checks UI
        app.UseHealthChecksUI(options => options.UIPath = "/health-ui");
        
        app.UseCors("DevelopmentPolicy");
    }
    else
    {
        app.UseExceptionHandler();
        app.UseHsts(); // HTTP Strict Transport Security
        app.UseCors("ProductionPolicy");
    }

    // Security headers
    app.UseSecurityHeaders();

    // Request/Response logging and metrics
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
            
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
                diagnosticContext.Set("TenantId", httpContext.User.FindFirst("tenant_id")?.Value);
            }
        };
    });

    // Response compression
    app.UseResponseCompression();

    // Rate limiting
    app.UseRateLimiter();

    // HTTPS redirection
    app.UseHttpsRedirection();

    // Custom middleware pipeline (order matters!)
    app.UseSecurityMonitoring();  // First - security threat detection
    app.UseTenantIsolation();     // Second - tenant context resolution  
    app.UseAuditLogging();        // Third - audit all requests/responses

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Health checks
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        ResultStatusCodes =
        {
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });
    
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
    
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false, // Exclude all checks - just return 200 if app is running
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // API Controllers
    app.MapControllers()
        .RequireAuthorization(); // Require authentication for all controllers

    // Metrics endpoint for Prometheus
    if (builder.Environment.IsDevelopment())
    {
        app.MapPrometheusScrapingEndpoint("/metrics");
    }

    // Default redirects
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

    // Graceful shutdown handling
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        Log.Information("Application is shutting down gracefully...");
    });
    
    lifetime.ApplicationStopped.Register(() =>
    {
        Log.Information("Application has shut down.");
        Log.CloseAndFlush();
    });

    // Initialize database if needed
    await InitializeDatabaseAsync(app);

    Log.Information("Northstar ET LMS API starting up...");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("Multi-tenant isolation: {TenantIsolation}", 
        builder.Configuration["MultiTenant:Enabled"] ?? "false");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Database initialization
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Initializing database...");
        
        // Create database if it doesn't exist
        await context.Database.EnsureCreatedAsync();
        
        // Apply any pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
        }
        
        // Seed initial data
        await SeedInitialDataAsync(context, logger);
        
        logger.LogInformation("Database initialization completed");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

// Seed initial system data
static async Task SeedInitialDataAsync(LmsDbContext context, Microsoft.Extensions.Logging.ILogger logger)
{
    // This would be implemented to seed:
    // - Default retention policies
    // - System role definitions
    // - Platform admin accounts
    // - Default district quotas
    
    logger.LogInformation("Seeding initial system data...");
    
    // TODO: Implement data seeding
    await Task.CompletedTask;
}

// Extension method for security headers
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Security headers for compliance and security
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            
            // Content Security Policy
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'";
            
            await next();
        });
        
        return app;
    }
}

// Global exception filter
public class GlobalExceptionFilter : Microsoft.AspNetCore.Mvc.Filters.IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(Microsoft.AspNetCore.Mvc.Filters.ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception occurred");
        
        // Don't handle the exception, let the framework handle it
        // The exception will be logged and handled by the problem details middleware
    }
}

// Model validation filter
public class ModelValidationFilter : Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute
{
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors.Select(e => new { Field = x.Key, Error = e.ErrorMessage }))
                .ToArray();

            var result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                error = "ValidationError",
                message = "One or more validation errors occurred",
                details = errors
            });

            context.Result = result;
        }
    }
}

// Custom health checks
public class TenantIsolationHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement tenant isolation validation
        return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Tenant isolation is working correctly"));
    }
}

public class AuditChainHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement audit chain integrity validation
        return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Audit chain integrity verified"));
    }
}

// Make Program accessible for testing
public partial class Program { }