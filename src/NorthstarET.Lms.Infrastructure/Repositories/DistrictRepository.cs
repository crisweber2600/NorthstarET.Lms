using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Infrastructure.Data;

namespace NorthstarET.Lms.Infrastructure.Repositories;

public class DistrictRepository : IDistrictRepository
{
    private readonly LmsDbContext _context;

    public DistrictRepository(LmsDbContext context)
    {
        _context = context;
    }

    public async Task<DistrictTenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // DistrictTenant doesn't have RetentionPolicy navigation property
        return await _context.Districts
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<DistrictTenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .FirstOrDefaultAsync(d => d.Slug == slug.ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .AnyAsync(d => d.Slug == slug.ToLowerInvariant(), cancellationToken);
    }

    public async Task<IList<DistrictTenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .OrderBy(d => d.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<DistrictTenant>> GetByStatusAsync(DistrictStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .Where(d => d.Status == status)
            .OrderBy(d => d.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveDistrictsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .CountAsync(d => d.Status == DistrictStatus.Active, cancellationToken);
    }

    public async Task<bool> IsWithinQuotaAsync(Guid districtId, string entityType, CancellationToken cancellationToken = default)
    {
        var district = await GetByIdAsync(districtId, cancellationToken);
        if (district == null) return false;

        var currentCount = entityType switch
        {
            "Student" => await _context.Students.CountAsync(s => s.Status == UserLifecycleStatus.Active, cancellationToken),
            "Staff" => await _context.Staff.CountAsync(s => s.Status == UserLifecycleStatus.Active, cancellationToken),
            _ => 0
        };

        var quota = entityType switch
        {
            "Student" => district.Quotas.MaxStudents,
            "Staff" => district.Quotas.MaxStaff,
            _ => int.MaxValue
        };

        return currentCount < quota;
    }

    public async Task AddAsync(DistrictTenant district, CancellationToken cancellationToken = default)
    {
        await _context.Districts.AddAsync(district, cancellationToken);
    }

    public void Update(DistrictTenant district)
    {
        _context.Districts.Update(district);
    }

    public void Remove(DistrictTenant district)
    {
        _context.Districts.Remove(district);
    }

    // Interface methods without CancellationToken
    public async Task<DistrictTenant?> GetByIdAsync(Guid id)
    {
        return await GetByIdAsync(id, CancellationToken.None);
    }

    public async Task<DistrictTenant?> GetBySlugAsync(string slug)
    {
        return await GetBySlugAsync(slug, CancellationToken.None);
    }

    public async Task<IEnumerable<DistrictTenant>> GetAllAsync()
    {
        return await GetAllAsync(CancellationToken.None);
    }

    public async Task AddAsync(DistrictTenant district)
    {
        await AddAsync(district, CancellationToken.None);
    }

    public async Task UpdateAsync(DistrictTenant district)
    {
        Update(district);
        await Task.CompletedTask;
    }

    public async Task<bool> HasActiveRetentionPoliciesAsync(Guid districtId)
    {
        // Since DistrictTenant doesn't have RetentionPolicy navigation,
        // we'll check the RetentionPolicies table directly
        return await _context.RetentionPolicies
            .AnyAsync(rp => rp.IsDefault && rp.SupersededDate == null);
    }

    public async Task<bool> SlugExistsAsync(string slug)
    {
        return await SlugExistsAsync(slug, CancellationToken.None);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}