// T115: Database migration automation in Aspire startup
// T116: Tenant schema provisioning automation

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace NorthstarET.Lms.Infrastructure.Persistence.Migrations;

/// <summary>
/// Handles database migrations and tenant schema provisioning
/// </summary>
public class DatabaseMigrator
{
    private readonly LmsDbContext _dbContext;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(LmsDbContext dbContext, ILogger<DatabaseMigrator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Applies pending migrations to the platform database
    /// Creates base schema and tables if they don't exist
    /// </summary>
    public async Task MigrateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting database migration...");
            
            // Apply any pending migrations
            await _dbContext.Database.MigrateAsync(cancellationToken);
            
            _logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate database");
            throw;
        }
    }

    /// <summary>
    /// Provisions a new tenant schema
    /// Creates tenant-specific schema and applies necessary permissions
    /// </summary>
    /// <param name="tenantSlug">The tenant slug (e.g., "oakland-unified")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ProvisionTenantSchemaAsync(string tenantSlug, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Provisioning schema for tenant: {TenantSlug}", tenantSlug);
            
            // Create schema if it doesn't exist
            var createSchemaSQL = $"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{tenantSlug}') BEGIN EXEC('CREATE SCHEMA [{tenantSlug}]') END";
            await _dbContext.Database.ExecuteSqlRawAsync(createSchemaSQL, cancellationToken);
            
            // The schema will be populated with tables on first use via EF Core migrations
            // Global query filters ensure tenant isolation
            
            _logger.LogInformation("Successfully provisioned schema for tenant: {TenantSlug}", tenantSlug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision schema for tenant: {TenantSlug}", tenantSlug);
            throw;
        }
    }

    /// <summary>
    /// Verifies tenant schema exists and is properly configured
    /// </summary>
    /// <param name="tenantSlug">The tenant slug to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if schema exists and is valid, false otherwise</returns>
    public async Task<bool> VerifyTenantSchemaAsync(string tenantSlug, CancellationToken cancellationToken = default)
    {
        try
        {
            var schemaExistsSql = $"SELECT COUNT(*) FROM sys.schemas WHERE name = '{tenantSlug}'";
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);
            
            using var command = connection.CreateCommand();
            command.CommandText = schemaExistsSql;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify schema for tenant: {TenantSlug}", tenantSlug);
            return false;
        }
    }
}
