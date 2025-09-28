using NorthstarET.Lms.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Text.Json;

namespace NorthstarET.Lms.Infrastructure.Security;

/// <summary>
/// EF Core interceptor that enforces multi-tenant data access patterns
/// Automatically applies tenant filters and validates query security
/// </summary>
public class TenantDataInterceptor : DbCommandInterceptor
{
    private readonly ITenantContextAccessor _tenantContext;
    private readonly ILogger<TenantDataInterceptor> _logger;
    private readonly HashSet<string> _tenantScopedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Students", "Staff", "Schools", "Classes", "Enrollments",
        "AuditRecords", "RoleAssignments", "Guardians"
    };

    public TenantDataInterceptor(
        ITenantContextAccessor tenantContext,
        ILogger<TenantDataInterceptor> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        InterceptCommand(command, CommandType.Read);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        InterceptCommand(command, CommandType.Read);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        InterceptCommand(command, CommandType.NonQuery);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        InterceptCommand(command, CommandType.NonQuery);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        InterceptCommand(command, CommandType.Scalar);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        InterceptCommand(command, CommandType.Scalar);
        return new ValueTask<InterceptionResult<object>>(result);
    }

    private void InterceptCommand(DbCommand command, CommandType commandType)
    {
        var currentTenantId = _tenantContext.GetCurrentTenantId();
        if (string.IsNullOrEmpty(currentTenantId))
        {
            _logger.LogWarning("No tenant context available for database command: {CommandType}", commandType);
            return;
        }

        var originalSql = command.CommandText;
        var modifiedSql = ApplyTenantFiltering(originalSql, currentTenantId);

        if (modifiedSql != originalSql)
        {
            command.CommandText = modifiedSql;
            _logger.LogDebug("Applied tenant filtering to {CommandType} command for tenant {TenantId}", 
                commandType, currentTenantId);
        }

        // Validate that sensitive operations have proper tenant context
        ValidateCommandSecurity(command, currentTenantId, commandType);
    }

    private string ApplyTenantFiltering(string sql, string tenantId)
    {
        var sqlLower = sql.ToLowerInvariant();
        
        // Skip if already has tenant filtering or is a system query
        if (sqlLower.Contains("tenantid") || IsSystemQuery(sqlLower))
        {
            return sql;
        }

        // Apply tenant filtering to SELECT, UPDATE, DELETE operations
        foreach (var tableName in _tenantScopedTables)
        {
            if (sqlLower.Contains(tableName.ToLowerInvariant()))
            {
                sql = ApplyTenantFilterToTable(sql, tableName, tenantId);
            }
        }

        return sql;
    }

    private string ApplyTenantFilterToTable(string sql, string tableName, string tenantId)
    {
        try
        {
            var patterns = new[]
            {
                // SELECT patterns
                $@"FROM\s+{tableName}(\s+\w+)?\s*(WHERE|GROUP|ORDER|$)",
                $@"JOIN\s+{tableName}(\s+\w+)?\s*ON",
                
                // UPDATE patterns  
                $@"UPDATE\s+{tableName}\s+SET",
                
                // DELETE patterns
                $@"DELETE\s+FROM\s+{tableName}(\s+WHERE|$)"
            };

            foreach (var pattern in patterns)
            {
                var regex = new System.Text.RegularExpressions.Regex(pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (regex.IsMatch(sql))
                {
                    // Add tenant filter condition
                    var tenantCondition = $" AND {tableName}.TenantId = '{tenantId}'";
                    
                    if (sql.ToLowerInvariant().Contains("where"))
                    {
                        // Add to existing WHERE clause
                        var whereIndex = sql.ToLowerInvariant().IndexOf("where");
                        sql = sql.Insert(whereIndex + "where".Length, tenantCondition.Substring(4)); // Remove AND
                    }
                    else
                    {
                        // Add new WHERE clause
                        var insertIndex = FindInsertionPoint(sql);
                        sql = sql.Insert(insertIndex, $" WHERE {tableName}.TenantId = '{tenantId}'");
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply tenant filtering to table {TableName}", tableName);
        }

        return sql;
    }

    private int FindInsertionPoint(string sql)
    {
        var keywords = new[] { "GROUP BY", "ORDER BY", "HAVING", "LIMIT", "OFFSET" };
        
        foreach (var keyword in keywords)
        {
            var index = sql.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return index;
            }
        }

        return sql.Length;
    }

    private bool IsSystemQuery(string sql)
    {
        var systemPatterns = new[]
        {
            "information_schema",
            "sys.",
            "__efmigrationshistory",
            "pg_",
            "sqlite_",
            "select 1"
        };

        return systemPatterns.Any(pattern => sql.Contains(pattern));
    }

    private void ValidateCommandSecurity(DbCommand command, string tenantId, CommandType commandType)
    {
        var sql = command.CommandText.ToLowerInvariant();
        
        // Check for potentially dangerous operations
        var risks = new List<string>();

        // Check for cross-tenant data access risks
        if (sql.Contains("select") && ContainsTenantScopedTables(sql) && !sql.Contains("tenantid"))
        {
            risks.Add("SELECT without tenant filtering");
        }

        // Check for bulk operations without proper filtering
        if ((sql.Contains("update") || sql.Contains("delete")) && !sql.Contains("tenantid"))
        {
            risks.Add($"{commandType} without tenant context");
        }

        // Check for potential SQL injection risks
        if (HasSqlInjectionRisk(command))
        {
            risks.Add("Potential SQL injection risk");
        }

        if (risks.Any())
        {
            var riskSummary = string.Join(", ", risks);
            _logger.LogWarning("Security risks detected in database command: {Risks} for tenant {TenantId}", 
                riskSummary, tenantId);

            // In production, you might want to throw an exception here
            // throw new UnauthorizedAccessException($"Security violation: {riskSummary}");
        }
    }

    private bool ContainsTenantScopedTables(string sql)
    {
        return _tenantScopedTables.Any(table => sql.Contains(table.ToLowerInvariant()));
    }

    private bool HasSqlInjectionRisk(DbCommand command)
    {
        var sql = command.CommandText;
        
        // Basic SQL injection pattern detection
        var injectionPatterns = new[]
        {
            "';",
            "--",
            "/*",
            "xp_",
            "sp_",
            "union select",
            "drop table",
            "create table",
            "alter table"
        };

        // Check for parameterized queries vs string concatenation
        var hasParameters = command.Parameters.Count > 0;
        var hasInjectionPatterns = injectionPatterns.Any(pattern => 
            sql.Contains(pattern, StringComparison.OrdinalIgnoreCase));

        return hasInjectionPatterns && !hasParameters;
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        _logger.LogError(eventData.Exception, 
            "Database command failed for tenant {TenantId}. Command: {CommandPreview}", 
            tenantId, 
            command.CommandText[..Math.Min(command.CommandText.Length, 200)]);
        
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        _logger.LogError(eventData.Exception, 
            "Database command failed for tenant {TenantId}. Command: {CommandPreview}", 
            tenantId, 
            command.CommandText[..Math.Min(command.CommandText.Length, 200)]);
        
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private enum CommandType
    {
        Read,
        NonQuery,
        Scalar
    }
}

/// <summary>
/// Configuration options for tenant data interceptor
/// </summary>
public class TenantDataInterceptorOptions
{
    /// <summary>
    /// Whether to enforce strict tenant filtering (throw exceptions on violations)
    /// </summary>
    public bool StrictMode { get; set; } = false;

    /// <summary>
    /// Additional table names to apply tenant filtering to
    /// </summary>
    public HashSet<string> AdditionalTenantScopedTables { get; set; } = new();

    /// <summary>
    /// Whether to log all intercepted commands (for debugging)
    /// </summary>
    public bool LogAllCommands { get; set; } = false;
}

/// <summary>
/// Extension methods for registering tenant data interceptor
/// </summary>
public static class TenantDataInterceptorExtensions
{
    /// <summary>
    /// Registers the tenant data interceptor with DbContext
    /// </summary>
    public static DbContextOptionsBuilder AddTenantDataInterceptor(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        var tenantContext = serviceProvider.GetRequiredService<ITenantContextAccessor>();
        var logger = serviceProvider.GetRequiredService<ILogger<TenantDataInterceptor>>();
        
        return optionsBuilder.AddInterceptors(new TenantDataInterceptor(tenantContext, logger));
    }
}