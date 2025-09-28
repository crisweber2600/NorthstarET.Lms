using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthstarET.Lms.Application.Commands.Students;
using NorthstarET.Lms.Application.Services;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace NorthstarET.Lms.Performance.Tests;

/// <summary>
/// Performance tests for bulk operations to ensure completion within 120 seconds for 10k records
/// These tests validate that large-scale data operations meet production requirements
/// </summary>
[Collection("Performance")]
public class BulkOperationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly LmsDbContext _context;
    private readonly List<TimeSpan> _measurements = new();
    private bool _disposed;

    public BulkOperationTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));

        var services = new ServiceCollection();
        services.AddDbContext<LmsDbContext>(options =>
            options.UseInMemoryDatabase($"BulkPerformanceTest_{Guid.NewGuid()}"));
        
        services.AddScoped<StudentService>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ITenantContextAccessor, MockTenantContextAccessor>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<LmsDbContext>();
        _context.Database.EnsureCreated();
    }

    #region Bulk Insert Performance Tests

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10000)]
    [Trait("Category", "Performance")]
    [Trait("Operation", "BULK_INSERT")]
    public async Task BulkInsertStudents_ShouldComplete_Within120Seconds(int recordCount)
    {
        // Arrange
        var students = GenerateTestStudents(recordCount);
        var timeLimit = TimeSpan.FromSeconds(120);

        // Act & Assert
        var elapsed = await MeasureOperationAsync(async () =>
        {
            // Use batch insert for better performance
            const int batchSize = 1000;
            var batches = students.Chunk(batchSize);

            foreach (var batch in batches)
            {
                _context.Students.AddRange(batch);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear(); // Clear change tracking for memory efficiency
            }

            return students.Count;
        });

        elapsed.Should().BeLessThan(timeLimit);
        
        var throughput = recordCount / elapsed.TotalSeconds;
        _output.WriteLine($"Bulk Insert {recordCount:N0} Students:");
        _output.WriteLine($"  Total Time: {elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"  Throughput: {throughput:F0} records/second");
        _output.WriteLine($"  Time per 1K records: {elapsed.TotalMilliseconds / (recordCount / 1000.0):F0}ms");

        // Verify data was inserted
        var actualCount = await _context.Students.CountAsync();
        actualCount.Should().Be(recordCount);
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "BULK_INSERT_OPTIMIZED")]
    public async Task OptimizedBulkInsert_10000Records_ShouldComplete_Within60Seconds()
    {
        // Arrange
        const int recordCount = 10000;
        var students = GenerateTestStudents(recordCount);

        // Act - Use optimized bulk insert strategy
        var elapsed = await MeasureOperationAsync(async () =>
        {
            // Disable change tracking for bulk operations
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            try
            {
                const int batchSize = 2000; // Larger batches for better performance
                var batches = students.Chunk(batchSize);

                foreach (var batch in batches)
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.Students.AddRange(batch);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        // Clear tracking to prevent memory issues
                        _context.ChangeTracker.Clear();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                return recordCount;
            }
            finally
            {
                // Re-enable change tracking
                _context.ChangeTracker.AutoDetectChangesEnabled = true;
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
            }
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60));
        
        var throughput = recordCount / elapsed.TotalSeconds;
        _output.WriteLine($"Optimized Bulk Insert {recordCount:N0} Students:");
        _output.WriteLine($"  Total Time: {elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"  Throughput: {throughput:F0} records/second");

        // Verify all records were inserted
        var actualCount = await _context.Students.CountAsync();
        actualCount.Should().Be(recordCount);
    }

    #endregion

    #region Bulk Update Performance Tests

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [Trait("Category", "Performance")]
    [Trait("Operation", "BULK_UPDATE")]
    public async Task BulkUpdateStudents_ShouldComplete_Within120Seconds(int recordCount)
    {
        // Arrange - Create test data first
        var students = GenerateTestStudents(recordCount);
        _context.Students.AddRange(students);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert - Update all students
        var elapsed = await MeasureOperationAsync(async () =>
        {
            const int batchSize = 1000;
            var processedCount = 0;

            while (processedCount < recordCount)
            {
                var batch = await _context.Students
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();

                if (!batch.Any()) break;

                // Update each student in the batch
                foreach (var student in batch)
                {
                    student.UpdateGradeLevel((GradeLevel)((int)student.GradeLevel + 1 % 12), "performance-test");
                }

                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();

                processedCount += batch.Count;
            }

            return processedCount;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(120));
        
        var throughput = recordCount / elapsed.TotalSeconds;
        _output.WriteLine($"Bulk Update {recordCount:N0} Students:");
        _output.WriteLine($"  Total Time: {elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"  Throughput: {throughput:F0} records/second");
    }

    #endregion

    #region Bulk Delete Performance Tests

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [Trait("Category", "Performance")]
    [Trait("Operation", "BULK_DELETE")]
    public async Task BulkDeleteStudents_ShouldComplete_Within120Seconds(int recordCount)
    {
        // Arrange - Create test data
        var students = GenerateTestStudents(recordCount);
        _context.Students.AddRange(students);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert - Bulk delete
        var elapsed = await MeasureOperationAsync(async () =>
        {
            const int batchSize = 1000;
            var deletedCount = 0;

            while (deletedCount < recordCount)
            {
                var batch = await _context.Students
                    .Take(batchSize)
                    .ToListAsync();

                if (!batch.Any()) break;

                _context.Students.RemoveRange(batch);
                await _context.SaveChangesAsync();
                
                deletedCount += batch.Count;
            }

            return deletedCount;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(120));
        
        var throughput = recordCount / elapsed.TotalSeconds;
        _output.WriteLine($"Bulk Delete {recordCount:N0} Students:");
        _output.WriteLine($"  Total Time: {elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"  Throughput: {throughput:F0} records/second");

        // Verify deletion
        var remainingCount = await _context.Students.CountAsync();
        remainingCount.Should().Be(0);
    }

    #endregion

    #region Bulk Rollover Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "BULK_ROLLOVER")]
    public async Task BulkRolloverOperation_10000Students_ShouldComplete_Within120Seconds()
    {
        // Arrange - Create school year and students
        const int studentCount = 10000;

        var oldSchoolYear = new SchoolYear(2023, "2023-2024", 
            new DateTime(2023, 8, 15), new DateTime(2024, 6, 15));
        var newSchoolYear = new SchoolYear(2024, "2024-2025", 
            new DateTime(2024, 8, 15), new DateTime(2025, 6, 15));

        _context.SchoolYears.AddRange(oldSchoolYear, newSchoolYear);
        await _context.SaveChangesAsync();

        var students = GenerateTestStudents(studentCount);
        _context.Students.AddRange(students);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert - Perform bulk rollover (grade promotion + year transition)
        var elapsed = await MeasureOperationAsync(async () =>
        {
            const int batchSize = 2000;
            var processedCount = 0;

            while (processedCount < studentCount)
            {
                var batch = await _context.Students
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();

                if (!batch.Any()) break;

                // Simulate rollover operations
                foreach (var student in batch)
                {
                    // Promote grade level
                    var newGrade = (GradeLevel)Math.Min((int)student.GradeLevel + 1, 12);
                    student.UpdateGradeLevel(newGrade, "bulk-rollover-system");
                    
                    // Record would typically involve enrollment updates, but we're focusing on student updates
                }

                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
                processedCount += batch.Count;
            }

            return processedCount;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(120));
        
        var throughput = studentCount / elapsed.TotalSeconds;
        _output.WriteLine($"Bulk Rollover {studentCount:N0} Students:");
        _output.WriteLine($"  Total Time: {elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"  Throughput: {throughput:F0} students/second");
        
        // Verify rollover completed
        var updatedStudents = await _context.Students.CountAsync();
        updatedStudents.Should().Be(studentCount);
    }

    #endregion

    #region Data Import/Export Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "BULK_IMPORT")]
    public async Task BulkImportFromCSV_10000Records_ShouldComplete_Within120Seconds()
    {
        // Arrange - Simulate CSV data processing
        const int recordCount = 10000;
        var csvData = GenerateCSVData(recordCount);

        // Act & Assert - Process CSV import
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var students = new List<Student>();
            const int batchSize = 2000;

            // Simulate CSV parsing and validation
            for (int i = 0; i < csvData.Count; i++)
            {
                var row = csvData[i];
                var student = new Student(
                    row.StudentNumber,
                    row.FirstName,
                    row.LastName,
                    row.DateOfBirth,
                    row.GradeLevel);

                students.Add(student);

                // Process in batches
                if (students.Count >= batchSize || i == csvData.Count - 1)
                {
                    _context.Students.AddRange(students);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    students.Clear();
                }
            }

            return recordCount;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(120));
        
        var throughput = recordCount / elapsed.TotalSeconds;
        _output.WriteLine($"Bulk CSV Import {recordCount:N0} Students:");
        _output.WriteLine($"  Total Time: {elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"  Throughput: {throughput:F0} records/second");

        // Verify import
        var importedCount = await _context.Students.CountAsync();
        importedCount.Should().Be(recordCount);
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

    private List<Student> GenerateTestStudents(int count)
    {
        var students = new List<Student>(count);
        var random = new Random(42); // Use seed for consistent results

        for (int i = 0; i < count; i++)
        {
            var student = new Student(
                $"BULK-{i:D6}",
                $"FirstName{i:D6}",
                $"LastName{i % 100:D2}", // Some name variety
                new DateTime(2005 + (i % 15), 1, 1), // Age variety
                (GradeLevel)(i % 12)); // Grade variety

            students.Add(student);
        }

        return students;
    }

    private List<CSVStudentRow> GenerateCSVData(int count)
    {
        var csvData = new List<CSVStudentRow>(count);
        var random = new Random(42);

        for (int i = 0; i < count; i++)
        {
            csvData.Add(new CSVStudentRow
            {
                StudentNumber = $"CSV-{i:D6}",
                FirstName = $"Import{i:D6}",
                LastName = $"Student{i % 50:D2}",
                DateOfBirth = new DateTime(2005 + (i % 15), 1, 1),
                GradeLevel = (GradeLevel)(i % 12)
            });
        }

        return csvData;
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

    private record CSVStudentRow
    {
        public string StudentNumber { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public DateTime DateOfBirth { get; init; }
        public GradeLevel GradeLevel { get; init; }
    }
}