using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NorthstarET.Lms.Infrastructure.Data;
using NorthstarET.Lms.Infrastructure.Data.Seeding;
using System.Data;
using Microsoft.Data.SqlClient;

namespace NorthstarET.Lms.Infrastructure.Data.Initialization;

/// <summary>
/// Database initialization service responsible for:
/// - Platform-level database setup and migrations
/// - Tenant schema provisioning
/// - Initial data seeding for system defaults
/// - Tenant-specific data seeding
/// </summary>
public class DatabaseInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initialize the platform-level database and apply migrations
    /// </summary>
    public async Task InitializePlatformDatabaseAsync()
    {
        _logger.LogInformation("Starting platform database initialization...");

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var contextOptions = new DbContextOptionsBuilder<LmsDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            using var context = new LmsDbContext(contextOptions);

            // Ensure platform database exists
            await context.Database.EnsureCreatedAsync();
            _logger.LogInformation("Platform database ensured to exist");

            // Apply any pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Applying {MigrationCount} pending migrations: {Migrations}",
                    pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                
                await context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                _logger.LogInformation("No pending migrations found");
            }

            // Seed platform-level system data
            await SeedPlatformSystemDataAsync(context);

            _logger.LogInformation("Platform database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during platform database initialization");
            throw;
        }
    }

    /// <summary>
    /// Provision a new tenant schema with complete data isolation
    /// </summary>
    public async Task<bool> ProvisionTenantAsync(string tenantSlug, string displayName, DistrictQuotas quotas)
    {
        _logger.LogInformation("Provisioning tenant schema for: {TenantSlug} - {DisplayName}", tenantSlug, displayName);

        try
        {
            // Validate tenant slug format
            if (!IsValidTenantSlug(tenantSlug))
            {
                _logger.LogError("Invalid tenant slug format: {TenantSlug}", tenantSlug);
                return false;
            }

            var schemaName = ConvertSlugToSchemaName(tenantSlug);
            _logger.LogInformation("Using schema name: {SchemaName}", schemaName);

            // Check if tenant already exists
            if (await TenantExistsAsync(tenantSlug))
            {
                _logger.LogWarning("Tenant already exists: {TenantSlug}", tenantSlug);
                return false;
            }

            var connectionString = _configuration.GetConnectionString("TenantConnection");
            
            // Execute tenant schema provisioning script
            await ExecuteTenantProvisioningScript(connectionString, tenantSlug, schemaName, displayName);

            // Create tenant-specific DbContext and seed data
            var tenantContextOptions = new DbContextOptionsBuilder<LmsDbContext>()
                .UseSqlServer(connectionString, options =>
                {
                    options.MigrationsHistoryTable("__EFMigrationsHistory", schemaName);
                })
                .Options;

            using var tenantContext = new LmsDbContext(tenantContextOptions);
            
            // Seed tenant-specific system data
            await SeedTenantSystemDataAsync(tenantContext, tenantSlug, displayName);

            _logger.LogInformation("Tenant provisioning completed successfully: {TenantSlug}", tenantSlug);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning tenant: {TenantSlug}", tenantSlug);
            return false;
        }
    }

    /// <summary>
    /// Validate if a tenant is properly provisioned and accessible
    /// </summary>
    public async Task<bool> ValidateTenantProvisioningAsync(string tenantSlug)
    {
        _logger.LogInformation("Validating tenant provisioning: {TenantSlug}", tenantSlug);

        try
        {
            var schemaName = ConvertSlugToSchemaName(tenantSlug);
            var connectionString = _configuration.GetConnectionString("TenantConnection");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Check if schema exists
            var schemaExistsQuery = @"
                SELECT COUNT(1) 
                FROM sys.schemas 
                WHERE name = @SchemaName";

            using var schemaCommand = new SqlCommand(schemaExistsQuery, connection);
            schemaCommand.Parameters.AddWithValue("@SchemaName", schemaName);
            
            var schemaExists = (int)await schemaCommand.ExecuteScalarAsync() > 0;

            if (!schemaExists)
            {
                _logger.LogWarning("Schema does not exist for tenant: {TenantSlug}", tenantSlug);
                return false;
            }

            // Check if core tables exist in the schema
            var coreTablesExistQuery = @"
                SELECT COUNT(1) 
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE s.name = @SchemaName 
                AND t.name IN ('students', 'staff', 'schools', 'classes', 'enrollments', 'audit_records')";

            using var tablesCommand = new SqlCommand(coreTablesExistQuery, connection);
            tablesCommand.Parameters.AddWithValue("@SchemaName", schemaName);
            
            var tableCount = (int)await tablesCommand.ExecuteScalarAsync();

            if (tableCount < 6) // Should have at least 6 core tables
            {
                _logger.LogWarning("Core tables missing for tenant: {TenantSlug} (found {TableCount})", tenantSlug, tableCount);
                return false;
            }

            // Verify tenant record exists in platform database
            var tenantExists = await TenantExistsAsync(tenantSlug);
            if (!tenantExists)
            {
                _logger.LogWarning("Tenant record not found in platform database: {TenantSlug}", tenantSlug);
                return false;
            }

            _logger.LogInformation("Tenant validation successful: {TenantSlug}", tenantSlug);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating tenant provisioning: {TenantSlug}", tenantSlug);
            return false;
        }
    }

    #region Private Helper Methods

    private async Task SeedPlatformSystemDataAsync(LmsDbContext context)
    {
        _logger.LogInformation("Seeding platform system data...");

        // Seed default retention policies
        await RetentionPolicySeeder.SeedAsync(context);

        // Seed system role definitions  
        await RoleDefinitionSeeder.SeedAsync(context);

        _logger.LogInformation("Platform system data seeding completed");
    }

    private async Task SeedTenantSystemDataAsync(LmsDbContext context, string tenantSlug, string displayName)
    {
        _logger.LogInformation("Seeding tenant system data for: {TenantSlug}", tenantSlug);

        // Seed tenant-specific retention policies (if any overrides needed)
        await RetentionPolicySeeder.SeedDistrictSpecificPoliciesAsync(context, tenantSlug, displayName);

        // Seed tenant-specific role definitions (if any custom roles needed)
        await RoleDefinitionSeeder.SeedDistrictCustomRolesAsync(context, tenantSlug, displayName);

        _logger.LogInformation("Tenant system data seeding completed for: {TenantSlug}", tenantSlug);
    }

    private async Task ExecuteTenantProvisioningScript(string connectionString, string tenantSlug, string schemaName, string displayName)
    {
        _logger.LogInformation("Executing tenant provisioning script for schema: {SchemaName}", schemaName);

        // Load the SQL script template
        var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
            "Data", "Scripts", "ProvisionTenantSchema.sql");
        
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Tenant provisioning script not found: {scriptPath}");
        }

        var scriptTemplate = await File.ReadAllTextAsync(scriptPath);
        
        // Replace placeholders with actual values
        var script = scriptTemplate
            .Replace("{TENANT_SLUG}", tenantSlug)
            .Replace("{SCHEMA_NAME}", schemaName)
            .Replace("{DISPLAY_NAME}", displayName);

        // Execute the provisioning script
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Split and execute SQL commands (handle GO statements)
            var commands = script.Split(new[] { "\nGO\n", "\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var commandText in commands)
            {
                if (string.IsNullOrWhiteSpace(commandText)) continue;

                using var command = new SqlCommand(commandText, connection, transaction);
                command.CommandTimeout = 300; // 5 minute timeout for schema creation
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Tenant provisioning script executed successfully");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<bool> TenantExistsAsync(string tenantSlug)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = "SELECT COUNT(1) FROM district_tenants WHERE slug = @TenantSlug";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@TenantSlug", tenantSlug);

        var count = (int)await command.ExecuteScalarAsync();
        return count > 0;
    }

    private static bool IsValidTenantSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        if (slug.Length > 100) return false;
        
        // Must contain only lowercase letters, numbers, and hyphens
        return System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9-]+$");
    }

    private static string ConvertSlugToSchemaName(string slug)
    {
        // Convert kebab-case to snake_case for SQL schema names
        return slug.Replace('-', '_');
    }

    #endregion
}

/// <summary>
/// District quota configuration for tenant provisioning
/// </summary>
public class DistrictQuotas
{
    public int MaxStudents { get; set; } = 50000;
    public int MaxStaff { get; set; } = 5000;
    public int MaxAdmins { get; set; } = 100;
}

/// <summary>
/// Extension methods for database initialization
/// </summary>
public static class DatabaseInitializerExtensions
{
    public static IServiceCollection AddDatabaseInitialization(this IServiceCollection services)
    {
        services.AddScoped<DatabaseInitializer>();
        return services;
    }
}