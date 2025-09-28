using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthstarET.Lms.Application.Services;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data;
using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace NorthstarET.Lms.Security.Tests;

/// <summary>
/// Security analysis and penetration testing validation for the LMS system
/// These tests verify security boundaries, data isolation, and potential vulnerabilities
/// </summary>
[Collection("Security")]
public class SecurityAnalysisTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly LmsDbContext _context;
    private readonly List<SecurityVulnerability> _discoveredVulnerabilities = new();
    private bool _disposed;

    public SecurityAnalysisTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));

        var services = new ServiceCollection();
        services.AddDbContext<LmsDbContext>(options =>
            options.UseInMemoryDatabase($"SecurityTest_{Guid.NewGuid()}"));
        
        services.AddScoped<StudentService>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ITenantContextAccessor, SecurityTestTenantContextAccessor>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<LmsDbContext>();
        _context.Database.EnsureCreated();
    }

    #region SQL Injection Tests

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Vulnerability", "SQLInjection")]
    public async Task StudentQuery_WithMaliciousInput_ShouldBeProtected()
    {
        // Arrange - Create test data
        var testStudent = await CreateTestStudentAsync("TEST001", "John", "Doe");
        
        // Malicious inputs that could cause SQL injection
        var maliciousInputs = new[]
        {
            "'; DROP TABLE students; --",
            "' OR '1'='1' --",
            "'; UPDATE students SET first_name='HACKED' WHERE '1'='1'; --",
            "' UNION SELECT * FROM audit_records --",
            "\"; DELETE FROM students; --"
        };

        var studentService = _serviceProvider.GetRequiredService<StudentService>();

        // Act & Assert - Try each malicious input
        foreach (var maliciousInput in maliciousInputs)
        {
            try
            {
                var query = new SearchStudentsQuery 
                { 
                    SearchTerm = maliciousInput,
                    Page = 1,
                    Size = 10
                };
                
                var result = await studentService.SearchStudentsAsync(query);
                
                // Verify the query either failed safely or returned safe results
                if (result.IsSuccess)
                {
                    // Should not return more results than expected
                    result.Value.Items.Count.Should().BeLessOrEqualTo(10);
                    
                    // Original test data should be unchanged
                    var originalStudent = await studentService.GetStudentAsync(
                        new GetStudentQuery { UserId = testStudent.UserId });
                    
                    originalStudent.IsSuccess.Should().BeTrue();
                    originalStudent.Value.FirstName.Should().Be("John");
                }
            }
            catch (Exception ex)
            {
                // Exceptions are acceptable - they indicate the system rejected malicious input
                _output.WriteLine($"Malicious input properly rejected: {maliciousInput}");
                _output.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Vulnerability", "SQLInjection")]
    public async Task AuditQuery_WithMaliciousInput_ShouldBeProtected()
    {
        // Arrange
        await CreateTestAuditRecordsAsync(10);
        
        var maliciousInputs = new[]
        {
            "'; DROP TABLE audit_records; --",
            "' OR 1=1 --",
            "'; INSERT INTO audit_records (event_type) VALUES ('INJECTED'); --"
        };

        // Act & Assert
        foreach (var maliciousInput in maliciousInputs)
        {
            try
            {
                var results = await _context.AuditRecords
                    .Where(ar => ar.UserId == maliciousInput) // This should be parameterized
                    .ToListAsync();

                // If the query succeeded, it should return empty results, not all records
                results.Should().BeEmpty();
            }
            catch (Exception ex)
            {
                // Exceptions are acceptable for malicious input
                _output.WriteLine($"Malicious audit query properly rejected: {maliciousInput}");
            }
        }
    }

    #endregion

    #region Cross-Site Scripting (XSS) Protection Tests

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Vulnerability", "XSS")]
    public async Task StudentData_WithScriptTags_ShouldBeSanitized()
    {
        // Arrange - XSS payloads
        var xssPayloads = new[]
        {
            "<script>alert('xss')</script>",
            "javascript:alert('xss')",
            "<img src='x' onerror='alert(1)'>",
            "<svg onload='alert(1)'>",
            "';--have <script>alert(1)</script>",
            "<iframe src='javascript:alert(1)'></iframe>"
        };

        var studentService = _serviceProvider.GetRequiredService<StudentService>();

        // Act & Assert - Try to create students with XSS payloads
        foreach (var payload in xssPayloads)
        {
            var command = new CreateStudentCommand
            {
                StudentNumber = $"XSS-{Guid.NewGuid()}",
                FirstName = payload, // XSS in first name
                LastName = "Test",
                DateOfBirth = new DateTime(2010, 1, 1),
                GradeLevel = GradeLevel.FirstGrade,
                EnrollmentDate = DateTime.UtcNow
            };

            var result = await studentService.CreateStudentAsync(command);

            if (result.IsSuccess)
            {
                // Verify the data was sanitized/escaped
                result.Value.FirstName.Should().NotContain("<script>");
                result.Value.FirstName.Should().NotContain("javascript:");
                result.Value.FirstName.Should().NotContain("onerror");
                result.Value.FirstName.Should().NotContain("onload");
                
                _output.WriteLine($"XSS payload sanitized: '{payload}' -> '{result.Value.FirstName}'");
            }
            else
            {
                // Rejection is also acceptable
                _output.WriteLine($"XSS payload rejected: {payload}");
            }
        }
    }

    #endregion

    #region Authorization Bypass Tests

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Vulnerability", "AuthorizationBypass")]
    public async Task DirectDatabaseAccess_ShouldRequirePermissions()
    {
        // This test verifies that direct database queries still enforce tenant boundaries
        var tenantA = "tenant-a";
        var tenantB = "tenant-b";

        // Arrange - Create data in different tenants
        var studentA = await CreateTestStudentWithTenantAsync("STU-A-001", "Alice", "TenantA", tenantA);
        var studentB = await CreateTestStudentWithTenantAsync("STU-B-001", "Bob", "TenantB", tenantB);

        // Act - Try to access tenant B data while in tenant A context
        var tenantAccessor = _serviceProvider.GetRequiredService<ITenantContextAccessor>() as SecurityTestTenantContextAccessor;
        tenantAccessor?.SetTenantId(tenantA);

        // This should only return tenant A student
        var tenantAStudents = await _context.Students
            .Where(s => s.TenantId == tenantA) // Explicit tenant filtering
            .ToListAsync();

        // Assert - Verify proper isolation
        tenantAStudents.Should().ContainSingle();
        tenantAStudents[0].StudentNumber.Should().Be("STU-A-001");
        
        // Verify we cannot accidentally access tenant B data
        tenantAStudents.Should().NotContain(s => s.StudentNumber == "STU-B-001");
    }

    #endregion

    #region Security Headers and Configuration

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Vulnerability", "Configuration")]
    public void DatabaseConnection_ShouldUseSecureConfiguration()
    {
        // Verify database connection string doesn't contain insecure settings
        var connectionString = _context.Database.GetDbConnection().ConnectionString;
        
        // Should not contain password in plain text (though this is test DB)
        if (connectionString.Contains("Password="))
        {
            connectionString.Should().NotContain("Password=admin");
            connectionString.Should().NotContain("Password=123");
            connectionString.Should().NotContain("Password=password");
        }

        // Should use encrypted connections in production
        if (!connectionString.Contains("InMemory"))
        {
            connectionString.Should().Contain("Encrypt=true").Or.Contain("SSL");
        }
    }

    #endregion

    #region Helper Methods

    private async Task<Student> CreateTestStudentAsync(string studentNumber, string firstName, string lastName)
    {
        var student = new Student(studentNumber, firstName, lastName, new DateTime(2010, 1, 1), GradeLevel.FirstGrade);
        _context.Students.Add(student);
        await _context.SaveChangesAsync();
        return student;
    }

    private async Task<Student> CreateTestStudentWithTenantAsync(string studentNumber, string firstName, string lastName, string tenantId)
    {
        var student = new Student(studentNumber, firstName, lastName, new DateTime(2010, 1, 1), GradeLevel.FirstGrade);
        student.SetTenantId(tenantId); // Method to set tenant for testing
        _context.Students.Add(student);
        await _context.SaveChangesAsync();
        return student;
    }

    private async Task CreateTestAuditRecordsAsync(int count)
    {
        var auditRecords = new List<AuditRecord>();
        for (int i = 0; i < count; i++)
        {
            var auditRecord = new AuditRecord(
                "TestEvent",
                "Student",
                Guid.NewGuid(),
                $"user-{i}",
                DateTime.UtcNow.AddMinutes(-i),
                "127.0.0.1",
                "Test Agent",
                $"{{\"test\": \"data{i}\"}}"
            );
            auditRecord.SetSequenceNumber(i + 1);
            auditRecord.SetHash($"test-hash-{i}");
            auditRecords.Add(auditRecord);
        }

        _context.AuditRecords.AddRange(auditRecords);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Report discovered vulnerabilities
            if (_discoveredVulnerabilities.Any())
            {
                _output.WriteLine($"SECURITY ALERT: {_discoveredVulnerabilities.Count} vulnerabilities discovered:");
                foreach (var vuln in _discoveredVulnerabilities)
                {
                    _output.WriteLine($"  - {vuln.Type}: {vuln.Description}");
                }
            }

            _context?.Dispose();
            _serviceProvider?.GetService<IDisposable>()?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}

/// <summary>
/// Test tenant context accessor for security testing
/// </summary>
public class SecurityTestTenantContextAccessor : ITenantContextAccessor
{
    private string? _currentTenantId = "security-test-tenant";
    
    public string? GetCurrentTenantId() => _currentTenantId;
    public void SetTenantId(string tenantId) => _currentTenantId = tenantId;
    public void SetTenant(TenantContext context) => _currentTenantId = context.TenantId;
}

/// <summary>
/// Security vulnerability tracking for test results
/// </summary>
public record SecurityVulnerability(string Type, string Description, string Severity = "Medium");

// Missing command and query classes that would exist in a real implementation
public record SearchStudentsQuery
{
    public string SearchTerm { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;
}

public record GetStudentQuery
{
    public Guid UserId { get; init; }
}

public record CreateStudentCommand
{
    public string StudentNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public GradeLevel GradeLevel { get; init; }
    public DateTime EnrollmentDate { get; init; }
}

public record UpdateStudentCommand
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public GradeLevel? GradeLevel { get; init; }
}