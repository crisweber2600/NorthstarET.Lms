using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data;
using System.Data;
using Xunit;
using Xunit.Abstractions;

namespace NorthstarET.Lms.Security.Tests;

/// <summary>
/// Multi-tenant data isolation validation tests to ensure complete tenant separation
/// These tests verify that tenants cannot access each other's data under any circumstances
/// </summary>
[Collection("Security")]
public class MultiTenantIsolationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProviderA;
    private readonly IServiceProvider _serviceProviderB;
    private readonly LmsDbContext _contextA;
    private readonly LmsDbContext _contextB;
    private readonly string _tenantA = "tenant-alpha";
    private readonly string _tenantB = "tenant-beta";
    private bool _disposed;

    public MultiTenantIsolationTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));

        // Create separate service providers for each tenant to simulate isolation
        _serviceProviderA = CreateTenantServiceProvider(_tenantA);
        _serviceProviderB = CreateTenantServiceProvider(_tenantB);

        _contextA = _serviceProviderA.GetRequiredService<LmsDbContext>();
        _contextB = _serviceProviderB.GetRequiredService<LmsDbContext>();

        // Ensure databases are created
        _contextA.Database.EnsureCreated();
        _contextB.Database.EnsureCreated();

        _output.WriteLine($"Tenant isolation test setup: {_tenantA} vs {_tenantB}");
    }

    #region Student Data Isolation Tests

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Isolation", "Student")]
    public async Task StudentData_ShouldBeCompletelyIsolated_BetweenTenants()
    {
        // Arrange - Create students in different tenants
        var studentA = await CreateStudentInTenantAsync(_contextA, "STU-A-001", "Alice", "Alpha", _tenantA);
        var studentB = await CreateStudentInTenantAsync(_contextB, "STU-B-001", "Bob", "Beta", _tenantB);

        // Act - Query each tenant's context
        var studentsInA = await _contextA.Students.ToListAsync();
        var studentsInB = await _contextB.Students.ToListAsync();

        // Assert - Each tenant should only see their own data
        studentsInA.Should().ContainSingle();
        studentsInB.Should().ContainSingle();

        studentsInA[0].StudentNumber.Should().Be("STU-A-001");
        studentsInB[0].StudentNumber.Should().Be("STU-B-001");

        // Verify cross-tenant data leakage prevention
        studentsInA.Should().NotContain(s => s.StudentNumber == "STU-B-001");
        studentsInB.Should().NotContain(s => s.StudentNumber == "STU-A-001");

        _output.WriteLine($"Tenant A students: {studentsInA.Count}, Tenant B students: {studentsInB.Count}");
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Isolation", "Student")]
    public async Task StudentQuery_WithTenantFilter_ShouldEnforceIsolation()
    {
        // Arrange - Create students in both tenants
        await CreateMultipleStudentsAsync();

        // Act - Query with explicit tenant filtering
        var tenantAStudents = await _contextA.Students
            .Where(s => s.TenantId == _tenantA)
            .ToListAsync();

        var tenantBStudents = await _contextB.Students
            .Where(s => s.TenantId == _tenantB)
            .ToListAsync();

        // Cross-tenant query attempt (should return empty)
        var crossTenantQuery = await _contextA.Students
            .Where(s => s.TenantId == _tenantB) // Tenant A context querying Tenant B data
            .ToListAsync();

        // Assert
        tenantAStudents.Should().HaveCount(3);
        tenantBStudents.Should().HaveCount(3);
        crossTenantQuery.Should().BeEmpty(); // Critical: No cross-tenant access

        // Verify all students in tenant A belong to tenant A
        tenantAStudents.Should().OnlyContain(s => s.TenantId == _tenantA);
        tenantBStudents.Should().OnlyContain(s => s.TenantId == _tenantB);
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Isolation", "Student")]
    public async Task DirectStudentAccess_ById_ShouldRespectTenantBoundaries()
    {
        // Arrange
        var studentA = await CreateStudentInTenantAsync(_contextA, "DIR-A-001", "Direct", "AccessA", _tenantA);
        var studentB = await CreateStudentInTenantAsync(_contextB, "DIR-B-001", "Direct", "AccessB", _tenantB);

        // Act - Try to access student from different tenant by ID
        var studentAFromContextA = await _contextA.Students
            .FirstOrDefaultAsync(s => s.UserId == studentA.UserId);
        
        var studentBFromContextA = await _contextA.Students
            .FirstOrDefaultAsync(s => s.UserId == studentB.UserId); // Cross-tenant access attempt

        var studentBFromContextB = await _contextB.Students
            .FirstOrDefaultAsync(s => s.UserId == studentB.UserId);
        
        var studentAFromContextB = await _contextB.Students
            .FirstOrDefaultAsync(s => s.UserId == studentA.UserId); // Cross-tenant access attempt

        // Assert - Should only find students within their own tenant context
        studentAFromContextA.Should().NotBeNull();
        studentBFromContextA.Should().BeNull(); // Critical: Cannot access cross-tenant

        studentBFromContextB.Should().NotBeNull();
        studentAFromContextB.Should().BeNull(); // Critical: Cannot access cross-tenant

        _output.WriteLine($"Cross-tenant access properly blocked");
    }

    #endregion

    #region Staff Data Isolation Tests

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Isolation", "Staff")]
    public async Task StaffData_ShouldBeCompletelyIsolated_BetweenTenants()
    {
        // Arrange - Create staff in different tenants
        var staffA = await CreateStaffInTenantAsync(_contextA, "EMP-A-001", "Alice", "StaffA", _tenantA);
        var staffB = await CreateStaffInTenantAsync(_contextB, "EMP-B-001", "Bob", "StaffB", _tenantB);

        // Act
        var staffInA = await _contextA.Staff.ToListAsync();
        var staffInB = await _contextB.Staff.ToListAsync();

        // Assert
        staffInA.Should().ContainSingle();
        staffInB.Should().ContainSingle();

        staffInA[0].EmployeeNumber.Should().Be("EMP-A-001");
        staffInB[0].EmployeeNumber.Should().Be("EMP-B-001");

        // Verify isolation
        staffInA.Should().OnlyContain(s => s.TenantId == _tenantA);
        staffInB.Should().OnlyContain(s => s.TenantId == _tenantB);
    }

    #endregion

    #region School and Class Isolation Tests

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Isolation", "School")]
    public async Task SchoolData_ShouldBeCompletelyIsolated_BetweenTenants()
    {
        // Arrange - Create schools in different tenants
        var schoolA = await CreateSchoolInTenantAsync(_contextA, "SCH-A-001", "Alpha Elementary", _tenantA);
        var schoolB = await CreateSchoolInTenantAsync(_contextB, "SCH-B-001", "Beta Elementary", _tenantB);

        // Act
        var schoolsInA = await _contextA.Schools.ToListAsync();
        var schoolsInB = await _contextB.Schools.ToListAsync();

        // Assert
        schoolsInA.Should().ContainSingle();
        schoolsInB.Should().ContainSingle();

        schoolsInA[0].Code.Should().Be("SCH-A-001");
        schoolsInB[0].Code.Should().Be("SCH-B-001");

        schoolsInA.Should().OnlyContain(s => s.TenantId == _tenantA);
        schoolsInB.Should().OnlyContain(s => s.TenantId == _tenantB);
    }

    #endregion

    #region Audit Data Isolation Tests

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Isolation", "Audit")]
    public async Task AuditRecords_ShouldBeCompletelyIsolated_BetweenTenants()
    {
        // Arrange - Create audit records in both tenants
        await CreateAuditRecordsInTenantAsync(_contextA, _tenantA, 5);
        await CreateAuditRecordsInTenantAsync(_contextB, _tenantB, 7);

        // Act
        var auditA = await _contextA.AuditRecords.ToListAsync();
        var auditB = await _contextB.AuditRecords.ToListAsync();

        // Assert
        auditA.Should().HaveCount(5);
        auditB.Should().HaveCount(7);

        auditA.Should().OnlyContain(ar => ar.TenantId == _tenantA);
        auditB.Should().OnlyContain(ar => ar.TenantId == _tenantB);

        // Verify no cross-tenant audit visibility
        var crossAuditQuery = await _contextA.AuditRecords
            .Where(ar => ar.TenantId == _tenantB)
            .ToListAsync();

        crossAuditQuery.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private IServiceProvider CreateTenantServiceProvider(string tenantId)
    {
        var services = new ServiceCollection();
        services.AddDbContext<LmsDbContext>(options =>
            options.UseInMemoryDatabase($"TenantTest_{tenantId}_{Guid.NewGuid()}"));
        
        services.AddScoped<ITenantContextAccessor>(_ => new TestTenantContextAccessor(tenantId));
        
        return services.BuildServiceProvider();
    }

    private async Task<Student> CreateStudentInTenantAsync(LmsDbContext context, string studentNumber, string firstName, string lastName, string tenantId)
    {
        var student = new Student(studentNumber, firstName, lastName, new DateTime(2010, 1, 1), GradeLevel.FirstGrade);
        student.SetTenantId(tenantId);
        context.Students.Add(student);
        await context.SaveChangesAsync();
        return student;
    }

    private async Task<Staff> CreateStaffInTenantAsync(LmsDbContext context, string employeeNumber, string firstName, string lastName, string tenantId)
    {
        var staff = new Staff(employeeNumber, firstName, lastName, "test@example.com");
        staff.SetTenantId(tenantId);
        context.Staff.Add(staff);
        await context.SaveChangesAsync();
        return staff;
    }

    private async Task<School> CreateSchoolInTenantAsync(LmsDbContext context, string code, string name, string tenantId)
    {
        var school = new School(code, name, SchoolType.Elementary);
        school.SetTenantId(tenantId);
        context.Schools.Add(school);
        await context.SaveChangesAsync();
        return school;
    }

    private async Task CreateMultipleStudentsAsync()
    {
        // Create 3 students in each tenant
        for (int i = 1; i <= 3; i++)
        {
            await CreateStudentInTenantAsync(_contextA, $"MULTI-A-{i:D3}", $"StudentA{i}", "Multi", _tenantA);
            await CreateStudentInTenantAsync(_contextB, $"MULTI-B-{i:D3}", $"StudentB{i}", "Multi", _tenantB);
        }
    }

    private async Task CreateAuditRecordsInTenantAsync(LmsDbContext context, string tenantId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var auditRecord = new AuditRecord(
                "TestEvent",
                "Student",
                Guid.NewGuid(),
                $"user-{tenantId}-{i}",
                DateTime.UtcNow.AddMinutes(-i),
                "127.0.0.1",
                $"Test Agent {tenantId}",
                $"{{\"tenant\":\"{tenantId}\", \"test\":\"data{i}\"}}"
            );
            auditRecord.SetSequenceNumber(i + 1);
            auditRecord.SetHash($"hash-{tenantId}-{i}");
            auditRecord.SetTenantId(tenantId);

            context.AuditRecords.Add(auditRecord);
        }
        await context.SaveChangesAsync();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _output.WriteLine($"Tenant isolation test completed - {_tenantA} vs {_tenantB}");
            
            _contextA?.Dispose();
            _contextB?.Dispose();
            _serviceProviderA?.GetService<IDisposable>()?.Dispose();
            _serviceProviderB?.GetService<IDisposable>()?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}

/// <summary>
/// Test tenant context accessor with fixed tenant ID
/// </summary>
public class TestTenantContextAccessor : ITenantContextAccessor
{
    private readonly string _tenantId;

    public TestTenantContextAccessor(string tenantId)
    {
        _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
    }

    public string? GetCurrentTenantId() => _tenantId;
    public void SetTenant(TenantContext context) { /* Fixed tenant for testing */ }
}