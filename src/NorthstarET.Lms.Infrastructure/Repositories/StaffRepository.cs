using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Infrastructure.Data;

namespace NorthstarET.Lms.Infrastructure.Repositories;

public class StaffRepository : IStaffRepository
{
    private readonly LmsDbContext _context;

    public StaffRepository(LmsDbContext context)
    {
        _context = context;
    }

    public async Task<Staff?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .Include(s => s.RoleAssignments)
                
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<Staff?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .Include(s => s.RoleAssignments)
                
            .FirstOrDefaultAsync(s => s.EmployeeNumber == employeeNumber, cancellationToken);
    }

    public async Task<Staff?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .Include(s => s.RoleAssignments)
                
            .FirstOrDefaultAsync(s => s.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> EmployeeNumberExistsAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .AnyAsync(s => s.EmployeeNumber == employeeNumber, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .AnyAsync(s => s.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<PagedResult<Staff>> SearchStaffAsync(StaffSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        var query = _context.Staff.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLowerInvariant();
            query = query.Where(s => 
                s.FirstName.ToLower().Contains(searchTerm) ||
                s.LastName.ToLower().Contains(searchTerm) ||
                s.EmployeeNumber.ToLower().Contains(searchTerm) ||
                s.Email.ToLower().Contains(searchTerm));
        }

        if (searchDto.Status.HasValue)
        {
            query = query.Where(s => s.Status == searchDto.Status.Value);
        }

        if (searchDto.SchoolId.HasValue)
        {
            query = query.Where(s => s.RoleAssignments
                .Any(ra => ra.SchoolId == searchDto.SchoolId.Value && ra.IsActive));
        }

        if (!string.IsNullOrWhiteSpace(searchDto.RoleName))
        {
            query = query.Where(s => s.RoleAssignments
                .Any(ra => ra.IsActive)); // TODO: Add role name filtering when RoleDefinition navigation is available
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var staff = await query
            .Include(s => s.RoleAssignments.Where(ra => ra.IsActive))
                
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Skip((searchDto.Page - 1) * searchDto.Size)
            .Take(searchDto.Size)
            .ToListAsync(cancellationToken);

        return new PagedResult<Staff>(staff, searchDto.Page, searchDto.Size, totalCount);
    }

    public async Task<IList<Staff>> GetStaffByStatusAsync(UserLifecycleStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .Where(s => s.Status == status)
            .Include(s => s.RoleAssignments.Where(ra => ra.IsActive))
                
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<Staff>> GetStaffInSchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .Where(s => s.Status == UserLifecycleStatus.Active)
            .Where(s => s.RoleAssignments.Any(ra => 
                ra.SchoolId == schoolId && 
                ra.IsActive &&
                (ra.ExpirationDate == null || ra.ExpirationDate > DateTime.UtcNow)))
            .Include(s => s.RoleAssignments.Where(ra => ra.SchoolId == schoolId && ra.IsActive))
                
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<Staff>> GetStaffWithRoleAsync(string roleName, Guid? schoolId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Staff
            .Where(s => s.Status == UserLifecycleStatus.Active)
            .Where(s => s.RoleAssignments.Any(ra => 
                ra.IsActive &&
                ra.IsActive &&
                (ra.ExpirationDate == null || ra.ExpirationDate > DateTime.UtcNow)));

        if (schoolId.HasValue)
        {
            query = query.Where(s => s.RoleAssignments.Any(ra => 
                ra.IsActive &&
                ra.SchoolId == schoolId.Value &&
                ra.IsActive));
        }

        return await query
            .Include(s => s.RoleAssignments.Where(ra => 
                ra.IsActive && ra.IsActive))
                
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveStaffAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .CountAsync(s => s.Status == UserLifecycleStatus.Active, cancellationToken);
    }

    public async Task<int> CountStaffInSchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .Where(s => s.Status == UserLifecycleStatus.Active)
            .CountAsync(s => s.RoleAssignments.Any(ra => 
                ra.SchoolId == schoolId && 
                ra.IsActive &&
                (ra.ExpirationDate == null || ra.ExpirationDate > DateTime.UtcNow)), 
                cancellationToken);
    }

    public async Task<IList<Staff>> GetStaffHiredInDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .Where(s => s.HireDate >= fromDate.Date && s.HireDate <= toDate.Date)
            .OrderBy(s => s.HireDate)
            .ThenBy(s => s.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<Staff>> GetStaffWithExpiringRolesAsync(DateTime expirationDate, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .Where(s => s.RoleAssignments.Any(ra => 
                ra.ExpirationDate.HasValue && 
                ra.ExpirationDate.Value <= expirationDate &&
                ra.IsActive))
            .Include(s => s.RoleAssignments.Where(ra => 
                ra.ExpirationDate.HasValue && 
                ra.ExpirationDate.Value <= expirationDate &&
                ra.IsActive))
                
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Staff staff, CancellationToken cancellationToken = default)
    {
        await _context.Staff.AddAsync(staff, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Staff> staff, CancellationToken cancellationToken = default)
    {
        await _context.Staff.AddRangeAsync(staff, cancellationToken);
    }

    public void Update(Staff staff)
    {
        _context.Staff.Update(staff);
    }

    public void Remove(Staff staff)
    {
        _context.Staff.Remove(staff);
    }

    // Interface methods without CancellationToken
    public async Task<Staff?> GetByIdAsync(Guid id)
    {
        return await GetByIdAsync(id, CancellationToken.None);
    }

    public async Task<IEnumerable<Staff>> GetBySchoolIdAsync(Guid schoolId)
    {
        return await GetStaffInSchoolAsync(schoolId, CancellationToken.None);
    }

    public async Task<IEnumerable<Staff>> GetAllAsync()
    {
        return await _context.Staff
            .Include(s => s.RoleAssignments.Where(ra => ra.IsActive))
                
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();
    }

    public async Task AddAsync(Staff staff)
    {
        await AddAsync(staff, CancellationToken.None);
    }

    public async Task UpdateAsync(Staff staff)
    {
        Update(staff);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}