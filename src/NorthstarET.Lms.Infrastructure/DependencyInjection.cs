using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Infrastructure.BackgroundServices;
using NorthstarET.Lms.Infrastructure.Data;
using NorthstarET.Lms.Infrastructure.ExternalServices;
using NorthstarET.Lms.Infrastructure.Repositories;
using NorthstarET.Lms.Infrastructure.Security;

namespace NorthstarET.Lms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Context
        services.AddDbContext<LmsDbContext>((serviceProvider, options) =>
        {
            var tenantContextAccessor = serviceProvider.GetRequiredService<ITenantContextAccessor>();
            var tenant = tenantContextAccessor.GetTenant();
            
            var connectionString = tenant?.ConnectionString 
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No database connection string configured");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", tenant?.SchemaName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });

            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(configuration.GetValue<bool>("DetailedErrors", false));
        });

        // Tenant Context
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddHttpContextAccessor();

        // Unit of Work
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<LmsDbContext>());

        // Repository Registrations
        services.AddScoped<IDistrictRepository, DistrictRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IPlatformAuditRepository, PlatformAuditRepository>();

        // External Services
        AddExternalServices(services, configuration);

        // Background Services
        services.AddHostedService<RetentionJobService>();
        services.AddHostedService<AuditProcessorService>();

        return services;
    }

    private static void AddExternalServices(IServiceCollection services, IConfiguration configuration)
    {
        // Microsoft Graph Configuration
        var graphSection = configuration.GetSection("MicrosoftGraph");
        if (graphSection.Exists())
        {
            services.AddSingleton<GraphServiceClient>(provider =>
            {
                var clientId = graphSection.GetValue<string>("ClientId");
                var clientSecret = graphSection.GetValue<string>("ClientSecret");
                var tenantId = graphSection.GetValue<string>("TenantId");

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
                {
                    throw new InvalidOperationException("Microsoft Graph configuration is incomplete");
                }

                var options = new Azure.Identity.ClientSecretCredentialOptions
                {
                    AuthorityHost = Azure.Identity.AzureAuthorityHosts.AzurePublicCloud,
                };

                var clientSecretCredential = new Azure.Identity.ClientSecretCredential(
                    tenantId, clientId, clientSecret, options);

                return new GraphServiceClient(clientSecretCredential);
            });

            services.AddScoped<IIdentityProvider, EntraIdentityService>();
        }

        // Azure Blob Storage Configuration
        var storageSection = configuration.GetSection("AzureStorage");
        if (storageSection.Exists())
        {
            services.AddSingleton<BlobServiceClient>(provider =>
            {
                var connectionString = storageSection.GetValue<string>("ConnectionString");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Azure Storage connection string is required");
                }

                return new BlobServiceClient(connectionString);
            });

            services.AddScoped<IAssessmentFileService, AssessmentFileService>();
        }
    }

    public static async Task<IServiceProvider> EnsureDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        
        // Ensure database is created and up to date
        await dbContext.Database.EnsureCreatedAsync();
        
        return serviceProvider;
    }
}