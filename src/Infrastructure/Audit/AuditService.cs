using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Persistence;

namespace NorthstarET.Lms.Infrastructure.Audit;

public class AuditService : IAuditService
{
    private readonly LmsDbContext _context;

    public AuditService(LmsDbContext context)
    {
        _context = context;
    }

    public async Task<AuditRecord> CreateAuditRecordAsync(
        string entityType,
        Guid entityId,
        string action,
        string? entitySnapshot,
        string actor,
        CancellationToken cancellationToken = default)
    {
        // Get previous audit record for hash chaining
        var previousRecord = await _context.AuditRecords
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var previousHash = previousRecord?.RecordHash ?? string.Empty;

        // Create new audit record
        var auditRecord = new AuditRecord(
            entityType,
            entityId,
            action,
            actor,
            entitySnapshot,
            previousHash
        );

        auditRecord.SetAuditFields(actor, DateTimeOffset.UtcNow);

        await _context.AuditRecords.AddAsync(auditRecord, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return auditRecord;
    }

    public async Task<bool> VerifyAuditChainIntegrityAsync(
        string tenantSlug,
        CancellationToken cancellationToken = default)
    {
        var records = await _context.AuditRecords
            .Where(a => a.TenantSlug == tenantSlug)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
        {
            return true;
        }

        // Verify first record
        var firstRecord = records[0];
        if (!string.IsNullOrEmpty(firstRecord.PreviousRecordHash))
        {
            return false;
        }

        // Verify hash chain
        for (int i = 1; i < records.Count; i++)
        {
            var currentRecord = records[i];
            var previousRecord = records[i - 1];

            if (currentRecord.PreviousRecordHash != previousRecord.RecordHash)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<List<AuditRecord>> QueryAuditRecordsAsync(
        string? entityType,
        Guid? entityId,
        string? actor,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditRecords.AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (entityId.HasValue)
        {
            query = query.Where(a => a.EntityId == entityId.Value);
        }

        if (!string.IsNullOrEmpty(actor))
        {
            query = query.Where(a => a.PerformedBy == actor);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= toDate.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
