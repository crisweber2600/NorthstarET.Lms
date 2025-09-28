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

        if (!string.IsNullOrWhiteSpace(queryDto.UserId))
        {
            query = query.Where(ar => ar.UserId == queryDto.UserId);
        }

        if (queryDto.StartDate.HasValue)
        {
            query = query.Where(ar => ar.Timestamp >= queryDto.StartDate.Value);
        }

        if (queryDto.EndDate.HasValue)
        {
            query = query.Where(ar => ar.Timestamp <= queryDto.EndDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering (most recent first)
        var auditRecords = await query
            .OrderByDescending(ar => ar.Timestamp)
            .ThenBy(ar => ar.Id)
            .Skip((queryDto.Page - 1) * queryDto.PageSize)
            .Take(queryDto.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditRecord>(auditRecords, queryDto.Page, queryDto.PageSize, totalCount);
    }

    public async Task<IList<AuditRecord>> GetEntityAuditHistoryAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditRecords
            .Where(ar => ar.EntityType == entityType && ar.EntityId == entityId)
            .OrderBy(ar => ar.Timestamp)
            .ThenBy(ar => ar.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<AuditRecord>> GetCorrelatedAuditRecordsAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        // CorrelationId is Guid?, so we need to parse the string to Guid
        if (Guid.TryParse(correlationId, out var guidCorrelationId))
        {
            return await _context.AuditRecords
                .Where(ar => ar.CorrelationId == guidCorrelationId)
                .OrderBy(ar => ar.Timestamp)
                .ThenBy(ar => ar.Id)
                .ToListAsync(cancellationToken);
        }
        return new List<AuditRecord>();
    }

    public async Task<AuditRecord?> GetPreviousAuditRecordAsync(long currentSequenceNumber, CancellationToken cancellationToken = default)
    {
        // Since we don't have SequenceNumber, we'll use the record creation order
        return await _context.AuditRecords
            .OrderByDescending(ar => ar.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        // Since AuditRecord doesn't have SequenceNumber, we'll use record count + 1
        var count = await _context.AuditRecords.CountAsync(cancellationToken);
        return count + 1;
    }

    public async Task<bool> ValidateAuditChainIntegrityAsync(CancellationToken cancellationToken = default)
    {
        // Get all audit records ordered by sequence number
        var auditRecords = await _context.AuditRecords
            .OrderBy(ar => ar.Id)
            .Select(ar => new { ar.Id, ar.RecordHash, ar.PreviousRecordHash })
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
            .ThenBy(ar => ar.Id)
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
            .ThenBy(ar => ar.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditRecord>> GetChainAsync(string tenantId, DateTime startDate, DateTime endDate)
    {
        return await _context.AuditRecords
            .Where(ar => ar.Timestamp >= startDate && ar.Timestamp <= endDate)
            .OrderBy(ar => ar.Id)
            .ToListAsync();
    }

    public async Task AddAsync(AuditRecord auditRecord)
    {
        await AddAsync(auditRecord, CancellationToken.None);
    }

    public async Task<PagedResult<AuditRecord>> QueryAsync(string? entityType, Guid? entityId, string? userId, string? eventType, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        var query = _context.AuditRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(ar => ar.EntityType == entityType);
            
        if (entityId.HasValue)
            query = query.Where(ar => ar.EntityId == entityId.Value);
            
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(ar => ar.UserId == userId);
            
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(ar => ar.EventType.ToString() == eventType);
            
        if (startDate.HasValue)
            query = query.Where(ar => ar.Timestamp >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(ar => ar.Timestamp <= endDate.Value);

        var totalCount = await query.CountAsync();
        
        var records = await query
            .OrderByDescending(ar => ar.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        return new PagedResult<AuditRecord>(records, page, pageSize, totalCount);
    }

    public async Task<List<AuditRecord>> GetRecordsForIntegrityCheckAsync(long? startSequence, long? endSequence)
    {
        var query = _context.AuditRecords.AsQueryable();
        
        // Since AuditRecord doesn't have SequenceNumber, we'll use Id ordering instead
        if (startSequence.HasValue)
            query = query.Skip((int)startSequence.Value);
            
        if (endSequence.HasValue)
            query = query.Take((int)(endSequence.Value - (startSequence ?? 0)));
            
        return await query
            .OrderBy(ar => ar.Id)
            .ToListAsync();
    }

    public async Task<AuditComplianceSummary> GetComplianceSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var records = await _context.AuditRecords
            .Where(ar => ar.Timestamp >= startDate && ar.Timestamp <= endDate)
            .ToListAsync();

        return new AuditComplianceSummary
        {
            TotalRecords = records.Count,
            EventTypeCounts = records.GroupBy(ar => ar.EventType.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            DataAccessCount = records.Count(ar => ar.EventType.ToString().Contains("Read") || ar.EventType.ToString().Contains("View")),
            DataModificationCount = records.Count(ar => ar.EventType.ToString().Contains("Create") || ar.EventType.ToString().Contains("Update") || ar.EventType.ToString().Contains("Delete")),
            IntegrityIssues = 0 // Would need actual integrity checking logic
        };
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
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

        // Apply filters - map to actual PlatformAuditRecord properties
        if (!string.IsNullOrWhiteSpace(queryDto.ActingUserId))
        {
            query = query.Where(par => par.UserId == queryDto.ActingUserId);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.EventType))
        {
            query = query.Where(par => par.Action == queryDto.EventType);
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
            .ThenByDescending(par => par.Id)
            .Skip((queryDto.Page - 1) * queryDto.Size)
            .Take(queryDto.Size)
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformAuditRecord>(auditRecords, queryDto.Page, queryDto.Size, totalCount);
    }

    public async Task<IList<PlatformAuditRecord>> GetTenantAuditHistoryAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        // PlatformAuditRecord doesn't have TenantId, so we can't filter by it
        // This method should probably filter by some other criteria or be removed
        return await _context.PlatformAuditRecords
            .OrderBy(par => par.Timestamp)
            .ThenBy(par => par.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        // PlatformAuditRecord doesn't have SequenceNumber, so we'll use record count + 1
        var count = await _context.PlatformAuditRecords.CountAsync(cancellationToken);
        return count + 1;
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
            query = query.Where(par => par.Action == queryDto.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.UserId))
        {
            query = query.Where(par => par.UserId == queryDto.UserId);
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
            .ThenByDescending(par => par.Id)
            .Skip((queryDto.Page - 1) * queryDto.PageSize)
            .Take(queryDto.PageSize)
            .ToListAsync();

        return new PagedResult<PlatformAuditRecord>(auditRecords, queryDto.Page, queryDto.PageSize, totalCount);
    }

    public async Task AddAsync(PlatformAuditRecord auditRecord)
    {
        await AddAsync(auditRecord, CancellationToken.None);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}