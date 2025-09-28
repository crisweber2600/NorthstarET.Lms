using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
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
        return await _context.Districts
            .Include(d => d.RetentionPolicy)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<DistrictTenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .Include(d => d.RetentionPolicy)
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
            .Include(d => d.RetentionPolicy)
            .OrderBy(d => d.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<DistrictTenant>> GetByStatusAsync(DistrictStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Districts
            .Where(d => d.Status == status)
            .Include(d => d.RetentionPolicy)
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
}