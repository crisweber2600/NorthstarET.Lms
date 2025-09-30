using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Shared;
using FluentValidation;
using NorthstarET.Lms.Infrastructure.Audit;
using NorthstarET.Lms.Infrastructure.BackgroundJobs;
using NorthstarET.Lms.Infrastructure.Files;
using NorthstarET.Lms.Infrastructure.Identity;
using NorthstarET.Lms.Infrastructure.Persistence;
using NorthstarET.Lms.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NorthstarET.Lms.Presentation.Api.CompositionRoot;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Domain layer has no services - only pure entities and value objects
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR for CQRS - scan the Application assembly for handlers
        var applicationAssembly = typeof(NorthstarET.Lms.Application.Commands.Districts.CreateDistrictCommand).Assembly;
        
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
            
            // Register pipeline behaviors
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(NorthstarET.Lms.Application.Behaviors.ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(NorthstarET.Lms.Application.Behaviors.LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(NorthstarET.Lms.Application.Behaviors.TenantScopingBehavior<,>));
        });

        // FluentValidation validators - scan the Application assembly
        services.AddValidatorsFromAssembly(applicationAssembly);

        // Application service interfaces (to be implemented)
        // These will be placeholder implementations until handlers are complete
        // services.AddScoped<IDistrictManagementService, DistrictManagementService>();
        // services.AddScoped<IIdentityMappingService, IdentityMappingService>();
        // ... other services as needed

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database context with connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=LmsDb;Trusted_Connection=true;TrustServerCertificate=true";
        
        services.AddDbContext<LmsDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));

        // Tenant context accessor
        services.AddScoped<ITenantContext, TenantContextAccessor>();

        // Note: Repository interfaces are defined in Application.Abstractions
        // Implementations are in Infrastructure.Persistence.Repositories
        // For now, DbContext will be used directly by services

        // External services with configuration
        services.Configure<IdentityMappingOptions>(configuration.GetSection("IdentityMapping"));
        services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
        services.Configure<AuditOptions>(configuration.GetSection("Audit"));

        // Register infrastructure services that implement Application.Abstractions interfaces
        services.AddScoped<IIdentityMappingService, IdentityMappingService>();
        services.AddScoped<IAssessmentService, AssessmentFileService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IBulkOperationService, BackgroundJobService>();

        return services;
    }

    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // HTTP context accessor for tenant resolution
        services.AddHttpContextAccessor();

        // Controllers
        services.AddControllers();

        // API documentation
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "Foundational LMS API",
                Version = "v1",
                Description = "Learning Management System with multi-tenant isolation and FERPA compliance"
            });
            
            // Add tenant header parameter to all operations
            options.AddSecurityDefinition("Tenant", new()
            {
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = "X-Tenant-Slug",
                Description = "Tenant identifier (district slug)"
            });
        });

        return services;
    }
}

// Configuration options classes
public class IdentityMappingOptions
{
    public string GraphApiEndpoint { get; set; } = "https://graph.microsoft.com/v1.0";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class FileStorageOptions
{
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
    public string ContainerName { get; set; } = "assessments";
    public long MaxFileSizeBytes { get; set; } = 104_857_600; // 100MB
}

public class AuditOptions
{
    public bool EnableIntegrityChecks { get; set; } = true;
    public int RetentionDays { get; set; } = 2555; // 7 years for FERPA
}
