using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace NorthstarET.Lms.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes and validates audit chain integrity
/// </summary>
public class AuditProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditProcessorService> _logger;
    private readonly TimeSpan _runInterval = TimeSpan.FromMinutes(5); // Run every 5 minutes

    public AuditProcessorService(
        IServiceProvider serviceProvider,
        ILogger<AuditProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit Processor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAuditRecordsAsync(stoppingToken);
                await Task.Delay(_runInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Audit Processor Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in audit processor service");
                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ProcessAuditRecordsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LmsDbContext>();

        try
        {
            // Process unprocessed audit records (those without proper hash chaining)
            var unprocessedRecords = await dbContext.AuditRecords
                .Where(ar => string.IsNullOrEmpty(ar.RecordHash))
                .OrderBy(ar => ar.Timestamp)
                .ThenBy(ar => ar.SequenceNumber)
                .Take(100) // Process in batches
                .ToListAsync(cancellationToken);

            if (unprocessedRecords.Any())
            {
                await ProcessAuditChainAsync(dbContext, unprocessedRecords, cancellationToken);
            }

            // Periodically validate chain integrity
            if (DateTime.UtcNow.Minute % 30 == 0) // Every 30 minutes
            {
                await ValidateAuditChainIntegrityAsync(dbContext, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audit records");
            throw;
        }
    }

    private async Task ProcessAuditChainAsync(LmsDbContext dbContext, List<AuditRecord> auditRecords, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing {Count} unprocessed audit records", auditRecords.Count);

        try
        {
            foreach (var auditRecord in auditRecords)
            {
                // Get the previous audit record for chaining
                var previousRecord = await dbContext.AuditRecords
                    .Where(ar => ar.SequenceNumber < auditRecord.SequenceNumber)
                    .OrderByDescending(ar => ar.SequenceNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                // Set the previous record hash
                auditRecord.PreviousRecordHash = previousRecord?.RecordHash ?? string.Empty;

                // Calculate and set the record hash
                auditRecord.RecordHash = CalculateRecordHash(auditRecord);

                // Mark as processed
                dbContext.AuditRecords.Update(auditRecord);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully processed {Count} audit records", auditRecords.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audit chain");
            throw;
        }
    }

    private async Task ValidateAuditChainIntegrityAsync(LmsDbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Validating audit chain integrity");

        try
        {
            // Get a sample of recent audit records to validate
            var recentRecords = await dbContext.AuditRecords
                .Where(ar => ar.Timestamp >= DateTime.UtcNow.AddDays(-7)) // Last 7 days
                .OrderBy(ar => ar.SequenceNumber)
                .ToListAsync(cancellationToken);

            if (!recentRecords.Any())
            {
                _logger.LogDebug("No recent audit records found for validation");
                return;
            }

            var invalidRecords = new List<AuditRecord>();

            for (int i = 0; i < recentRecords.Count; i++)
            {
                var record = recentRecords[i];
                
                // Validate record hash
                var expectedHash = CalculateRecordHash(record);
                if (record.RecordHash != expectedHash)
                {
                    invalidRecords.Add(record);
                    _logger.LogError("Audit record {RecordId} has invalid hash. Expected: {Expected}, Actual: {Actual}",
                        record.Id, expectedHash, record.RecordHash);
                }

                // Validate chain linkage (except for the first record)
                if (i > 0)
                {
                    var previousRecord = recentRecords[i - 1];
                    if (record.PreviousRecordHash != previousRecord.RecordHash)
                    {
                        invalidRecords.Add(record);
                        _logger.LogError("Audit record {RecordId} has broken chain linkage. Expected previous hash: {Expected}, Actual: {Actual}",
                            record.Id, previousRecord.RecordHash, record.PreviousRecordHash);
                    }
                }
            }

            if (invalidRecords.Any())
            {
                _logger.LogCritical("AUDIT CHAIN INTEGRITY VIOLATION: Found {Count} invalid audit records", invalidRecords.Count);
                
                // Create a security alert for audit tampering
                var securityAlert = new AuditRecord(
                    eventType: "SecurityAlert",
                    entityType: "AuditRecord",
                    entityId: null,
                    userId: "SYSTEM",
                    changeDetails: $"Audit chain integrity violation detected. {invalidRecords.Count} invalid records found.",
                    correlationId: Guid.NewGuid().ToString()
                );

                await dbContext.AuditRecords.AddAsync(securityAlert, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug("Audit chain integrity validation passed for {Count} records", recentRecords.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating audit chain integrity");
            throw;
        }
    }

    private static string CalculateRecordHash(AuditRecord auditRecord)
    {
        // Create a deterministic string representation of the audit record
        var recordString = $"{auditRecord.Id}|{auditRecord.EventType}|{auditRecord.EntityType}|" +
                          $"{auditRecord.EntityId}|{auditRecord.UserId}|{auditRecord.Timestamp:O}|" +
                          $"{auditRecord.ChangeDetails}|{auditRecord.PreviousRecordHash}|{auditRecord.SequenceNumber}";

        // Calculate SHA-256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(recordString));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}