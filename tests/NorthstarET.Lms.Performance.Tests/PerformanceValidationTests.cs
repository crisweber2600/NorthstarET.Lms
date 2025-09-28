using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using Testcontainers.SqlServer;
using Xunit.Abstractions;

namespace NorthstarET.Lms.Tests.Performance;

public class PerformanceValidationTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private SqlServerContainer? _sqlContainer;

    public PerformanceValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ValidateCrudOperationsSLA_ShouldMeetPR001_200msP95()
    {
        // Arrange: Setup test container
        _sqlContainer = new SqlServerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test123!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .Build();

        await _sqlContainer.StartAsync();

        // Setup test table
        await SetupTestTable(_sqlContainer.GetConnectionString());
        
        // Act: Measure CRUD response times for 1000 operations
        var responseTimes = new List<double>();
        var stopwatch = new Stopwatch();

        for (int i = 0; i < 1000; i++)
        {
            stopwatch.Restart();
            
            // Simulate typical CRUD operations
            await SimulateCrudOperation(_sqlContainer.GetConnectionString(), i);
            
            stopwatch.Stop();
            responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        // Calculate P95 (95th percentile)
        responseTimes.Sort();
        var p95Index = (int)(responseTimes.Count * 0.95);
        var p95ResponseTime = responseTimes[p95Index];

        _output.WriteLine($"P95 Response Time: {p95ResponseTime:F2}ms");
        _output.WriteLine($"Average Response Time: {responseTimes.Average():F2}ms");
        _output.WriteLine($"Min Response Time: {responseTimes.Min():F2}ms");
        _output.WriteLine($"Max Response Time: {responseTimes.Max():F2}ms");

        // Assert: Verify SLA compliance (PR-001: <200ms P95)
        p95ResponseTime.Should().BeLessThan(200, 
            $"P95 response time {p95ResponseTime:F2}ms should be less than 200ms SLA requirement");
    }

    [Fact]
    public async Task ValidateBulkOperationsSLA_ShouldMeetPR002_120sMax()
    {
        // Arrange
        _sqlContainer = new SqlServerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test123!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .Build();

        await _sqlContainer.StartAsync();
        await SetupTestTable(_sqlContainer.GetConnectionString());

        var stopwatch = new Stopwatch();
        
        // Act: Simulate bulk student rollover (10,000 students)
        stopwatch.Start();
        await SimulateBulkStudentRollover(_sqlContainer.GetConnectionString(), 10000);
        stopwatch.Stop();

        var bulkOperationTime = stopwatch.Elapsed.TotalSeconds;

        _output.WriteLine($"Bulk Operation Time: {bulkOperationTime:F2}s");
        _output.WriteLine($"Records per second: {10000 / bulkOperationTime:F2}");
        
        // Assert: Verify SLA compliance (PR-002: <120s for bulk ops)
        bulkOperationTime.Should().BeLessThan(120, 
            $"Bulk operation time {bulkOperationTime:F2}s should be less than 120s SLA requirement");
    }

    [Fact]
    public async Task ValidateAuditQueriesSLA_ShouldMeetPR003_2sMax()
    {
        // Arrange
        _sqlContainer = new SqlServerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test123!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .Build();

        await _sqlContainer.StartAsync();

        // Setup audit data
        await SetupAuditTestData(_sqlContainer.GetConnectionString(), 100000);

        var stopwatch = new Stopwatch();
        
        // Act: Execute complex audit queries
        stopwatch.Start();
        await SimulateComplexAuditQuery(_sqlContainer.GetConnectionString());
        stopwatch.Stop();

        var auditQueryTime = stopwatch.Elapsed.TotalSeconds;

        _output.WriteLine($"Audit Query Time: {auditQueryTime:F2}s");
        _output.WriteLine($"Query processed 100k audit records");
        
        // Assert: Verify SLA compliance (PR-003: <2s for audit queries)
        auditQueryTime.Should().BeLessThan(2.0, 
            $"Audit query time {auditQueryTime:F2}s should be less than 2s SLA requirement");
    }

    private async Task SetupTestTable(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var createTable = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Students' AND xtype='U')
            CREATE TABLE Students (
                Id uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
                StudentNumber nvarchar(50) UNIQUE NOT NULL,
                FirstName nvarchar(100) NOT NULL,
                LastName nvarchar(100) NOT NULL,
                CreatedDate datetime2 DEFAULT GETUTCDATE(),
                INDEX IX_Students_StudentNumber (StudentNumber),
                INDEX IX_Students_LastName (LastName)
            )";
        
        await new SqlCommand(createTable, connection).ExecuteNonQueryAsync();
    }

    private async Task SimulateCrudOperation(string connectionString, int iteration)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var studentNumber = $"STU{iteration:000000}";
        
        // INSERT
        var insertCommand = new SqlCommand(@"
            INSERT INTO Students (StudentNumber, FirstName, LastName) 
            VALUES (@StudentNumber, @FirstName, @LastName)", connection);
        
        insertCommand.Parameters.AddWithValue("@StudentNumber", studentNumber);
        insertCommand.Parameters.AddWithValue("@FirstName", "Test");
        insertCommand.Parameters.AddWithValue("@LastName", "Student");
        
        await insertCommand.ExecuteNonQueryAsync();
        
        // UPDATE
        var updateCommand = new SqlCommand(@"
            UPDATE Students SET LastName = @UpdatedLastName 
            WHERE StudentNumber = @StudentNumber", connection);
        
        updateCommand.Parameters.AddWithValue("@StudentNumber", studentNumber);
        updateCommand.Parameters.AddWithValue("@UpdatedLastName", $"UpdatedStudent{iteration}");
        
        await updateCommand.ExecuteNonQueryAsync();
        
        // SELECT
        var selectCommand = new SqlCommand(@"
            SELECT Id, StudentNumber, FirstName, LastName, CreatedDate 
            FROM Students WHERE StudentNumber = @StudentNumber", connection);
        
        selectCommand.Parameters.AddWithValue("@StudentNumber", studentNumber);
        
        using var reader = await selectCommand.ExecuteReaderAsync();
        var found = await reader.ReadAsync();
        
        if (!found)
            throw new InvalidOperationException($"Student {studentNumber} not found after insert");
    }

    private async Task SimulateBulkStudentRollover(string connectionString, int studentCount)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Create promotions table
        var createPromotionsTable = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StudentPromotions' AND xtype='U')
            CREATE TABLE StudentPromotions (
                Id uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
                StudentId uniqueidentifier NOT NULL,
                FromGrade int NOT NULL,
                ToGrade int NOT NULL,
                PromotionDate datetime2 DEFAULT GETUTCDATE(),
                INDEX IX_StudentPromotions_StudentId (StudentId)
            )";
        
        await new SqlCommand(createPromotionsTable, connection).ExecuteNonQueryAsync();
        
        // Process in batches to avoid memory issues
        const int batchSize = 1000;
        for (int batch = 0; batch < studentCount / batchSize; batch++)
        {
            var bulkInsert = new StringBuilder();
            bulkInsert.AppendLine("INSERT INTO StudentPromotions (StudentId, FromGrade, ToGrade) VALUES");
            
            for (int i = 0; i < batchSize; i++)
            {
                var recordIndex = batch * batchSize + i;
                bulkInsert.AppendLine($"(NEWID(), NEWID(), {recordIndex % 12}, {(recordIndex % 12) + 1}){(i == batchSize - 1 ? "" : ",")}");
            }
            
            var command = new SqlCommand(bulkInsert.ToString(), connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task SetupAuditTestData(string connectionString, int recordCount)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Create audit table
        var createTable = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditRecords' AND xtype='U')
            CREATE TABLE AuditRecords (
                Id uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
                EventType nvarchar(50) NOT NULL,
                EntityType nvarchar(50) NOT NULL,
                EntityId uniqueidentifier NOT NULL,
                UserId nvarchar(50) NOT NULL,
                Timestamp datetime2 DEFAULT GETUTCDATE(),
                Details nvarchar(max),
                INDEX IX_AuditRecords_Timestamp (Timestamp),
                INDEX IX_AuditRecords_EntityType_EventType (EntityType, EventType),
                INDEX IX_AuditRecords_UserId (UserId)
            )";
        
        await new SqlCommand(createTable, connection).ExecuteNonQueryAsync();
        
        // Insert test audit records in batches
        const int batchSize = 1000;
        var eventTypes = new[] { "Create", "Update", "Delete", "View" };
        var entityTypes = new[] { "Student", "Staff", "Class", "Assessment" };
        
        for (int batch = 0; batch < recordCount / batchSize; batch++)
        {
            var batchInsert = new StringBuilder();
            batchInsert.AppendLine("INSERT INTO AuditRecords (EventType, EntityType, EntityId, UserId, Timestamp, Details) VALUES");
            
            for (int i = 0; i < batchSize; i++)
            {
                var recordIndex = batch * batchSize + i;
                var eventType = eventTypes[recordIndex % eventTypes.Length];
                var entityType = entityTypes[recordIndex % entityTypes.Length];
                var timestamp = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 30)).ToString("yyyy-MM-dd HH:mm:ss");
                
                batchInsert.AppendLine($"('{eventType}', '{entityType}', NEWID(), 'user{recordIndex % 100}', '{timestamp}', 'Test audit record {recordIndex}'){(i == batchSize - 1 ? "" : ",")}");
            }
            
            var command = new SqlCommand(batchInsert.ToString(), connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task SimulateComplexAuditQuery(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Complex audit query with aggregations and filtering
        var complexQuery = @"
            WITH AuditSummary AS (
                SELECT 
                    EntityType,
                    EventType,
                    DATEPART(hour, Timestamp) as HourOfDay,
                    CAST(Timestamp AS date) as EventDate,
                    COUNT(*) as EventCount,
                    COUNT(DISTINCT UserId) as UniqueUsers
                FROM AuditRecords 
                WHERE Timestamp >= DATEADD(day, -7, GETUTCDATE())
                GROUP BY EntityType, EventType, DATEPART(hour, Timestamp), CAST(Timestamp AS date)
            ),
            HourlyStats AS (
                SELECT 
                    EntityType,
                    EventType,
                    HourOfDay,
                    AVG(CAST(EventCount AS float)) as AvgEventsPerHour,
                    MAX(EventCount) as MaxEventsPerHour,
                    SUM(UniqueUsers) as TotalUniqueUsers,
                    COUNT(DISTINCT EventDate) as DaysActive
                FROM AuditSummary
                GROUP BY EntityType, EventType, HourOfDay
            )
            SELECT 
                EntityType,
                EventType,
                AVG(AvgEventsPerHour) as OverallAvgEventsPerHour,
                MAX(MaxEventsPerHour) as PeakEventsPerHour,
                SUM(TotalUniqueUsers) as CumulativeUniqueUsers,
                MAX(DaysActive) as MaxDaysActive,
                COUNT(*) as HoursWithActivity
            FROM HourlyStats
            GROUP BY EntityType, EventType
            HAVING AVG(AvgEventsPerHour) > 1
            ORDER BY OverallAvgEventsPerHour DESC";
        
        var command = new SqlCommand(complexQuery, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var results = new List<object>();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                EntityType = reader["EntityType"].ToString(),
                EventType = reader["EventType"].ToString(),
                OverallAvgEventsPerHour = Convert.ToDouble(reader["OverallAvgEventsPerHour"]),
                PeakEventsPerHour = Convert.ToInt32(reader["PeakEventsPerHour"]),
                CumulativeUniqueUsers = Convert.ToInt32(reader["CumulativeUniqueUsers"]),
                MaxDaysActive = Convert.ToInt32(reader["MaxDaysActive"]),
                HoursWithActivity = Convert.ToInt32(reader["HoursWithActivity"])
            });
        }
        
        _output.WriteLine($"Complex audit query processed and returned {results.Count} result rows");
        results.Count.Should().BeGreaterThan(0, "Query should return aggregated results");
    }

    public async ValueTask DisposeAsync()
    {
        if (_sqlContainer != null)
        {
            await _sqlContainer.DisposeAsync();
        }
    }
}