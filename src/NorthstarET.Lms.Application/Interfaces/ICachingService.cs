namespace NorthstarET.Lms.Application.Interfaces;

/// <summary>
/// Interface for caching services
/// </summary>
public interface ICachingService
{
    Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Cache statistics for monitoring
/// </summary>
public class CacheStatistics
{
    public double HitRatio { get; set; }
    public long TotalOperations { get; set; }
    public Dictionary<CacheLayer, LayerStatistics> LayerStats { get; set; } = new();
}

/// <summary>
/// Cache layer enumeration
/// </summary>
public enum CacheLayer
{
    Memory,
    Redis,
    Database
}

/// <summary>
/// Statistics for a specific cache layer
/// </summary>
public class LayerStatistics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public long EvictionCount { get; set; }
    public double SizeBytes { get; set; }
}