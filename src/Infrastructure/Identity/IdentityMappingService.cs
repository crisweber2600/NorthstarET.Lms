using Microsoft.Graph;
using Microsoft.Graph.Models;
using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Persistence;

namespace NorthstarET.Lms.Infrastructure.Identity;

public class IdentityMappingService : IIdentityMappingService
{
    private readonly GraphServiceClient? _graphClient;
    private readonly LmsDbContext _context;

    public IdentityMappingService(LmsDbContext context, GraphServiceClient? graphClient = null)
    {
        _context = context;
        _graphClient = graphClient;
    }

    public async Task<IdentityMapping> CreateMappingAsync(
        string issuer,
        string externalUserId,
        Guid internalUserId,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var mapping = new IdentityMapping(issuer, externalUserId, internalUserId, createdBy);
        mapping.SetAuditFields(createdBy, DateTimeOffset.UtcNow);

        await _context.IdentityMappings.AddAsync(mapping, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return mapping;
    }

    public async Task<IdentityMapping?> GetMappingByExternalIdAsync(
        string issuer,
        string externalUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.IdentityMappings
            .Where(m => m.Issuer == issuer && m.ExternalUserId == externalUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> ResolveInternalUserIdAsync(
        string issuer,
        string externalUserId,
        CancellationToken cancellationToken = default)
    {
        var mapping = await GetMappingByExternalIdAsync(issuer, externalUserId, cancellationToken);
        return mapping?.InternalUserId;
    }

    public async Task SuspendMappingAsync(
        Guid mappingId,
        DateTime suspendedUntil,
        string reason,
        string suspendedBy,
        CancellationToken cancellationToken = default)
    {
        var mapping = await _context.IdentityMappings.FindAsync([mappingId], cancellationToken);
        if (mapping != null)
        {
            mapping.Suspend(suspendedUntil, reason, suspendedBy);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
