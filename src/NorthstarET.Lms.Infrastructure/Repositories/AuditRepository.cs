using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data;

namespace NorthstarET.Lms.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly LmsDbContext _context;

    public AuditRepository(LmsDbContext context)
    {
        _context = context;
    }

    public async Task<AuditRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditRecords
            .FirstOrDefaultAsync(ar => ar.Id == id, cancellationToken);
    }

    public async Task<PagedResult<AuditRecord>> QueryAuditRecordsAsync(AuditQueryDto queryDto, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditRecords.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(queryDto.EntityType))
        {
            query = query.Where(ar => ar.EntityType == queryDto.EntityType);
        }

        if (queryDto.EntityId.HasValue)
        {
            query = query.Where(ar => ar.EntityId == queryDto.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.UserId))
        {
            query = query.Where(ar => ar.UserId == queryDto.UserId);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.EventType))
        {
            query = query.Where(ar => ar.EventType == queryDto.EventType);
        }

        if (queryDto.FromDate.HasValue)
        {
            query = query.Where(ar => ar.Timestamp >= queryDto.FromDate.Value);
        }

        if (queryDto.ToDate.HasValue)
        {
            query = query.Where(ar => ar.Timestamp <= queryDto.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.CorrelationId))
        {
            query = query.Where(ar => ar.CorrelationId == queryDto.CorrelationId);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering (most recent first)
        var auditRecords = await query
            .OrderByDescending(ar => ar.Timestamp)
            .ThenByDescending(ar => ar.SequenceNumber)
            .Skip((queryDto.Page - 1) * queryDto.Size)
            .Take(queryDto.Size)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditRecord>(auditRecords, queryDto.Page, queryDto.Size, totalCount);
    }

    public async Task<IList<AuditRecord>> GetEntityAuditHistoryAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditRecords
            .Where(ar => ar.EntityType == entityType && ar.EntityId == entityId)
            .OrderBy(ar => ar.Timestamp)
            .ThenBy(ar => ar.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<AuditRecord>> GetCorrelatedAuditRecordsAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditRecords
            .Where(ar => ar.CorrelationId == correlationId)
            .OrderBy(ar => ar.Timestamp)
            .ThenBy(ar => ar.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditRecord?> GetPreviousAuditRecordAsync(long currentSequenceNumber, CancellationToken cancellationToken = default)
    {
        return await _context.AuditRecords
            .Where(ar => ar.SequenceNumber < currentSequenceNumber)
            .OrderByDescending(ar => ar.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        var lastRecord = await _context.AuditRecords
            .OrderByDescending(ar => ar.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return (lastRecord?.SequenceNumber ?? 0) + 1;
    }

    public async Task<bool> ValidateAuditChainIntegrityAsync(CancellationToken cancellationToken = default)
    {
        // Get all audit records ordered by sequence number
        var auditRecords = await _context.AuditRecords
            .OrderBy(ar => ar.SequenceNumber)
            .Select(ar => new { ar.SequenceNumber, ar.RecordHash, ar.PreviousRecordHash })
            .ToListAsync(cancellationToken);

        if (!auditRecords.Any())
        {
            return true; // Empty chain is valid
        }

        // Check chain integrity
        for (int i = 1; i < auditRecords.Count; i++)
        {
            var current = auditRecords[i];
            var previous = auditRecords[i - 1];

            if (current.PreviousRecordHash != previous.RecordHash)
            {
                return false; // Chain is broken
            }
        }

        return true; // Chain is intact
    }

    public async Task<int> CountAuditRecordsByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.AuditRecords
            .CountAsync(ar => ar.Timestamp >= fromDate && ar.Timestamp <= toDate, cancellationToken);
    }

    public async Task<IList<AuditRecord>> GetAuditRecordsForExportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.AuditRecords
            .Where(ar => ar.Timestamp >= fromDate && ar.Timestamp <= toDate)
            .OrderBy(ar => ar.Timestamp)
            .ThenBy(ar => ar.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AuditRecord auditRecord, CancellationToken cancellationToken = default)
    {
        await _context.AuditRecords.AddAsync(auditRecord, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<AuditRecord> auditRecords, CancellationToken cancellationToken = default)
    {
        await _context.AuditRecords.AddRangeAsync(auditRecords, cancellationToken);
    }

    // Interface methods without CancellationToken
    public async Task<AuditRecord?> GetByIdAsync(Guid id)
    {
        return await GetByIdAsync(id, CancellationToken.None);
    }

    public async Task<PagedResult<AuditRecord>> QueryAsync(AuditQueryDto queryDto)
    {
        return await QueryAuditRecordsAsync(queryDto, CancellationToken.None);
    }

    public async Task<IEnumerable<AuditRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, string[] entityTypes)
    {
        var query = _context.AuditRecords
            .Where(ar => ar.Timestamp >= startDate && ar.Timestamp <= endDate);

        if (entityTypes?.Length > 0)
        {
            query = query.Where(ar => entityTypes.Contains(ar.EntityType));
        }

        return await query
            .OrderBy(ar => ar.Timestamp)
            .ThenBy(ar => ar.SequenceNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditRecord>> GetChainAsync(string tenantId, DateTime startDate, DateTime endDate)
    {
        return await _context.AuditRecords
            .Where(ar => ar.Timestamp >= startDate && ar.Timestamp <= endDate)
            .OrderBy(ar => ar.SequenceNumber)
            .ToListAsync();
    }

    public async Task AddAsync(AuditRecord auditRecord)
    {
        await AddAsync(auditRecord, CancellationToken.None);
    }

    // Note: Audit records are typically immutable, so Update and Remove are not implemented
}

public class PlatformAuditRepository : IPlatformAuditRepository
{
    private readonly LmsDbContext _context;

    public PlatformAuditRepository(LmsDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformAuditRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PlatformAuditRecords
            .FirstOrDefaultAsync(par => par.Id == id, cancellationToken);
    }

    public async Task<PagedResult<PlatformAuditRecord>> QueryPlatformAuditRecordsAsync(PlatformAuditQueryDto queryDto, CancellationToken cancellationToken = default)
    {
        var query = _context.PlatformAuditRecords.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(queryDto.TenantId))
        {
            query = query.Where(par => par.TenantId == queryDto.TenantId);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.ActingUserId))
        {
            query = query.Where(par => par.ActingUserId == queryDto.ActingUserId);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.EventType))
        {
            query = query.Where(par => par.EventType == queryDto.EventType);
        }

        if (queryDto.FromDate.HasValue)
        {
            query = query.Where(par => par.Timestamp >= queryDto.FromDate.Value);
        }

        if (queryDto.ToDate.HasValue)
        {
            query = query.Where(par => par.Timestamp <= queryDto.ToDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering (most recent first)
        var auditRecords = await query
            .OrderByDescending(par => par.Timestamp)
            .ThenByDescending(par => par.SequenceNumber)
            .Skip((queryDto.Page - 1) * queryDto.Size)
            .Take(queryDto.Size)
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformAuditRecord>(auditRecords, queryDto.Page, queryDto.Size, totalCount);
    }

    public async Task<IList<PlatformAuditRecord>> GetTenantAuditHistoryAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.PlatformAuditRecords
            .Where(par => par.TenantId == tenantId)
            .OrderBy(par => par.Timestamp)
            .ThenBy(par => par.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        var lastRecord = await _context.PlatformAuditRecords
            .OrderByDescending(par => par.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return (lastRecord?.SequenceNumber ?? 0) + 1;
    }

    public async Task AddAsync(PlatformAuditRecord auditRecord, CancellationToken cancellationToken = default)
    {
        await _context.PlatformAuditRecords.AddAsync(auditRecord, cancellationToken);
    }

    // Interface methods without CancellationToken
    public async Task<PlatformAuditRecord?> GetByIdAsync(Guid id)
    {
        return await GetByIdAsync(id, CancellationToken.None);
    }

    public async Task<PagedResult<PlatformAuditRecord>> QueryAsync(AuditQueryDto queryDto)
    {
        var query = _context.PlatformAuditRecords.AsQueryable();

        // Apply filters - convert AuditQueryDto to platform audit query
        if (!string.IsNullOrWhiteSpace(queryDto.EntityType))
        {
            query = query.Where(par => par.EventType == queryDto.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.UserId))
        {
            query = query.Where(par => par.ActingUserId == queryDto.UserId);
        }

        if (queryDto.StartDate.HasValue)
        {
            query = query.Where(par => par.Timestamp >= queryDto.StartDate.Value);
        }

        if (queryDto.EndDate.HasValue)
        {
            query = query.Where(par => par.Timestamp <= queryDto.EndDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering (most recent first)
        var auditRecords = await query
            .OrderByDescending(par => par.Timestamp)
            .ThenByDescending(par => par.SequenceNumber)
            .Skip((queryDto.Page - 1) * queryDto.PageSize)
            .Take(queryDto.PageSize)
            .ToListAsync();

        return new PagedResult<PlatformAuditRecord>(auditRecords, queryDto.Page, queryDto.PageSize, totalCount);
    }

    public async Task AddAsync(PlatformAuditRecord auditRecord)
    {
        await AddAsync(auditRecord, CancellationToken.None);
    }
}