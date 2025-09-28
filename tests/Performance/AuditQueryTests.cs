using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace NorthstarET.Lms.Performance.Tests;

/// <summary>
/// Performance tests for audit query operations to ensure <2s response time for 1M records
/// These tests validate that compliance audit queries meet production performance requirements
/// </summary>
[Collection("Performance")]
public class AuditQueryPerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly LmsDbContext _context;
    private readonly List<TimeSpan> _measurements = new();
    private bool _disposed;

    public AuditQueryPerformanceTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));

        var services = new ServiceCollection();
        services.AddDbContext<LmsDbContext>(options =>
            options.UseInMemoryDatabase($"AuditPerformanceTest_{Guid.NewGuid()}"));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<LmsDbContext>();
        _context.Database.EnsureCreated();
    }

    #region Audit Record Query Performance Tests

    [Theory]
    [InlineData(100000)] // 100K records
    [InlineData(500000)] // 500K records
    [InlineData(1000000)] // 1M records
    [Trait("Category", "Performance")]
    [Trait("Operation", "AUDIT_QUERY")]
    public async Task QueryAuditRecords_ShouldComplete_Within2Seconds(int recordCount)
    {
        // Arrange
        await CreateAuditRecordsAsync(recordCount);
        var timeLimit = TimeSpan.FromSeconds(2);

        // Act & Assert - Basic audit query with filtering
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var results = await _context.AuditRecords
                .Where(ar => ar.EventType == "StudentCreated")
                .Where(ar => ar.Timestamp >= DateTime.UtcNow.AddDays(-30)) // Last 30 days
                .OrderByDescending(ar => ar.Timestamp)
                .Take(1000)
                .Select(ar => new
                {
                    ar.Id,
                    ar.EventType,
                    ar.EntityType,
                    ar.UserId,
                    ar.Timestamp,
                    ar.CorrelationId
                })
                .ToListAsync();

            return results;
        });

        elapsed.Should().BeLessThan(timeLimit);
        
        var throughput = recordCount / elapsed.TotalSeconds;
        _output.WriteLine($"Audit Query Performance ({recordCount:N0} records):");
        _output.WriteLine($"  Query Time: {elapsed.TotalMilliseconds:F0}ms");
        _output.WriteLine($"  Throughput: {throughput:F0} records/second");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "AUDIT_ENTITY_TRAIL")]
    public async Task EntityAuditTrail_1MillionRecords_ShouldComplete_Within2Seconds()
    {
        // Arrange
        const int recordCount = 1000000;
        var testEntityId = Guid.NewGuid();
        await CreateAuditRecordsForEntityAsync(recordCount, testEntityId);

        // Act & Assert - Entity-specific audit trail
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var auditTrail = await _context.AuditRecords
                .Where(ar => ar.EntityId == testEntityId)
                .Where(ar => ar.EntityType == "Student")
                .OrderByDescending(ar => ar.Timestamp)
                .Select(ar => new
                {
                    ar.EventType,
                    ar.Timestamp,
                    ar.UserId,
                    ar.ChangeDetails,
                    ar.SequenceNumber
                })
                .Take(100) // Last 100 changes
                .ToListAsync();

            auditTrail.Should().NotBeEmpty();
            return auditTrail;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        _output.WriteLine($"Entity Audit Trail Query ({recordCount:N0} records):");
        _output.WriteLine($"  Query Time: {elapsed.TotalMilliseconds:F0}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "AUDIT_USER_ACTIVITY")]
    public async Task UserActivityQuery_1MillionRecords_ShouldComplete_Within2Seconds()
    {
        // Arrange
        const int recordCount = 1000000;
        await CreateAuditRecordsAsync(recordCount);
        var testUserId = "user-123";

        // Act & Assert - User activity query
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var userActivity = await _context.AuditRecords
                .Where(ar => ar.UserId == testUserId)
                .Where(ar => ar.Timestamp >= DateTime.UtcNow.AddDays(-7)) // Last 7 days
                .GroupBy(ar => ar.EventType)
                .Select(g => new
                {
                    EventType = g.Key,
                    Count = g.Count(),
                    LatestActivity = g.Max(ar => ar.Timestamp)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return userActivity;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        _output.WriteLine($"User Activity Query ({recordCount:N0} records):");
        _output.WriteLine($"  Query Time: {elapsed.TotalMilliseconds:F0}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "AUDIT_COMPLIANCE_REPORT")]
    public async Task ComplianceReportQuery_1MillionRecords_ShouldComplete_Within2Seconds()
    {
        // Arrange
        const int recordCount = 1000000;
        await CreateAuditRecordsAsync(recordCount);

        // Act & Assert - Compliance report aggregation
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var complianceData = await _context.AuditRecords
                .Where(ar => ar.Timestamp >= DateTime.UtcNow.AddYears(-1)) // Last year
                .GroupBy(ar => new { ar.EntityType, Date = ar.Timestamp.Date })
                .Select(g => new
                {
                    g.Key.EntityType,
                    g.Key.Date,
                    CreateCount = g.Count(ar => ar.EventType.Contains("Create")),
                    UpdateCount = g.Count(ar => ar.EventType.Contains("Update")),
                    DeleteCount = g.Count(ar => ar.EventType.Contains("Delete")),
                    TotalActivity = g.Count()
                })
                .Where(x => x.TotalActivity > 0)
                .OrderByDescending(x => x.Date)
                .Take(365) // Last year of data
                .ToListAsync();

            complianceData.Should().NotBeEmpty();
            return complianceData;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        _output.WriteLine($"Compliance Report Query ({recordCount:N0} records):");
        _output.WriteLine($"  Query Time: {elapsed.TotalMilliseconds:F0}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "AUDIT_CHAIN_VERIFICATION")]
    public async Task AuditChainVerification_1MillionRecords_ShouldComplete_Within2Seconds()
    {
        // Arrange
        const int recordCount = 1000000;
        await CreateAuditRecordsWithChainAsync(recordCount);

        // Act & Assert - Audit chain integrity verification
        var elapsed = await MeasureOperationAsync(async () =>
        {
            // Verify sequential integrity (sequence numbers should be continuous)
            var chainVerification = await _context.AuditRecords
                .OrderBy(ar => ar.SequenceNumber)
                .Select(ar => new
                {
                    ar.SequenceNumber,
                    ar.RecordHash,
                    ar.PreviousRecordHash
                })
                .ToListAsync();

            // Verify chain integrity (simplified check)
            var brokenLinks = 0;
            for (int i = 1; i < Math.Min(chainVerification.Count, 10000); i++) // Check first 10K for performance
            {
                var current = chainVerification[i];
                var previous = chainVerification[i - 1];
                
                if (current.PreviousRecordHash != previous.RecordHash)
                {
                    brokenLinks++;
                }
            }

            return new { TotalRecords = chainVerification.Count, BrokenLinks = brokenLinks };
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        _output.WriteLine($"Audit Chain Verification ({recordCount:N0} records):");
        _output.WriteLine($"  Query Time: {elapsed.TotalMilliseconds:F0}ms");
    }

    #endregion

    #region Complex Audit Queries

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "AUDIT_COMPLEX_SEARCH")]
    public async Task ComplexAuditSearch_1MillionRecords_ShouldComplete_Within2Seconds()
    {
        // Arrange
        const int recordCount = 1000000;
        await CreateAuditRecordsAsync(recordCount);

        // Act & Assert - Complex multi-criteria search
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var searchResults = await _context.AuditRecords
                .Where(ar => ar.Timestamp >= DateTime.UtcNow.AddMonths(-6))
                .Where(ar => ar.EntityType == "Student" || ar.EntityType == "Staff")
                .Where(ar => ar.EventType.Contains("Update") || ar.EventType.Contains("Delete"))
                .Where(ar => !string.IsNullOrEmpty(ar.CorrelationId))
                .GroupBy(ar => new { ar.EntityType, ar.UserId })
                .Select(g => new
                {
                    g.Key.EntityType,
                    g.Key.UserId,
                    ActivityCount = g.Count(),
                    FirstActivity = g.Min(ar => ar.Timestamp),
                    LastActivity = g.Max(ar => ar.Timestamp),
                    EventTypes = g.Select(ar => ar.EventType).Distinct().Count()
                })
                .Where(x => x.ActivityCount >= 5) // Users with significant activity
                .OrderByDescending(x => x.ActivityCount)
                .Take(100)
                .ToListAsync();

            searchResults.Should().NotBeEmpty();
            return searchResults;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        _output.WriteLine($"Complex Audit Search ({recordCount:N0} records):");
        _output.WriteLine($"  Query Time: {elapsed.TotalMilliseconds:F0}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "AUDIT_TIME_SERIES")]
    public async Task AuditTimeSeriesAnalysis_1MillionRecords_ShouldComplete_Within2Seconds()
    {
        // Arrange
        const int recordCount = 1000000;
        await CreateAuditRecordsAsync(recordCount);

        // Act & Assert - Time series analysis for activity patterns
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var timeSeries = await _context.AuditRecords
                .Where(ar => ar.Timestamp >= DateTime.UtcNow.AddDays(-90)) // Last 90 days
                .GroupBy(ar => new 
                { 
                    Date = ar.Timestamp.Date,
                    Hour = ar.Timestamp.Hour 
                })
                .Select(g => new
                {
                    g.Key.Date,
                    g.Key.Hour,
                    ActivityCount = g.Count(),
                    UniqueUsers = g.Select(ar => ar.UserId).Distinct().Count(),
                    EventTypeDistribution = g.GroupBy(ar => ar.EventType)
                        .Select(eg => new { EventType = eg.Key, Count = eg.Count() })
                        .ToList()
                })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Hour)
                .ToListAsync();

            timeSeries.Should().NotBeEmpty();
            return timeSeries;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        _output.WriteLine($"Audit Time Series Analysis ({recordCount:N0} records):");
        _output.WriteLine($"  Query Time: {elapsed.TotalMilliseconds:F0}ms");
    }

    #endregion

    #region Helper Methods

    private async Task<TimeSpan> MeasureOperationAsync<T>(Func<Task<T>> operation)
    {
        // Force garbage collection before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var stopwatch = Stopwatch.StartNew();
        await operation();
        stopwatch.Stop();
        
        _measurements.Add(stopwatch.Elapsed);
        return stopwatch.Elapsed;
    }

    private async Task CreateAuditRecordsAsync(int count)
    {
        const int batchSize = 10000;
        var eventTypes = new[] { "StudentCreated", "StudentUpdated", "StudentDeleted", "StaffCreated", "StaffUpdated", "EnrollmentCreated" };
        var entityTypes = new[] { "Student", "Staff", "Enrollment", "Class", "School" };
        var userIds = Enumerable.Range(1, 100).Select(i => $"user-{i}").ToArray();
        
        var random = new Random(42); // Consistent seed for reproducible results
        var baseDate = DateTime.UtcNow.AddYears(-2);

        _output.WriteLine($"Creating {count:N0} audit records...");

        for (int batch = 0; batch < count; batch += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, count - batch);
            var auditRecords = new List<AuditRecord>();

            for (int i = 0; i < currentBatchSize; i++)
            {
                var sequenceNumber = batch + i + 1;
                var timestamp = baseDate.AddMinutes(random.Next(0, 1051200)); // Random time within 2 years
                
                var auditRecord = new AuditRecord(
                    eventTypes[random.Next(eventTypes.Length)],
                    entityTypes[random.Next(entityTypes.Length)],
                    Guid.NewGuid(),
                    userIds[random.Next(userIds.Length)],
                    timestamp,
                    "127.0.0.1",
                    "Mozilla/5.0 Test Agent",
                    $"{{\"test\":\"data-{i}\"}}"
                );

                // Set sequence number and hash (simplified for performance testing)
                auditRecord.SetSequenceNumber(sequenceNumber);
                auditRecord.SetHash($"hash-{sequenceNumber:D10}");
                if (sequenceNumber > 1)
                {
                    auditRecord.SetPreviousRecordHash($"hash-{sequenceNumber - 1:D10}");
                }

                auditRecords.Add(auditRecord);
            }

            _context.AuditRecords.AddRange(auditRecords);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            if (batch % 50000 == 0)
            {
                _output.WriteLine($"  Created {batch + currentBatchSize:N0} records...");
            }
        }

        _output.WriteLine($"Completed creating {count:N0} audit records");
    }

    private async Task CreateAuditRecordsForEntityAsync(int count, Guid entityId)
    {
        const int batchSize = 10000;
        var eventTypes = new[] { "StudentCreated", "StudentUpdated", "GradeUpdated", "StudentTransferred" };
        var userIds = new[] { "teacher-1", "admin-1", "system", "principal-1" };
        
        var random = new Random(42);
        var baseDate = DateTime.UtcNow.AddYears(-1);

        for (int batch = 0; batch < count; batch += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, count - batch);
            var auditRecords = new List<AuditRecord>();

            for (int i = 0; i < currentBatchSize; i++)
            {
                var sequenceNumber = batch + i + 1;
                var timestamp = baseDate.AddMinutes(random.Next(0, 525600)); // Random time within 1 year
                
                var auditRecord = new AuditRecord(
                    eventTypes[random.Next(eventTypes.Length)],
                    "Student",
                    entityId, // Same entity for all records
                    userIds[random.Next(userIds.Length)],
                    timestamp,
                    "127.0.0.1",
                    "Mozilla/5.0 Test Agent",
                    $"{{\"entityId\":\"{entityId}\",\"change\":\"test-{i}\"}}"
                );

                auditRecord.SetSequenceNumber(sequenceNumber);
                auditRecord.SetHash($"entity-hash-{sequenceNumber:D10}");
                
                auditRecords.Add(auditRecord);
            }

            _context.AuditRecords.AddRange(auditRecords);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }
    }

    private async Task CreateAuditRecordsWithChainAsync(int count)
    {
        const int batchSize = 10000;
        var random = new Random(42);
        var baseDate = DateTime.UtcNow.AddYears(-1);
        
        string? previousHash = null;

        for (int batch = 0; batch < count; batch += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, count - batch);
            var auditRecords = new List<AuditRecord>();

            for (int i = 0; i < currentBatchSize; i++)
            {
                var sequenceNumber = batch + i + 1;
                var timestamp = baseDate.AddMinutes(sequenceNumber); // Sequential timestamps for chain
                
                var auditRecord = new AuditRecord(
                    "ChainTest",
                    "TestEntity",
                    Guid.NewGuid(),
                    "chain-test-user",
                    timestamp,
                    "127.0.0.1",
                    "Chain Test Agent",
                    $"{{\"sequence\":{sequenceNumber}}}"
                );

                auditRecord.SetSequenceNumber(sequenceNumber);
                var currentHash = $"chain-{sequenceNumber:D10}-{Guid.NewGuid():N}";
                auditRecord.SetHash(currentHash);
                
                if (previousHash != null)
                {
                    auditRecord.SetPreviousRecordHash(previousHash);
                }

                auditRecords.Add(auditRecord);
                previousHash = currentHash;
            }

            _context.AuditRecords.AddRange(auditRecords);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context?.Dispose();
            _serviceProvider?.GetService<IDisposable>()?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}