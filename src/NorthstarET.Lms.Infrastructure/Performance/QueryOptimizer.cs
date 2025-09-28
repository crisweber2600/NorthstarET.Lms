using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace NorthstarET.Lms.Infrastructure.Performance;

/// <summary>
/// Service that analyzes and optimizes database queries for performance
/// Implements PR-003 (audit queries <2s on 1M records) optimization
/// </summary>
public class QueryOptimizer : DbCommandInterceptor, IQueryOptimizer
{
    private readonly ILogger<QueryOptimizer> _logger;
    private readonly QueryOptimizationOptions _options;
    private readonly ConcurrentDictionary<string, QueryPattern> _queryPatterns = new();
    private readonly ConcurrentDictionary<string, QueryOptimizationSuggestion> _optimizationCache = new();

    public QueryOptimizer(ILogger<QueryOptimizer> logger, QueryOptimizationOptions options)
    {
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Analyzes a query and provides optimization suggestions
    /// </summary>
    public async Task<QueryAnalysisResult> AnalyzeQueryAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        var result = new QueryAnalysisResult
        {
            OriginalQuery = sql,
            AnalyzedAt = DateTime.UtcNow
        };

        try
        {
            // Normalize query for pattern matching
            var normalizedQuery = NormalizeQuery(sql);
            var queryHash = ComputeQueryHash(normalizedQuery);

            // Check cache first
            if (_optimizationCache.TryGetValue(queryHash, out var cachedSuggestion))
            {
                result.Suggestions.Add(cachedSuggestion);
                result.FromCache = true;
                return result;
            }

            // Analyze query structure
            result.QueryComplexity = AnalyzeQueryComplexity(sql);
            result.EstimatedCost = EstimateQueryCost(sql);
            result.PotentialIssues = IdentifyPotentialIssues(sql);

            // Generate optimization suggestions
            var suggestions = await GenerateOptimizationSuggestionsAsync(sql, parameters);
            result.Suggestions.AddRange(suggestions);

            // Cache the analysis
            if (suggestions.Any())
            {
                _optimizationCache.TryAdd(queryHash, suggestions.First());
            }

            _logger.LogDebug("Query analysis completed: Complexity={Complexity}, Cost={Cost}, Issues={IssueCount}",
                result.QueryComplexity, result.EstimatedCost, result.PotentialIssues.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing query");
            result.AnalysisError = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Optimizes a query automatically where possible
    /// </summary>
    public async Task<QueryOptimizationResult> OptimizeQueryAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        var result = new QueryOptimizationResult
        {
            OriginalQuery = sql,
            OptimizedQuery = sql, // Start with original
            OptimizedAt = DateTime.UtcNow
        };

        try
        {
            var analysis = await AnalyzeQueryAsync(sql, parameters);
            
            var optimizedSql = sql;
            var appliedOptimizations = new List<string>();

            // Apply automatic optimizations
            foreach (var suggestion in analysis.Suggestions.Where(s => s.CanAutoApply))
            {
                var (newSql, applied) = ApplyOptimization(optimizedSql, suggestion);
                if (applied)
                {
                    optimizedSql = newSql;
                    appliedOptimizations.Add(suggestion.OptimizationType);
                }
            }

            result.OptimizedQuery = optimizedSql;
            result.AppliedOptimizations = appliedOptimizations;
            result.ExpectedImprovement = CalculateExpectedImprovement(analysis.Suggestions);

            _logger.LogInformation("Query optimization completed: Applied {OptimizationCount} optimizations with {ExpectedImprovement}% expected improvement",
                appliedOptimizations.Count, result.ExpectedImprovement);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing query");
            result.OptimizationError = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Gets optimization statistics
    /// </summary>
    public async Task<QueryOptimizationStats> GetOptimizationStatsAsync()
    {
        await Task.CompletedTask;

        return new QueryOptimizationStats
        {
            GeneratedAt = DateTime.UtcNow,
            TotalQueriesAnalyzed = _queryPatterns.Values.Sum(p => p.ExecutionCount),
            UniqueQueryPatterns = _queryPatterns.Count,
            CachedOptimizations = _optimizationCache.Count,
            TopSlowQueries = GetTopSlowQueries(10),
            OptimizationOpportunities = GetOptimizationOpportunities()
        };
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (_options.EnableQueryInterception)
        {
            _ = Task.Run(() => RecordQueryExecutionAsync(command));
        }

        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (_options.EnableQueryInterception)
        {
            _ = Task.Run(() => RecordQueryExecutionAsync(command), cancellationToken);
        }

        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    private async Task RecordQueryExecutionAsync(DbCommand command)
    {
        try
        {
            var sql = command.CommandText;
            var normalizedQuery = NormalizeQuery(sql);
            var queryHash = ComputeQueryHash(normalizedQuery);

            var pattern = _queryPatterns.GetOrAdd(queryHash, _ => new QueryPattern
            {
                QueryHash = queryHash,
                NormalizedQuery = normalizedQuery,
                FirstSeen = DateTime.UtcNow
            });

            pattern.ExecutionCount++;
            pattern.LastExecuted = DateTime.UtcNow;

            // Analyze for optimization if it's a frequently executed query
            if (pattern.ExecutionCount % _options.AnalysisThreshold == 0)
            {
                await AnalyzeQueryAsync(sql);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording query execution");
        }
    }

    private string NormalizeQuery(string sql)
    {
        // Remove comments and normalize whitespace
        var normalized = Regex.Replace(sql, @"--.*?$", "", RegexOptions.Multiline);
        normalized = Regex.Replace(normalized, @"/\*.*?\*/", "", RegexOptions.Singleline);
        normalized = Regex.Replace(normalized, @"\s+", " ");
        
        // Normalize parameter placeholders
        normalized = Regex.Replace(normalized, @"@\w+", "@param");
        normalized = Regex.Replace(normalized, @"\?\d*", "?");
        
        return normalized.Trim().ToUpperInvariant();
    }

    private string ComputeQueryHash(string normalizedQuery)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(normalizedQuery));
        return Convert.ToBase64String(hash)[..16]; // Use first 16 characters
    }

    private QueryComplexity AnalyzeQueryComplexity(string sql)
    {
        var sqlUpper = sql.ToUpperInvariant();
        var complexity = QueryComplexity.Simple;

        // Count joins
        var joinCount = Regex.Matches(sqlUpper, @"\bJOIN\b").Count;
        if (joinCount > 3) complexity = QueryComplexity.High;
        else if (joinCount > 1) complexity = QueryComplexity.Medium;

        // Check for subqueries
        var subqueryCount = Regex.Matches(sqlUpper, @"\bSELECT\b").Count - 1;
        if (subqueryCount > 2) complexity = QueryComplexity.High;
        else if (subqueryCount > 0 && complexity < QueryComplexity.Medium) 
            complexity = QueryComplexity.Medium;

        // Check for complex operations
        var complexOperations = new[] { "UNION", "INTERSECT", "EXCEPT", "RECURSIVE", "WINDOW" };
        if (complexOperations.Any(op => sqlUpper.Contains(op)))
            complexity = QueryComplexity.High;

        return complexity;
    }

    private int EstimateQueryCost(string sql)
    {
        var sqlUpper = sql.ToUpperInvariant();
        var cost = 1;

        // Base cost adjustments
        cost += Regex.Matches(sqlUpper, @"\bJOIN\b").Count * 10;
        cost += Regex.Matches(sqlUpper, @"\bSELECT\b").Count * 5;
        cost += Regex.Matches(sqlUpper, @"\bGROUP BY\b").Count * 15;
        cost += Regex.Matches(sqlUpper, @"\bORDER BY\b").Count * 10;

        // Expensive operations
        if (sqlUpper.Contains("DISTINCT")) cost += 20;
        if (sqlUpper.Contains("UNION")) cost += 25;
        if (sqlUpper.Contains("LIKE '%")) cost += 30; // Non-leading wildcard

        return cost;
    }

    private List<QueryIssue> IdentifyPotentialIssues(string sql)
    {
        var issues = new List<QueryIssue>();
        var sqlUpper = sql.ToUpperInvariant();

        // Check for common performance issues
        if (sqlUpper.Contains("SELECT *"))
        {
            issues.Add(new QueryIssue
            {
                IssueType = "SELECT_ALL",
                Severity = IssueSeverity.Medium,
                Description = "Using SELECT * may retrieve unnecessary columns",
                Suggestion = "Specify only required columns"
            });
        }

        if (Regex.IsMatch(sqlUpper, @"LIKE\s+'%\w"))
        {
            issues.Add(new QueryIssue
            {
                IssueType = "LEADING_WILDCARD",
                Severity = IssueSeverity.High,
                Description = "Leading wildcard in LIKE prevents index usage",
                Suggestion = "Avoid leading wildcards or use full-text search"
            });
        }

        if (!sqlUpper.Contains("WHERE") && sqlUpper.Contains("SELECT"))
        {
            issues.Add(new QueryIssue
            {
                IssueType = "MISSING_WHERE",
                Severity = IssueSeverity.High,
                Description = "Query lacks WHERE clause, may scan entire table",
                Suggestion = "Add appropriate WHERE clause for filtering"
            });
        }

        if (Regex.Matches(sqlUpper, @"\bOR\b").Count > 3)
        {
            issues.Add(new QueryIssue
            {
                IssueType = "EXCESSIVE_OR",
                Severity = IssueSeverity.Medium,
                Description = "Multiple OR conditions may prevent index usage",
                Suggestion = "Consider using UNION or IN clause"
            });
        }

        // Multi-tenant specific issues
        if (!sqlUpper.Contains("TENANTID"))
        {
            issues.Add(new QueryIssue
            {
                IssueType = "MISSING_TENANT_FILTER",
                Severity = IssueSeverity.Critical,
                Description = "Query lacks tenant filtering",
                Suggestion = "Add TenantId filter for multi-tenant isolation"
            });
        }

        return issues;
    }

    private async Task<List<QueryOptimizationSuggestion>> GenerateOptimizationSuggestionsAsync(
        string sql, Dictionary<string, object>? parameters)
    {
        var suggestions = new List<QueryOptimizationSuggestion>();
        var sqlUpper = sql.ToUpperInvariant();

        // Index suggestions
        var indexSuggestions = await AnalyzeIndexNeedsAsync(sql);
        suggestions.AddRange(indexSuggestions);

        // Query rewrite suggestions
        if (sqlUpper.Contains("SELECT *"))
        {
            suggestions.Add(new QueryOptimizationSuggestion
            {
                OptimizationType = "COLUMN_SELECTION",
                Priority = OptimizationPriority.High,
                Description = "Replace SELECT * with specific columns",
                ExpectedImprovementPercent = 15,
                CanAutoApply = false,
                Implementation = "Specify only required columns in SELECT clause"
            });
        }

        // Pagination suggestions
        if (sqlUpper.Contains("ORDER BY") && !sqlUpper.Contains("OFFSET") && !sqlUpper.Contains("LIMIT"))
        {
            suggestions.Add(new QueryOptimizationSuggestion
            {
                OptimizationType = "PAGINATION",
                Priority = OptimizationPriority.Medium,
                Description = "Add pagination to limit result set size",
                ExpectedImprovementPercent = 25,
                CanAutoApply = false,
                Implementation = "Add OFFSET/LIMIT or equivalent pagination"
            });
        }

        // Caching suggestions for frequently executed queries
        if (_queryPatterns.Values.Any(p => p.ExecutionCount > _options.FrequentQueryThreshold))
        {
            suggestions.Add(new QueryOptimizationSuggestion
            {
                OptimizationType = "CACHING",
                Priority = OptimizationPriority.Medium,
                Description = "Consider caching results for frequently executed query",
                ExpectedImprovementPercent = 80,
                CanAutoApply = false,
                Implementation = "Implement result caching with appropriate TTL"
            });
        }

        return suggestions;
    }

    private async Task<List<QueryOptimizationSuggestion>> AnalyzeIndexNeedsAsync(string sql)
    {
        var suggestions = new List<QueryOptimizationSuggestion>();
        
        // Analyze WHERE clauses for missing indexes
        var whereMatches = Regex.Matches(sql, @"WHERE\s+(\w+\.)?(\w+)\s*(=|>|<|>=|<=|LIKE)", RegexOptions.IgnoreCase);
        
        foreach (Match match in whereMatches)
        {
            var column = match.Groups[2].Value;
            if (IsCommonIndexCandidate(column))
            {
                suggestions.Add(new QueryOptimizationSuggestion
                {
                    OptimizationType = "INDEX_CREATION",
                    Priority = OptimizationPriority.High,
                    Description = $"Consider creating index on column '{column}'",
                    ExpectedImprovementPercent = 40,
                    CanAutoApply = false,
                    Implementation = $"CREATE INDEX IX_{column} ON table_name ({column})"
                });
            }
        }

        await Task.CompletedTask;
        return suggestions;
    }

    private bool IsCommonIndexCandidate(string column)
    {
        var commonFilterColumns = new[] { "Id", "TenantId", "CreatedAt", "Status", "UserId", "Email", "StudentNumber" };
        return commonFilterColumns.Any(c => column.Contains(c, StringComparison.OrdinalIgnoreCase));
    }

    private (string newSql, bool applied) ApplyOptimization(string sql, QueryOptimizationSuggestion suggestion)
    {
        switch (suggestion.OptimizationType)
        {
            case "LIMIT_ADDITION":
                if (!sql.ToUpperInvariant().Contains("LIMIT") && !sql.ToUpperInvariant().Contains("TOP"))
                {
                    return ($"{sql.TrimEnd(';')} LIMIT 1000", true);
                }
                break;

            case "TENANT_FILTER":
                if (!sql.ToUpperInvariant().Contains("TENANTID"))
                {
                    // This would require more context to apply safely
                    return (sql, false);
                }
                break;
        }

        return (sql, false);
    }

    private double CalculateExpectedImprovement(List<QueryOptimizationSuggestion> suggestions)
    {
        if (!suggestions.Any()) return 0;

        // Weighted average of expected improvements
        var totalWeight = suggestions.Sum(s => (int)s.Priority);
        var weightedSum = suggestions.Sum(s => s.ExpectedImprovementPercent * (int)s.Priority);

        return totalWeight > 0 ? weightedSum / totalWeight : 0;
    }

    private List<SlowQueryInfo> GetTopSlowQueries(int count)
    {
        return _queryPatterns.Values
            .OrderByDescending(p => p.ExecutionCount)
            .Take(count)
            .Select(p => new SlowQueryInfo
            {
                QueryHash = p.QueryHash,
                NormalizedQuery = p.NormalizedQuery,
                ExecutionCount = p.ExecutionCount,
                LastExecuted = p.LastExecuted
            })
            .ToList();
    }

    private List<OptimizationOpportunity> GetOptimizationOpportunities()
    {
        return _optimizationCache.Values
            .GroupBy(o => o.OptimizationType)
            .Select(g => new OptimizationOpportunity
            {
                OptimizationType = g.Key,
                Count = g.Count(),
                AverageImpact = g.Average(o => o.ExpectedImprovementPercent)
            })
            .OrderByDescending(o => o.Count * o.AverageImpact)
            .ToList();
    }
}

/// <summary>
/// Interface for query optimization service
/// </summary>
public interface IQueryOptimizer
{
    Task<QueryAnalysisResult> AnalyzeQueryAsync(string sql, Dictionary<string, object>? parameters = null);
    Task<QueryOptimizationResult> OptimizeQueryAsync(string sql, Dictionary<string, object>? parameters = null);
    Task<QueryOptimizationStats> GetOptimizationStatsAsync();
}

/// <summary>
/// Query analysis result
/// </summary>
public class QueryAnalysisResult
{
    public string OriginalQuery { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
    public QueryComplexity QueryComplexity { get; set; }
    public int EstimatedCost { get; set; }
    public List<QueryIssue> PotentialIssues { get; set; } = new();
    public List<QueryOptimizationSuggestion> Suggestions { get; set; } = new();
    public bool FromCache { get; set; }
    public string? AnalysisError { get; set; }
}

/// <summary>
/// Query optimization result
/// </summary>
public class QueryOptimizationResult
{
    public string OriginalQuery { get; set; } = "";
    public string OptimizedQuery { get; set; } = "";
    public DateTime OptimizedAt { get; set; }
    public List<string> AppliedOptimizations { get; set; } = new();
    public double ExpectedImprovement { get; set; }
    public string? OptimizationError { get; set; }
}

/// <summary>
/// Query optimization statistics
/// </summary>
public class QueryOptimizationStats
{
    public DateTime GeneratedAt { get; set; }
    public long TotalQueriesAnalyzed { get; set; }
    public int UniqueQueryPatterns { get; set; }
    public int CachedOptimizations { get; set; }
    public List<SlowQueryInfo> TopSlowQueries { get; set; } = new();
    public List<OptimizationOpportunity> OptimizationOpportunities { get; set; } = new();
}

/// <summary>
/// Query pattern tracking
/// </summary>
public class QueryPattern
{
    public string QueryHash { get; set; } = "";
    public string NormalizedQuery { get; set; } = "";
    public long ExecutionCount { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastExecuted { get; set; }
}

/// <summary>
/// Query optimization suggestion
/// </summary>
public class QueryOptimizationSuggestion
{
    public string OptimizationType { get; set; } = "";
    public OptimizationPriority Priority { get; set; }
    public string Description { get; set; } = "";
    public double ExpectedImprovementPercent { get; set; }
    public bool CanAutoApply { get; set; }
    public string Implementation { get; set; } = "";
}

/// <summary>
/// Query issue identification
/// </summary>
public class QueryIssue
{
    public string IssueType { get; set; } = "";
    public IssueSeverity Severity { get; set; }
    public string Description { get; set; } = "";
    public string Suggestion { get; set; } = "";
}

/// <summary>
/// Slow query information
/// </summary>
public class SlowQueryInfo
{
    public string QueryHash { get; set; } = "";
    public string NormalizedQuery { get; set; } = "";
    public long ExecutionCount { get; set; }
    public DateTime LastExecuted { get; set; }
}

/// <summary>
/// Optimization opportunity
/// </summary>
public class OptimizationOpportunity
{
    public string OptimizationType { get; set; } = "";
    public int Count { get; set; }
    public double AverageImpact { get; set; }
}

/// <summary>
/// Query optimization options
/// </summary>
public class QueryOptimizationOptions
{
    public bool EnableQueryInterception { get; set; } = true;
    public int AnalysisThreshold { get; set; } = 10; // Analyze after N executions
    public int FrequentQueryThreshold { get; set; } = 100;
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Enums for query analysis
/// </summary>
public enum QueryComplexity { Simple, Medium, High }
public enum OptimizationPriority { Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum IssueSeverity { Low, Medium, High, Critical }