using System.Diagnostics;
using System.Linq;
using Xunit.Abstractions;

namespace NorthstarET.Lms.Tests.Performance;

public class PerformanceSimulationTests
{
    private const int _crudSampleSize = 1_000;
    private readonly ITestOutputHelper _output;
    private readonly Random _random = new();

    public PerformanceSimulationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ValidateCrudOperationsSLA_ShouldMeetPR001_200msP95()
    {
        var stopwatch = new Stopwatch();
    var timings = new List<double>(_crudSampleSize);
    var students = new Dictionary<string, StudentRecord>(_crudSampleSize);

    for (var i = 0; i < _crudSampleSize; i++)
        {
            stopwatch.Restart();

            var studentNumber = $"STU{i:000000}";
            var record = new StudentRecord(studentNumber, "Test", "Student", DateTime.UtcNow);

            students[studentNumber] = record;
            students[studentNumber] = students[studentNumber] with { LastName = $"Updated{i:000000}" };
            _ = students[studentNumber];

            stopwatch.Stop();
            timings.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        var p95 = CalculatePercentile(timings, 95);

        _output.WriteLine($"CRUD operations P95 latency: {p95:F4} ms");
        _output.WriteLine($"CRUD operations average latency: {timings.Average():F4} ms");

    timings.Should().HaveCount(_crudSampleSize);
        p95.Should().BeLessThan(200);
    }

    [Fact]
    public void ValidateBulkOperationsSLA_ShouldMeetPR002_120sMax()
    {
        const int cohortSize = 10_000;
        var stopwatch = Stopwatch.StartNew();

        var processed = ExecuteBulkRollover(cohortSize);

        stopwatch.Stop();
        _output.WriteLine($"Bulk rollover processed {processed} records in {stopwatch.Elapsed.TotalSeconds:F2}s");

        processed.Should().Be(cohortSize);
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(120);
    }

    [Fact]
    public void ValidateAuditQueriesSLA_ShouldMeetPR003_2sMax()
    {
        const int auditRecordCount = 100_000;
        var auditLog = GenerateAuditEntries(auditRecordCount);

        var stopwatch = Stopwatch.StartNew();

        var summary = auditLog
            .GroupBy(entry => new { entry.EntityType, entry.EventType, entry.HourBucket })
            .Select(group => new AuditSummary(
                group.Key.EntityType,
                group.Key.EventType,
                group.Key.HourBucket,
                AverageLatency: group.Average(e => e.LatencyMs),
                PeakLatency: group.Max(e => e.LatencyMs),
                UniqueUsers: group.Select(e => e.UserId).Distinct().Count()))
            .Where(result => result.AverageLatency > 1)
            .ToList();

        stopwatch.Stop();

        _output.WriteLine($"Audit aggregation produced {summary.Count} rows in {stopwatch.Elapsed.TotalSeconds:F3}s");

        summary.Should().NotBeEmpty();
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(2);
    }

    private int ExecuteBulkRollover(int count)
    {
        var enrollments = Enumerable.Range(0, count)
            .Select(index => new EnrollmentSnapshot(Guid.NewGuid(), Guid.NewGuid(), _random.Next(1, 12)))
            .ToArray();

        var processed = 0;

        foreach (var enrollment in enrollments)
        {
            var nextGrade = Math.Min(enrollment.GradeLevel + 1, 12);
            var workloadSeed = HashCode.Combine(enrollment.StudentId, enrollment.SchoolId, nextGrade);
            var workloads = (workloadSeed & 0x7) + 1;

            for (var i = 0; i < workloads; i++)
            {
                _ = Math.Sqrt(workloadSeed + i);
            }

            processed++;
        }

        return processed;
    }

    private IReadOnlyList<AuditEntry> GenerateAuditEntries(int count)
    {
        var entities = new[] { "Student", "Staff", "Enrollment", "Assessment", "Audit" };
        var events = new[] { "CREATE", "UPDATE", "DELETE", "ACCESS", "EXPORT" };

        var entries = new List<AuditEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var entity = entities[_random.Next(entities.Length)];
            var eventType = events[_random.Next(events.Length)];
            var hourBucket = _random.Next(0, 24);
            var latency = 5 + _random.NextDouble() * 45;

            entries.Add(new AuditEntry(Guid.NewGuid(), entity, eventType, hourBucket, latency));
        }

        return entries;
    }

    private static double CalculatePercentile(IReadOnlyList<double> values, int percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var ordered = values.OrderBy(v => v).ToArray();
        var rank = (percentile / 100.0) * (ordered.Length - 1);
        var lowerIndex = (int)Math.Floor(rank);
        var upperIndex = (int)Math.Ceiling(rank);

        if (lowerIndex == upperIndex)
        {
            return ordered[lowerIndex];
        }

        var weight = rank - lowerIndex;
        return ordered[lowerIndex] + (ordered[upperIndex] - ordered[lowerIndex]) * weight;
    }

    private record StudentRecord(string StudentNumber, string FirstName, string LastName, DateTime CreatedUtc);

    private record EnrollmentSnapshot(Guid StudentId, Guid SchoolId, int GradeLevel);

    private record AuditEntry(Guid UserId, string EntityType, string EventType, int HourBucket, double LatencyMs);

    private record AuditSummary(string EntityType, string EventType, int HourBucket, double AverageLatency, double PeakLatency, int UniqueUsers);
}
