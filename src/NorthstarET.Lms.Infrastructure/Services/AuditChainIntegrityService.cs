using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data;

namespace NorthstarET.Lms.Infrastructure.Services;

/// <summary>
/// Service for maintaining tamper-evident audit chain integrity (FR-048)
/// Implements cryptographic chaining to ensure audit log immutability
/// </summary>
public class AuditChainIntegrityService : IAuditChainIntegrityService
{
    private readonly LmsDbContext _context;
    private readonly ILogger<AuditChainIntegrityService> _logger;
    private readonly SemaphoreSlim _chainLock = new(1, 1);

    public AuditChainIntegrityService(
        LmsDbContext context,
        ILogger<AuditChainIntegrityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Adds an audit record to the tamper-evident chain
    /// </summary>
    public async Task<string> AddToChainAsync(AuditRecord auditRecord, CancellationToken cancellationToken = default)
    {
        await _chainLock.WaitAsync(cancellationToken);
        
        try
        {
            // Get the last record in the chain to link to
            var lastRecord = await _context.AuditRecords
                .OrderByDescending(r => r.SequenceNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var previousHash = lastRecord?.RecordHash ?? GetGenesisHash();
            var sequenceNumber = (lastRecord?.SequenceNumber ?? 0) + 1;

            // Set chain-specific properties
            auditRecord.SetSequenceNumber(sequenceNumber);
            auditRecord.SetPreviousRecordHash(previousHash);

            // Calculate and set the record hash
            var recordHash = ComputeRecordHash(auditRecord);
            auditRecord.SetRecordHash(recordHash);

            _logger.LogInformation("Added audit record to chain: Sequence={SequenceNumber}, Hash={RecordHash}", 
                sequenceNumber, recordHash[..8]);

            return recordHash;
        }
        finally
        {
            _chainLock.Release();
        }
    }

    /// <summary>
    /// Validates the integrity of the entire audit chain
    /// </summary>
    public async Task<ChainValidationResult> ValidateChainIntegrityAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting audit chain integrity validation");
        
        var records = await _context.AuditRecords
            .OrderBy(r => r.SequenceNumber)
            .Select(r => new { r.SequenceNumber, r.RecordHash, r.PreviousRecordHash, r.Id })
            .ToListAsync(cancellationToken);

        if (!records.Any())
        {
            return ChainValidationResult.Success();
        }

        var violations = new List<ChainViolation>();
        string? expectedPreviousHash = GetGenesisHash();

        foreach (var record in records)
        {
            // Check previous hash linkage
            if (record.PreviousRecordHash != expectedPreviousHash)
            {
                violations.Add(new ChainViolation
                {
                    SequenceNumber = record.SequenceNumber,
                    RecordId = record.Id,
                    ViolationType = ViolationType.BrokenLink,
                    Description = $"Previous hash mismatch. Expected: {expectedPreviousHash?[..8]}, Found: {record.PreviousRecordHash?[..8]}"
                });
            }

            // Verify record hash integrity by recalculating
            var actualRecord = await _context.AuditRecords
                .FirstAsync(r => r.Id == record.Id, cancellationToken);
            
            var calculatedHash = ComputeRecordHash(actualRecord);
            if (calculatedHash != record.RecordHash)
            {
                violations.Add(new ChainViolation
                {
                    SequenceNumber = record.SequenceNumber,
                    RecordId = record.Id,
                    ViolationType = ViolationType.TamperedRecord,
                    Description = $"Record hash mismatch. Expected: {record.RecordHash?[..8]}, Calculated: {calculatedHash[..8]}"
                });
            }

            expectedPreviousHash = record.RecordHash;
        }

        var result = violations.Any() 
            ? ChainValidationResult.Failed(violations)
            : ChainValidationResult.Success();

        _logger.LogInformation("Chain validation completed. Valid: {IsValid}, Violations: {ViolationCount}", 
            result.IsValid, violations.Count);

        return result;
    }

    /// <summary>
    /// Gets audit records within a specific sequence range for compliance export
    /// </summary>
    public async Task<IEnumerable<AuditRecord>> GetChainSegmentAsync(
        long fromSequence, 
        long toSequence, 
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditRecords
            .Where(r => r.SequenceNumber >= fromSequence && r.SequenceNumber <= toSequence)
            .OrderBy(r => r.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Exports the audit chain in a verifiable format for compliance
    /// </summary>
    public async Task<byte[]> ExportChainAsync(
        DateTime? fromDate = null, 
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditRecords.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(r => r.Timestamp >= fromDate.Value);
        
        if (toDate.HasValue)
            query = query.Where(r => r.Timestamp <= toDate.Value);

        var records = await query
            .OrderBy(r => r.SequenceNumber)
            .ToListAsync(cancellationToken);

        var exportData = new AuditChainExport
        {
            ExportedAt = DateTime.UtcNow,
            RecordCount = records.Count,
            FromSequence = records.FirstOrDefault()?.SequenceNumber ?? 0,
            ToSequence = records.LastOrDefault()?.SequenceNumber ?? 0,
            Records = records.Select(r => new AuditRecordExport
            {
                SequenceNumber = r.SequenceNumber,
                Timestamp = r.Timestamp,
                RecordHash = r.RecordHash ?? string.Empty,
                PreviousRecordHash = r.PreviousRecordHash ?? string.Empty,
                EntityType = r.EntityType,
                EntityId = r.EntityId.ToString(),
                Action = r.Action,
                ActingUserId = r.UserId
            }).ToList()
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    private string ComputeRecordHash(AuditRecord record)
    {
        // Create a canonical representation of the record for hashing
        var hashInput = new
        {
            record.SequenceNumber,
            Timestamp = record.Timestamp.ToString("O"), // ISO 8601 format
            record.EntityType,
            record.EntityId,
            record.Action,
            UserId = record.UserId, // Use UserId instead of ActingUserId
            record.PreviousRecordHash,
            // Include audit data in hash but handle null/empty cases
            AuditData = string.IsNullOrEmpty(record.Details) ? "" : record.Details
        };

        var json = JsonSerializer.Serialize(hashInput, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        });

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash);
    }

    private string GetGenesisHash()
    {
        // Genesis hash for the first record in the chain
        const string genesisData = "NorthstarET.Lms.AuditChain.Genesis";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(genesisData));
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        _chainLock?.Dispose();
    }
}

/// <summary>
/// Interface for audit chain integrity service
/// </summary>
public interface IAuditChainIntegrityService : IDisposable
{
    Task<string> AddToChainAsync(AuditRecord auditRecord, CancellationToken cancellationToken = default);
    Task<ChainValidationResult> ValidateChainIntegrityAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditRecord>> GetChainSegmentAsync(long fromSequence, long toSequence, CancellationToken cancellationToken = default);
    Task<byte[]> ExportChainAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of chain validation operation
/// </summary>
public class ChainValidationResult
{
    public bool IsValid { get; set; }
    public List<ChainViolation> Violations { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    public static ChainValidationResult Success() => new() { IsValid = true };
    public static ChainValidationResult Failed(List<ChainViolation> violations) => 
        new() { IsValid = false, Violations = violations };
}

/// <summary>
/// Represents a violation in the audit chain
/// </summary>
public class ChainViolation
{
    public long SequenceNumber { get; set; }
    public Guid RecordId { get; set; }
    public ViolationType ViolationType { get; set; }
    public string Description { get; set; } = "";
}

/// <summary>
/// Types of chain violations
/// </summary>
public enum ViolationType
{
    BrokenLink,
    TamperedRecord,
    MissingRecord,
    DuplicateSequence
}

/// <summary>
/// Export format for audit chain compliance
/// </summary>
public class AuditChainExport
{
    public DateTime ExportedAt { get; set; }
    public int RecordCount { get; set; }
    public long FromSequence { get; set; }
    public long ToSequence { get; set; }
    public List<AuditRecordExport> Records { get; set; } = new();
}

/// <summary>
/// Export format for individual audit records
/// </summary>
public class AuditRecordExport
{
    public long SequenceNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public string RecordHash { get; set; } = "";
    public string? PreviousRecordHash { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public string ActingUserId { get; set; } = "";
}