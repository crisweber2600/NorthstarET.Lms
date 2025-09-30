// T115: Database migration automation in Aspire startup
// Hosted service that runs database migrations on application startup

using NorthstarET.Lms.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NorthstarET.Lms.Presentation.Api.HostedServices;

/// <summary>
/// Background service that applies database migrations on startup
/// Ensures database schema is up-to-date before application starts serving requests
/// </summary>
public class DatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationHostedService> _logger;

    public DatabaseMigrationHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting database migration service...");

        using var scope = _serviceProvider.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();

        try
        {
            // Apply pending migrations
            await migrator.MigrateDatabaseAsync(cancellationToken);
            
            _logger.LogInformation("Database migration service completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Database migration failed. Application startup aborted.");
            throw; // Fail fast if migrations fail
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database migration service stopped");
        return Task.CompletedTask;
    }
}
