using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthstarET.Lms.Application.Commands.Students;
using NorthstarET.Lms.Application.Commands.Staff;
using NorthstarET.Lms.Application.Services;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace NorthstarET.Lms.Performance.Tests;

/// <summary>
/// Performance tests for CRUD operations to ensure sub-200ms p95 latency
/// These tests validate that core operations meet production performance requirements
/// </summary>
[Collection("Performance")]
public class CrudPerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly LmsDbContext _context;
    private readonly List<long> _measurements = new();
    private bool _disposed;

    public CrudPerformanceTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));

        // Setup in-memory database for performance testing
        var services = new ServiceCollection();
        services.AddDbContext<LmsDbContext>(options =>
            options.UseInMemoryDatabase($"PerformanceTest_{Guid.NewGuid()}"));
        
        // Add application services
        services.AddScoped<StudentService>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<ITenantContextAccessor, MockTenantContextAccessor>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<LmsDbContext>();
        
        // Ensure database is created
        _context.Database.EnsureCreated();
        
        // Warm up the context
        WarmUpDatabase();
    }

    #region Student CRUD Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "CREATE")]
    public async Task CreateStudent_ShouldComplete_Within200Milliseconds()
    {
        // Arrange
        var studentService = _serviceProvider.GetRequiredService<StudentService>();
        var command = new CreateStudentCommand
        {
            StudentNumber = $"STU-{DateTime.Now.Ticks}",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 15),
            GradeLevel = GradeLevel.FirstGrade,
            EnrollmentDate = DateTime.UtcNow
        };

        // Act & Assert - Single operation
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var result = await studentService.CreateStudentAsync(command);
            result.IsSuccess.Should().BeTrue();
            return result.Value;
        });

        // Single operation should be very fast
        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(50));
        _output.WriteLine($"Create Student: {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "READ")]
    public async Task GetStudent_ShouldComplete_Within200Milliseconds()
    {
        // Arrange
        var student = await CreateTestStudentAsync();
        var studentService = _serviceProvider.GetRequiredService<StudentService>();

        // Act & Assert
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var query = new GetStudentQuery { UserId = student.UserId };
            var result = await studentService.GetStudentAsync(query);
            result.IsSuccess.Should().BeTrue();
            return result.Value;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
        _output.WriteLine($"Get Student: {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "UPDATE")]
    public async Task UpdateStudent_ShouldComplete_Within200Milliseconds()
    {
        // Arrange
        var student = await CreateTestStudentAsync();
        var studentService = _serviceProvider.GetRequiredService<StudentService>();
        var command = new UpdateStudentCommand
        {
            UserId = student.UserId,
            FirstName = "Jane",
            LastName = "Smith",
            GradeLevel = GradeLevel.SecondGrade
        };

        // Act & Assert
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var result = await studentService.UpdateStudentAsync(command);
            result.IsSuccess.Should().BeTrue();
            return result.Value;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
        _output.WriteLine($"Update Student: {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "DELETE")]
    public async Task DeleteStudent_ShouldComplete_Within200Milliseconds()
    {
        // Arrange
        var student = await CreateTestStudentAsync();
        
        // Act & Assert
        var elapsed = await MeasureOperationAsync(async () =>
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
        _output.WriteLine($"Delete Student: {elapsed.TotalMilliseconds:F2}ms");
    }

    #endregion

    #region Batch CRUD Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "BATCH_READ")]
    public async Task ListStudents_With1000Records_ShouldComplete_Within200Milliseconds()
    {
        // Arrange - Create 1000 test students
        await CreateTestStudentsAsync(1000);
        var studentService = _serviceProvider.GetRequiredService<StudentService>();

        // Act & Assert
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var query = new ListStudentsQuery { Page = 1, Size = 50 }; // Paginated
            var result = await studentService.ListStudentsAsync(query);
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(50);
            return result.Value;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        _output.WriteLine($"List Students (1000 records, page 50): {elapsed.TotalMilliseconds:F2}ms");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [Trait("Category", "Performance")]
    [Trait("Operation", "BATCH_CREATE")]
    public async Task BulkCreateStudents_ShouldMaintain_PerformanceStandards(int batchSize)
    {
        // Arrange
        var commands = Enumerable.Range(1, batchSize)
            .Select(i => new CreateStudentCommand
            {
                StudentNumber = $"BULK-{DateTime.Now.Ticks}-{i}",
                FirstName = $"Student{i}",
                LastName = "Test",
                DateOfBirth = new DateTime(2010, 1, 15),
                GradeLevel = GradeLevel.FirstGrade,
                EnrollmentDate = DateTime.UtcNow
            })
            .ToList();

        var studentService = _serviceProvider.GetRequiredService<StudentService>();

        // Act & Assert
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var results = new List<Student>();
            foreach (var command in commands)
            {
                var result = await studentService.CreateStudentAsync(command);
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                }
            }
            return results;
        });

        // Performance target: <5ms per record for creation
        var perRecordTime = elapsed.TotalMilliseconds / batchSize;
        perRecordTime.Should().BeLessThan(5.0);
        
        _output.WriteLine($"Bulk Create {batchSize} Students: {elapsed.TotalMilliseconds:F2}ms total, {perRecordTime:F2}ms per record");
    }

    #endregion

    #region Complex Query Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "COMPLEX_QUERY")]
    public async Task ComplexStudentQuery_WithJoins_ShouldComplete_Within200Milliseconds()
    {
        // Arrange
        await CreateComplexTestDataAsync();

        // Act & Assert - Complex query with joins
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var results = await _context.Students
                .Include(s => s.Enrollments)
                .ThenInclude(e => e.Class)
                .Where(s => s.Status == StudentStatus.Active)
                .Where(s => s.GradeLevel >= GradeLevel.FirstGrade)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Take(100)
                .ToListAsync();

            results.Should().NotBeEmpty();
            return results;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        _output.WriteLine($"Complex Student Query with Joins: {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "AGGREGATION")]
    public async Task StudentCountByGrade_Aggregation_ShouldComplete_Within200Milliseconds()
    {
        // Arrange
        await CreateTestStudentsAsync(500);

        // Act & Assert - Aggregation query
        var elapsed = await MeasureOperationAsync(async () =>
        {
            var gradeCounts = await _context.Students
                .Where(s => s.Status == StudentStatus.Active)
                .GroupBy(s => s.GradeLevel)
                .Select(g => new { Grade = g.Key, Count = g.Count() })
                .OrderBy(x => x.Grade)
                .ToListAsync();

            gradeCounts.Should().NotBeEmpty();
            return gradeCounts;
        });

        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        _output.WriteLine($"Grade Aggregation Query (500 records): {elapsed.TotalMilliseconds:F2}ms");
    }

    #endregion

    #region P95 Performance Measurement

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Operation", "P95_MEASUREMENT")]
    public async Task CrudOperations_P95_ShouldBe_Below200Milliseconds()
    {
        // Arrange
        var measurements = new List<double>();
        const int iterations = 100;

        // Warm up
        for (int i = 0; i < 10; i++)
        {
            await CreateTestStudentAsync();
        }

        // Act - Measure P95 performance
        for (int i = 0; i < iterations; i++)
        {
            var elapsed = await MeasureOperationAsync(async () =>
            {
                var student = await CreateTestStudentAsync();
                
                // Perform CRUD cycle
                var updateCommand = new UpdateStudentCommand
                {
                    UserId = student.UserId,
                    FirstName = $"Updated{i}",
                    LastName = "Performance"
                };

                var studentService = _serviceProvider.GetRequiredService<StudentService>();
                var updateResult = await studentService.UpdateStudentAsync(updateCommand);
                
                // Read back
                var readQuery = new GetStudentQuery { UserId = student.UserId };
                var readResult = await studentService.GetStudentAsync(readQuery);

                return readResult.Value;
            });

            measurements.Add(elapsed.TotalMilliseconds);
        }

        // Assert - Calculate P95
        measurements.Sort();
        var p95Index = (int)Math.Ceiling(0.95 * measurements.Count) - 1;
        var p95Time = measurements[p95Index];

        p95Time.Should().BeLessThan(200, "P95 latency should be below 200ms");
        
        var avgTime = measurements.Average();
        var maxTime = measurements.Max();

        _output.WriteLine($"CRUD Performance Summary:");
        _output.WriteLine($"  Average: {avgTime:F2}ms");
        _output.WriteLine($"  P95: {p95Time:F2}ms");
        _output.WriteLine($"  Max: {maxTime:F2}ms");
        _output.WriteLine($"  Iterations: {iterations}");
    }

    #endregion

    #region Helper Methods

    private async Task<TimeSpan> MeasureOperationAsync<T>(Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        await operation();
        stopwatch.Stop();
        
        _measurements.Add(stopwatch.ElapsedMilliseconds);
        return stopwatch.Elapsed;
    }

    private async Task<Student> CreateTestStudentAsync()
    {
        var student = new Student(
            $"STU-{Guid.NewGuid()}",
            "Test",
            "Student",
            new DateTime(2010, 1, 15),
            GradeLevel.FirstGrade);

        _context.Students.Add(student);
        await _context.SaveChangesAsync();
        return student;
    }

    private async Task CreateTestStudentsAsync(int count)
    {
        var students = new List<Student>();
        for (int i = 0; i < count; i++)
        {
            var student = new Student(
                $"STU-PERF-{Guid.NewGuid()}",
                $"Student{i:D4}",
                "Performance",
                new DateTime(2010, 1, 15),
                (GradeLevel)(i % 12)); // Distribute across grade levels

            students.Add(student);
        }

        _context.Students.AddRange(students);
        await _context.SaveChangesAsync();
    }

    private async Task CreateComplexTestDataAsync()
    {
        // Create test school year
        var schoolYear = new SchoolYear(2024, "2024-2025", 
            new DateTime(2024, 8, 15), new DateTime(2025, 6, 15));
        _context.SchoolYears.Add(schoolYear);

        // Create test school
        var school = new School("TEST001", "Performance Test School", SchoolType.Elementary);
        _context.Schools.Add(school);

        // Create test classes
        var classes = new List<Class>();
        for (int i = 0; i < 5; i++)
        {
            var testClass = new Class(
                $"CLASS-{i:D2}",
                $"Test Class {i}",
                "Mathematics",
                (GradeLevel)i,
                school.Id,
                schoolYear.Id);
            classes.Add(testClass);
            _context.Classes.Add(testClass);
        }

        await _context.SaveChangesAsync();

        // Create students with enrollments
        for (int i = 0; i < 100; i++)
        {
            var student = new Student(
                $"STU-COMPLEX-{i:D4}",
                $"Complex{i:D4}",
                "Student",
                new DateTime(2010, 1, 15),
                (GradeLevel)(i % 5));

            _context.Students.Add(student);
            await _context.SaveChangesAsync(); // Save to get ID

            // Enroll in a class
            var enrollment = new Enrollment(
                student.UserId,
                classes[i % classes.Count].Id,
                schoolYear.Id,
                student.GradeLevel);

            _context.Enrollments.Add(enrollment);
        }

        await _context.SaveChangesAsync();
    }

    private void WarmUpDatabase()
    {
        // Perform a simple query to warm up the context
        _context.Students.Any();
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

/// <summary>
/// Mock tenant context accessor for performance testing
/// </summary>
public class MockTenantContextAccessor : ITenantContextAccessor
{
    public string? GetCurrentTenantId() => "performance-test-tenant";
    public void SetTenant(TenantContext context) { }
}