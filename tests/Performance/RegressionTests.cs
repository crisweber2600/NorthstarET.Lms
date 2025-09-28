using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace NorthstarET.Lms.Api.Tests.Performance;

/// <summary>
/// Performance regression tests to ensure SLA compliance and detect performance degradation.
/// These tests verify that the system meets constitutional performance requirements.
/// </summary>
public class RegressionTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    // Constitutional performance requirements
    private const int CrudOperationSlaMs = 200;      // p95 < 200ms
    private const int BulkOperationSlaSeconds = 120; // < 120s for 10k records
    private const int AuditQuerySlaMs = 2000;        // < 2s for 1M records

    public RegressionTests(TestWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task StudentCrud_Operations_ShouldMeetP95SLA()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var measurements = new List<long>();
        const int iterations = 100; // Measure 100 operations for P95 calculation

        // Warm up
        await WarmUpEndpoint("/api/v1/students");

        // Act & Measure - CREATE operations
        _output.WriteLine("Testing CREATE operations performance...");
        for (int i = 0; i < iterations; i++)
        {
            var createRequest = new
            {
                StudentNumber = $"PERF-CREATE-{i:D4}",
                FirstName = $"PerfTest{i}",
                LastName = "Student",
                DateOfBirth = DateTime.Now.AddYears(-10).AddDays(i),
                EnrollmentDate = DateTime.Now,
                Programs = new { IsSpecialEducation = false, IsGifted = false, IsEnglishLanguageLearner = false }
            };

            var stopwatch = Stopwatch.StartNew();
            var response = await _client.PostAsJsonAsync("/api/v1/students", createRequest);
            stopwatch.Stop();

            if (response.StatusCode == HttpStatusCode.Created)
            {
                measurements.Add(stopwatch.ElapsedMilliseconds);
            }

            if (i % 20 == 0)
            {
                _output.WriteLine($"Completed {i + 1}/{iterations} CREATE operations");
            }
        }

        // Assert P95 performance
        var p95Create = CalculatePercentile(measurements, 95);
        _output.WriteLine($"CREATE P95: {p95Create}ms (SLA: {CrudOperationSlaMs}ms)");
        p95Create.Should().BeLessOrEqualTo(CrudOperationSlaMs, 
            $"CREATE operations P95 should be under {CrudOperationSlaMs}ms");

        // Test READ operations
        measurements.Clear();
        _output.WriteLine("Testing READ operations performance...");

        // Get some student IDs first
        var listResponse = await _client.GetAsync("/api/v1/students?page=1&size=50");
        if (listResponse.StatusCode == HttpStatusCode.OK)
        {
            var content = await listResponse.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (result.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                var studentIds = new List<string>();
                foreach (var student in dataArray.EnumerateArray().Take(50))
                {
                    if (student.TryGetProperty("userId", out var userIdElement))
                    {
                        studentIds.Add(userIdElement.GetString() ?? "");
                    }
                }

                // Measure READ operations
                for (int i = 0; i < Math.Min(iterations, studentIds.Count); i++)
                {
                    var studentId = studentIds[i % studentIds.Count];
                    
                    var stopwatch = Stopwatch.StartNew();
                    var response = await _client.GetAsync($"/api/v1/students/{studentId}");
                    stopwatch.Stop();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        measurements.Add(stopwatch.ElapsedMilliseconds);
                    }

                    if (i % 20 == 0)
                    {
                        _output.WriteLine($"Completed {i + 1}/{Math.Min(iterations, studentIds.Count)} READ operations");
                    }
                }

                var p95Read = CalculatePercentile(measurements, 95);
                _output.WriteLine($"READ P95: {p95Read}ms (SLA: {CrudOperationSlaMs}ms)");
                p95Read.Should().BeLessOrEqualTo(CrudOperationSlaMs,
                    $"READ operations P95 should be under {CrudOperationSlaMs}ms");
            }
        }
    }

    [Fact]
    public async Task StudentList_WithPagination_ShouldMeetPerformanceSLA()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var measurements = new List<long>();
        await WarmUpEndpoint("/api/v1/students");

        // Act - Test different page sizes and filters
        var testScenarios = new[]
        {
            ("?page=1&size=10", "Small page"),
            ("?page=1&size=50", "Medium page"),
            ("?page=1&size=100", "Large page"),
            ("?page=1&size=50&status=Active", "Filtered query"),
            ("?page=1&size=50&gradeLevel=Grade6", "Grade filtered"),
            ("?page=1&size=50&hasProgram=gifted", "Program filtered")
        };

        foreach (var (queryString, description) in testScenarios)
        {
            _output.WriteLine($"Testing {description}: {queryString}");
            
            // Measure multiple iterations
            var scenarioMeasurements = new List<long>();
            for (int i = 0; i < 20; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _client.GetAsync($"/api/v1/students{queryString}");
                stopwatch.Stop();

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
                {
                    scenarioMeasurements.Add(stopwatch.ElapsedMilliseconds);
                    measurements.Add(stopwatch.ElapsedMilliseconds);
                }
            }

            var scenarioP95 = CalculatePercentile(scenarioMeasurements, 95);
            _output.WriteLine($"{description} P95: {scenarioP95}ms");
        }

        // Assert overall performance
        var overallP95 = CalculatePercentile(measurements, 95);
        _output.WriteLine($"Overall LIST P95: {overallP95}ms (SLA: {CrudOperationSlaMs}ms)");
        overallP95.Should().BeLessOrEqualTo(CrudOperationSlaMs,
            $"LIST operations P95 should be under {CrudOperationSlaMs}ms");
    }

    [Fact]
    public async Task AuditQuery_Operations_ShouldMeetPerformanceSLA()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var measurements = new List<long>();
        await WarmUpEndpoint("/api/v1/audit");

        // Act - Test various audit query scenarios
        var auditQueryScenarios = new[]
        {
            ("?page=1&size=50", "Basic audit query"),
            ("?entityType=Student&page=1&size=50", "Student audits"),
            ("?eventType=StudentCreated&page=1&size=50", "Event type filter"),
            ("?startDate=2024-01-01&endDate=2024-12-31&page=1&size=50", "Date range query"),
            ("?userId=test-user&page=1&size=50", "User activity query")
        };

        foreach (var (queryString, description) in auditQueryScenarios)
        {
            _output.WriteLine($"Testing {description}: {queryString}");
            
            var scenarioMeasurements = new List<long>();
            for (int i = 0; i < 10; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _client.GetAsync($"/api/v1/audit{queryString}");
                stopwatch.Stop();

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
                {
                    scenarioMeasurements.Add(stopwatch.ElapsedMilliseconds);
                    measurements.Add(stopwatch.ElapsedMilliseconds);
                }
            }

            var scenarioP95 = CalculatePercentile(scenarioMeasurements, 95);
            _output.WriteLine($"{description} P95: {scenarioP95}ms");
        }

        // Assert audit query performance (more lenient SLA for complex queries)
        var auditP95 = CalculatePercentile(measurements, 95);
        _output.WriteLine($"Audit Query P95: {auditP95}ms (SLA: {AuditQuerySlaMs}ms)");
        auditP95.Should().BeLessOrEqualTo(AuditQuerySlaMs,
            $"Audit queries P95 should be under {AuditQuerySlaMs}ms");
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldMaintainPerformance()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        await WarmUpEndpoint("/api/v1/students");

        const int concurrentUsers = 10;
        const int operationsPerUser = 10;
        var allMeasurements = new List<long>();

        _output.WriteLine($"Testing concurrent operations: {concurrentUsers} users, {operationsPerUser} ops each");

        // Act - Simulate concurrent users
        var tasks = new List<Task>();

        for (int user = 0; user < concurrentUsers; user++)
        {
            var userId = user;
            var task = Task.Run(async () =>
            {
                var client = _factory.CreateClient();
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

                var userMeasurements = new List<long>();

                for (int op = 0; op < operationsPerUser; op++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var response = await client.GetAsync($"/api/v1/students?page={op % 5 + 1}&size=20");
                    stopwatch.Stop();

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
                    {
                        userMeasurements.Add(stopwatch.ElapsedMilliseconds);
                    }
                }

                lock (allMeasurements)
                {
                    allMeasurements.AddRange(userMeasurements);
                }

                var userP95 = CalculatePercentile(userMeasurements, 95);
                _output.WriteLine($"User {userId} P95: {userP95}ms");
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert concurrent performance doesn't degrade significantly
        var concurrentP95 = CalculatePercentile(allMeasurements, 95);
        _output.WriteLine($"Concurrent Operations P95: {concurrentP95}ms (SLA: {CrudOperationSlaMs * 2}ms - 2x allowance for concurrency)");
        
        // Allow 2x SLA for concurrent operations
        concurrentP95.Should().BeLessOrEqualTo(CrudOperationSlaMs * 2,
            "Concurrent operations should not degrade performance beyond 2x normal SLA");
    }

    [Fact]
    public async Task BulkOperations_ShouldMeetThroughputSLA()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Test bulk rollover preview (simulated bulk operation)
        var bulkRequest = new
        {
            FromSchoolYear = "2023-2024",
            ToSchoolYear = "2024-2025",
            GradeTransitions = new[]
            {
                new { From = "Grade5", To = "Grade6" },
                new { From = "Grade6", To = "Grade7" },
                new { From = "Grade7", To = "Grade8" }
            },
            ExcludeWithdrawn = true
        };

        _output.WriteLine("Testing bulk operation performance...");

        var measurements = new List<long>();

        // Act - Test bulk operation multiple times
        for (int i = 0; i < 5; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.PostAsJsonAsync("/api/v1/students/bulk-rollover/preview", bulkRequest);
            stopwatch.Stop();

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
            {
                measurements.Add(stopwatch.ElapsedMilliseconds);
                _output.WriteLine($"Bulk operation {i + 1}: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        // Assert bulk operation performance
        if (measurements.Any())
        {
            var avgBulkTime = measurements.Average();
            _output.WriteLine($"Bulk Operation Average: {avgBulkTime}ms (SLA: {BulkOperationSlaSeconds * 1000}ms for 10k records)");
            
            // For preview operations, should be much faster than full execution
            avgBulkTime.Should().BeLessOrEqualTo(BulkOperationSlaSeconds * 100, // 100x faster for preview
                "Bulk operation preview should be significantly faster than full execution");
        }
    }

    [Fact]
    public async Task MemoryUsage_ShouldNotGrowExcessively_DuringOperations()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Force garbage collection before starting
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);
        _output.WriteLine($"Initial memory usage: {initialMemory / 1024 / 1024} MB");

        // Act - Perform many operations that could cause memory leaks
        for (int batch = 0; batch < 10; batch++)
        {
            var tasks = new List<Task>();
            
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(_client.GetAsync($"/api/v1/students?page={i % 5 + 1}&size=50"));
            }

            await Task.WhenAll(tasks);

            if (batch % 2 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                var memoryGrowth = (currentMemory - initialMemory) / 1024.0 / 1024.0;
                _output.WriteLine($"Memory after batch {batch}: {currentMemory / 1024 / 1024} MB (growth: {memoryGrowth:F2} MB)");
            }
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var totalGrowth = (finalMemory - initialMemory) / 1024.0 / 1024.0;
        
        _output.WriteLine($"Final memory usage: {finalMemory / 1024 / 1024} MB");
        _output.WriteLine($"Total memory growth: {totalGrowth:F2} MB");

        // Assert memory growth is reasonable (allow up to 100MB growth)
        totalGrowth.Should().BeLessOrEqualTo(100, 
            "Memory usage should not grow excessively during normal operations");
    }

    [Fact]
    public async Task DatabaseConnection_Pooling_ShouldBeEfficient()
    {
        // Arrange - Test connection pooling efficiency
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        const int simultaneousConnections = 50;
        var measurements = new List<long>();

        // Act - Create many simultaneous requests to test connection pooling
        var tasks = new List<Task<long>>();

        for (int i = 0; i < simultaneousConnections; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var client = _factory.CreateClient();
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

                var stopwatch = Stopwatch.StartNew();
                var response = await client.GetAsync("/api/v1/students?page=1&size=10");
                stopwatch.Stop();

                return response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent
                    ? stopwatch.ElapsedMilliseconds 
                    : -1;
            }));
        }

        var results = await Task.WhenAll(tasks);
        measurements.AddRange(results.Where(r => r > 0));

        // Assert connection pooling efficiency
        var avgConnectionTime = measurements.Average();
        var maxConnectionTime = measurements.Max();
        
        _output.WriteLine($"Average connection time: {avgConnectionTime}ms");
        _output.WriteLine($"Max connection time: {maxConnectionTime}ms");
        _output.WriteLine($"Successful connections: {measurements.Count}/{simultaneousConnections}");

        // Connection pooling should keep response times reasonable even under load
        avgConnectionTime.Should().BeLessOrEqualTo(CrudOperationSlaMs * 3, 
            "Average connection time should be reasonable even with many simultaneous connections");
        
        maxConnectionTime.Should().BeLessOrEqualTo(CrudOperationSlaMs * 5,
            "Maximum connection time should not exceed 5x normal SLA");
    }

    private async Task WarmUpEndpoint(string endpoint)
    {
        // Warm up the endpoint to ensure fair performance testing
        for (int i = 0; i < 3; i++)
        {
            await _client.GetAsync(endpoint);
        }
    }

    private double CalculatePercentile(List<long> measurements, int percentile)
    {
        if (!measurements.Any()) return 0;
        
        measurements.Sort();
        var index = (percentile / 100.0) * (measurements.Count - 1);
        
        if (index == (int)index)
        {
            return measurements[(int)index];
        }
        
        var lower = measurements[(int)Math.Floor(index)];
        var upper = measurements[(int)Math.Ceiling(index)];
        var fraction = index - Math.Floor(index);
        
        return lower + fraction * (upper - lower);
    }
}