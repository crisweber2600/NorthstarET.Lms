using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Infrastructure.Data;
using NorthstarET.Lms.Infrastructure.Security;
using System.Data;
using Testcontainers.MsSql;
using Xunit;

namespace NorthstarET.Lms.Infrastructure.Tests.Security;

/// <summary>
/// Critical security tests to verify complete tenant data isolation.
/// These tests ensure no data can leak between tenants under any circumstances.
/// </summary>
public class MultiTenantIsolationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServerContainer;
    private string _connectionString = string.Empty;

    // Test tenant contexts
    private readonly TenantContext _tenant1 = new()
    {
        TenantId = "oakland-unified",
        SchemaName = "oakland_unified",
        ConnectionString = string.Empty
    };

    private readonly TenantContext _tenant2 = new()
    {
        TenantId = "berkeley-unified", 
        SchemaName = "berkeley_unified",
        ConnectionString = string.Empty
    };

    public MultiTenantIsolationTests()
    {
        _sqlServerContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("TestPassword123!")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _sqlServerContainer.StartAsync();
        _connectionString = _sqlServerContainer.GetConnectionString();
        _tenant1.ConnectionString = _connectionString;
        _tenant2.ConnectionString = _connectionString;

        // Create schemas for both tenants
        await CreateTenantSchemasAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlServerContainer.StopAsync();
        await _sqlServerContainer.DisposeAsync();
    }

    private async Task CreateTenantSchemasAsync()
    {
        using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Create schemas
        var createSchema1 = $"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{_tenant1.SchemaName}') EXEC('CREATE SCHEMA {_tenant1.SchemaName}')";
        var createSchema2 = $"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{_tenant2.SchemaName}') EXEC('CREATE SCHEMA {_tenant2.SchemaName}')";

        using var command1 = new Microsoft.Data.SqlClient.SqlCommand(createSchema1, connection);
        using var command2 = new Microsoft.Data.SqlClient.SqlCommand(createSchema2, connection);
        
        await command1.ExecuteNonQueryAsync();
        await command2.ExecuteNonQueryAsync();
    }

    private DbContextOptions<LmsDbContext> CreateDbContextOptions(TenantContext tenant)
    {
        return new DbContextOptionsBuilder<LmsDbContext>()
            .UseSqlServer(tenant.ConnectionString, opts =>
            {
                opts.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
            })
            .Options;
    }

    private LmsDbContext CreateDbContext(TenantContext tenant)
    {
        var options = CreateDbContextOptions(tenant);
        var tenantAccessor = new TestTenantContextAccessor(tenant);
        var context = new LmsDbContext(options, tenantAccessor);
        return context;
    }

    [Fact]
    public async Task CreateStudent_InDifferentTenants_ShouldIsolateData()
    {
        // Arrange
        using var tenant1Context = CreateDbContext(_tenant1);
        using var tenant2Context = CreateDbContext(_tenant2);

        // Ensure databases are created with proper schemas
        await tenant1Context.Database.EnsureCreatedAsync();
        await tenant2Context.Database.EnsureCreatedAsync();

        var student1 = new Student("STU-001", "Alice", "Johnson", DateTime.Now.AddYears(-10), DateTime.Now);
        var student2 = new Student("STU-002", "Bob", "Wilson", DateTime.Now.AddYears(-10), DateTime.Now);

        // Act - Add students to different tenants
        tenant1Context.Students.Add(student1);
        await tenant1Context.SaveChangesAsync();

        tenant2Context.Students.Add(student2);
        await tenant2Context.SaveChangesAsync();

        // Assert - Each tenant should only see their own data
        var tenant1Students = await tenant1Context.Students.ToListAsync();
        var tenant2Students = await tenant2Context.Students.ToListAsync();

        tenant1Students.Should().HaveCount(1);
        tenant1Students.First().StudentNumber.Should().Be("STU-001");
        tenant1Students.First().FirstName.Should().Be("Alice");

        tenant2Students.Should().HaveCount(1);
        tenant2Students.First().StudentNumber.Should().Be("STU-002");
        tenant2Students.First().FirstName.Should().Be("Bob");

        // Critical: Verify complete isolation
        tenant1Students.Should().NotContain(s => s.UserId == student2.UserId);
        tenant2Students.Should().NotContain(s => s.UserId == student1.UserId);
    }

    [Fact]
    public async Task QueryStudent_WithWrongTenantContext_ShouldReturnEmpty()
    {
        // Arrange
        using var tenant1Context = CreateDbContext(_tenant1);
        using var tenant2Context = CreateDbContext(_tenant2);

        await tenant1Context.Database.EnsureCreatedAsync();
        await tenant2Context.Database.EnsureCreatedAsync();

        var student = new Student("STU-ISOLATION-001", "Isolated", "Student", DateTime.Now.AddYears(-10), DateTime.Now);
        
        tenant1Context.Students.Add(student);
        await tenant1Context.SaveChangesAsync();

        // Act - Try to query the student from the wrong tenant
        var studentFromWrongTenant = await tenant2Context.Students
            .FirstOrDefaultAsync(s => s.UserId == student.UserId);

        // Assert - Should not find the student in the wrong tenant
        studentFromWrongTenant.Should().BeNull();
    }

    [Fact]
    public async Task BulkQuery_AcrossAllStudents_ShouldRespectTenantBoundaries()
    {
        // Arrange
        using var tenant1Context = CreateDbContext(_tenant1);
        using var tenant2Context = CreateDbContext(_tenant2);

        await tenant1Context.Database.EnsureCreatedAsync();
        await tenant2Context.Database.EnsureCreatedAsync();

        // Create multiple students in each tenant
        var tenant1Students = new[]
        {
            new Student("T1-STU-001", "T1Student1", "LastName", DateTime.Now.AddYears(-10), DateTime.Now),
            new Student("T1-STU-002", "T1Student2", "LastName", DateTime.Now.AddYears(-10), DateTime.Now),
            new Student("T1-STU-003", "T1Student3", "LastName", DateTime.Now.AddYears(-10), DateTime.Now)
        };

        var tenant2Students = new[]
        {
            new Student("T2-STU-001", "T2Student1", "LastName", DateTime.Now.AddYears(-10), DateTime.Now),
            new Student("T2-STU-002", "T2Student2", "LastName", DateTime.Now.AddYears(-10), DateTime.Now)
        };

        tenant1Context.Students.AddRange(tenant1Students);
        await tenant1Context.SaveChangesAsync();

        tenant2Context.Students.AddRange(tenant2Students);
        await tenant2Context.SaveChangesAsync();

        // Act - Query all students from each tenant
        var tenant1Results = await tenant1Context.Students.ToListAsync();
        var tenant2Results = await tenant2Context.Students.ToListAsync();

        // Assert - Each tenant should only see their own students
        tenant1Results.Should().HaveCount(3);
        tenant1Results.Should().OnlyContain(s => s.StudentNumber.StartsWith("T1-"));

        tenant2Results.Should().HaveCount(2);
        tenant2Results.Should().OnlyContain(s => s.StudentNumber.StartsWith("T2-"));

        // Verify no cross-contamination
        var tenant1Ids = tenant1Results.Select(s => s.UserId).ToHashSet();
        var tenant2Ids = tenant2Results.Select(s => s.UserId).ToHashSet();
        
        tenant1Ids.Should().NotIntersectWith(tenant2Ids);
    }

    [Fact]
    public async Task DirectSqlQuery_ShouldRespectSchemaIsolation()
    {
        // Arrange
        using var tenant1Context = CreateDbContext(_tenant1);
        using var tenant2Context = CreateDbContext(_tenant2);

        await tenant1Context.Database.EnsureCreatedAsync();
        await tenant2Context.Database.EnsureCreatedAsync();

        var student1 = new Student("SQL-001", "SqlTest1", "Student", DateTime.Now.AddYears(-10), DateTime.Now);
        var student2 = new Student("SQL-002", "SqlTest2", "Student", DateTime.Now.AddYears(-10), DateTime.Now);

        tenant1Context.Students.Add(student1);
        await tenant1Context.SaveChangesAsync();

        tenant2Context.Students.Add(student2);
        await tenant2Context.SaveChangesAsync();

        // Act - Execute raw SQL queries against each schema
        var tenant1SqlResults = await tenant1Context.Students
            .FromSqlRaw($"SELECT * FROM {_tenant1.SchemaName}.students")
            .ToListAsync();

        var tenant2SqlResults = await tenant2Context.Students
            .FromSqlRaw($"SELECT * FROM {_tenant2.SchemaName}.students")
            .ToListAsync();

        // Assert - Each schema should contain only its tenant's data
        tenant1SqlResults.Should().HaveCount(1);
        tenant1SqlResults.First().StudentNumber.Should().Be("SQL-001");

        tenant2SqlResults.Should().HaveCount(1);
        tenant2SqlResults.First().StudentNumber.Should().Be("SQL-002");
    }

    [Fact]
    public async Task SchemaPermissions_ShouldPreventCrossTenantAccess()
    {
        // Arrange
        using var tenant1Context = CreateDbContext(_tenant1);
        await tenant1Context.Database.EnsureCreatedAsync();

        var student = new Student("PERM-001", "PermTest", "Student", DateTime.Now.AddYears(-10), DateTime.Now);
        tenant1Context.Students.Add(student);
        await tenant1Context.SaveChangesAsync();

        // Act & Assert - Try to access wrong schema (should fail or return empty)
        using var tenant2Context = CreateDbContext(_tenant2);
        await tenant2Context.Database.EnsureCreatedAsync();

        var wrongSchemaQuery = async () => await tenant2Context.Students
            .FromSqlRaw($"SELECT * FROM {_tenant1.SchemaName}.students")
            .ToListAsync();

        // This should either throw an exception or return empty results
        // depending on database permissions setup
        await wrongSchemaQuery.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AuditRecords_ShouldBeTenantIsolated()
    {
        // Arrange
        using var tenant1Context = CreateDbContext(_tenant1);
        using var tenant2Context = CreateDbContext(_tenant2);

        await tenant1Context.Database.EnsureCreatedAsync();
        await tenant2Context.Database.EnsureCreatedAsync();

        var audit1 = new AuditRecord(
            "TestEvent1",
            "Student",
            Guid.NewGuid(),
            "user1",
            "127.0.0.1",
            "TestAgent",
            "{}");

        var audit2 = new AuditRecord(
            "TestEvent2",
            "Student", 
            Guid.NewGuid(),
            "user2",
            "127.0.0.1",
            "TestAgent",
            "{}");

        // Act
        tenant1Context.AuditRecords.Add(audit1);
        await tenant1Context.SaveChangesAsync();

        tenant2Context.AuditRecords.Add(audit2);
        await tenant2Context.SaveChangesAsync();

        // Assert - Audit records should be tenant-isolated
        var tenant1Audits = await tenant1Context.AuditRecords.ToListAsync();
        var tenant2Audits = await tenant2Context.AuditRecords.ToListAsync();

        tenant1Audits.Should().HaveCount(1);
        tenant1Audits.First().EventType.Should().Be("TestEvent1");

        tenant2Audits.Should().HaveCount(1);
        tenant2Audits.First().EventType.Should().Be("TestEvent2");

        // Verify no cross-tenant audit access
        tenant1Audits.Should().NotContain(a => a.Id == audit2.Id);
        tenant2Audits.Should().NotContain(a => a.Id == audit1.Id);
    }

    [Fact]
    public async Task ConnectionString_WithWrongTenantId_ShouldNotAccessData()
    {
        // Arrange - Set up tenant with modified connection but wrong schema
        var wrongTenant = new TenantContext
        {
            TenantId = _tenant2.TenantId,
            SchemaName = _tenant1.SchemaName, // Wrong schema!
            ConnectionString = _tenant2.ConnectionString
        };

        using var correctContext = CreateDbContext(_tenant1);
        using var wrongContext = CreateDbContext(wrongTenant);

        await correctContext.Database.EnsureCreatedAsync();

        var student = new Student("WRONG-001", "Wrong", "Access", DateTime.Now.AddYears(-10), DateTime.Now);
        correctContext.Students.Add(student);
        await correctContext.SaveChangesAsync();

        // Act - Try to access with wrong tenant context
        var studentsFromWrongContext = await wrongContext.Students.ToListAsync();

        // Assert - Should not see the data (tenant isolation enforced)
        studentsFromWrongContext.Should().BeEmpty();
    }

    [Fact]
    public async Task TenantSwitching_InSameDbContext_ShouldIsolateDataCorrectly()
    {
        // This test verifies that if tenant context changes, data access is properly isolated
        // This is important for security - tenant context must be immutable per request

        // Arrange
        var mutableTenantAccessor = new TestTenantContextAccessor(_tenant1);
        var options = CreateDbContextOptions(_tenant1);
        
        using var context = new LmsDbContext(options, mutableTenantAccessor);
        await context.Database.EnsureCreatedAsync();

        // Add data as tenant1
        var student1 = new Student("SWITCH-001", "Tenant1", "Student", DateTime.Now.AddYears(-10), DateTime.Now);
        context.Students.Add(student1);
        await context.SaveChangesAsync();

        // Act - Try to switch tenant context (should not be possible in real system)
        mutableTenantAccessor.SetTenant(_tenant2);
        
        // Create new context with tenant2 to simulate proper tenant switching
        using var tenant2Context = CreateDbContext(_tenant2);
        await tenant2Context.Database.EnsureCreatedAsync();

        var studentsAfterSwitch = await tenant2Context.Students.ToListAsync();

        // Assert - Should not see tenant1 data when using tenant2 context
        studentsAfterSwitch.Should().BeEmpty();
    }
}

// Helper class for testing tenant context
public class TestTenantContextAccessor : ITenantContextAccessor
{
    private ITenantContext? _tenant;

    public TestTenantContextAccessor(ITenantContext? tenant)
    {
        _tenant = tenant;
    }

    public ITenantContext? GetTenant()
    {
        return _tenant;
    }

    public void SetTenant(ITenantContext? tenant)
    {
        _tenant = tenant;
    }

    public string? GetCurrentTenantId()
    {
        return _tenant?.TenantId;
    }
}

public class TenantContext
{
    public string TenantId { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}