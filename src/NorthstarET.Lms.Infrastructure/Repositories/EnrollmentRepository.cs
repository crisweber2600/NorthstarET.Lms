using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Infrastructure.Data;

namespace NorthstarET.Lms.Infrastructure.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly LmsDbContext _context;

    public EnrollmentRepository(LmsDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Class)
                .ThenInclude(c => c.School)
            .Include(e => e.SchoolYear)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // Interface method without cancellation token
    public async Task<Enrollment?> GetByIdAsync(Guid id)
    {
        return await GetByIdAsync(id, CancellationToken.None);
    }

    public async Task<Enrollment?> GetActiveEnrollmentAsync(Guid studentId, Guid classId, Guid schoolYearId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Class)
            .Include(e => e.SchoolYear)
            .FirstOrDefaultAsync(e => 
                e.StudentId == studentId && 
                e.ClassId == classId && 
                e.SchoolYearId == schoolYearId &&
                e.Status == EnrollmentStatus.Active, 
                cancellationToken);
    }

    // Interface method without cancellation token
    public async Task<Enrollment?> GetActiveEnrollmentAsync(Guid studentId, Guid classId, Guid schoolYearId)
    {
        return await GetActiveEnrollmentAsync(studentId, classId, schoolYearId, CancellationToken.None);
    }

    public async Task<bool> EnrollmentExistsAsync(Guid studentId, Guid classId, Guid schoolYearId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .AnyAsync(e => 
                e.StudentId == studentId && 
                e.ClassId == classId && 
                e.SchoolYearId == schoolYearId, 
                cancellationToken);
    }

    public async Task<IList<Enrollment>> GetStudentEnrollmentsAsync(Guid studentId, Guid schoolYearId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId && e.SchoolYearId == schoolYearId)
            .Include(e => e.Class)
                .ThenInclude(c => c.School)
            .Include(e => e.SchoolYear)
            .OrderBy(e => e.Class.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<Enrollment>> GetClassRosterAsync(Guid classId, Guid schoolYearId, EnrollmentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Enrollments
            .Where(e => e.ClassId == classId && e.SchoolYearId == schoolYearId);

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        return await query
            .Include(e => e.Student)
            .Include(e => e.Class)
            .Include(e => e.SchoolYear)
            .OrderBy(e => e.Student.LastName)
            .ThenBy(e => e.Student.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Enrollment>> SearchEnrollmentsAsync(EnrollmentSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        var query = _context.Enrollments.AsQueryable();

        // Apply filters
        if (searchDto.StudentId.HasValue)
        {
            query = query.Where(e => e.StudentId == searchDto.StudentId.Value);
        }

        if (searchDto.ClassId.HasValue)
        {
            query = query.Where(e => e.ClassId == searchDto.ClassId.Value);
        }

        if (searchDto.SchoolYearId.HasValue)
        {
            query = query.Where(e => e.SchoolYearId == searchDto.SchoolYearId.Value);
        }

        if (searchDto.Status.HasValue)
        {
            query = query.Where(e => e.Status == searchDto.Status.Value);
        }

        if (searchDto.GradeLevel.HasValue)
        {
            query = query.Where(e => e.GradeLevel == searchDto.GradeLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.StudentSearchTerm))
        {
            var searchTerm = searchDto.StudentSearchTerm.ToLowerInvariant();
            query = query.Where(e => 
                e.Student.FirstName.ToLower().Contains(searchTerm) ||
                e.Student.LastName.ToLower().Contains(searchTerm) ||
                e.Student.StudentNumber.ToLower().Contains(searchTerm));
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var enrollments = await query
            .Include(e => e.Student)
            .Include(e => e.Class)
                .ThenInclude(c => c.School)
            .Include(e => e.SchoolYear)
            .OrderBy(e => e.Student.LastName)
            .ThenBy(e => e.Student.FirstName)
            .Skip((searchDto.Page - 1) * searchDto.Size)
            .Take(searchDto.Size)
            .ToListAsync(cancellationToken);

        return new PagedResult<Enrollment>(enrollments, searchDto.Page, searchDto.Size, totalCount);
    }

    public async Task<IList<Enrollment>> GetEnrollmentsForBulkRolloverAsync(GradeLevel fromGrade, Guid fromSchoolYearId, Guid toSchoolYearId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Where(e => 
                e.GradeLevel == fromGrade && 
                e.SchoolYearId == fromSchoolYearId && 
                e.Status == EnrollmentStatus.Active)
            .Include(e => e.Student)
            .Include(e => e.Class)
                .ThenInclude(c => c.School)
            .Where(e => 
                // Ensure target school year has corresponding classes
                _context.Classes.Any(c => 
                    c.SchoolId == e.Class.SchoolId && 
                    c.SchoolYearId == toSchoolYearId))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountEnrollmentsByStatusAsync(EnrollmentStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .CountAsync(e => e.Status == status, cancellationToken);
    }

    public async Task<int> CountEnrollmentsInClassAsync(Guid classId, Guid schoolYearId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .CountAsync(e => 
                e.ClassId == classId && 
                e.SchoolYearId == schoolYearId && 
                e.Status == EnrollmentStatus.Active, 
                cancellationToken);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        await _context.Enrollments.AddAsync(enrollment, cancellationToken);
    }

    // Interface method without CancellationToken
    public async Task AddAsync(Enrollment enrollment)
    {
        await AddAsync(enrollment, CancellationToken.None);
    }

    public async Task AddRangeAsync(IEnumerable<Enrollment> enrollments, CancellationToken cancellationToken = default)
    {
        await _context.Enrollments.AddRangeAsync(enrollments, cancellationToken);
    }

    public void Update(Enrollment enrollment)
    {
        _context.Enrollments.Update(enrollment);
    }

    public void UpdateRange(IEnumerable<Enrollment> enrollments)
    {
        _context.Enrollments.UpdateRange(enrollments);
    }

    public void Remove(Enrollment enrollment)
    {
        _context.Enrollments.Remove(enrollment);
    }

    // Interface methods
    public async Task<IEnumerable<Enrollment>> GetByClassAndSchoolYearAsync(Guid classId, Guid schoolYearId)
    {
        return await GetClassRosterAsync(classId, schoolYearId, null, CancellationToken.None);
    }

    public async Task<IEnumerable<Enrollment>> GetBySchoolYearAsync(Guid schoolYearId)
    {
        return await _context.Enrollments
            .Where(e => e.SchoolYearId == schoolYearId)
            .Include(e => e.Student)
            .Include(e => e.Class)
            .Include(e => e.SchoolYear)
            .ToListAsync();
    }

    public async Task<IEnumerable<Enrollment>> GetByStudentIdAsync(Guid studentId)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Class)
            .Include(e => e.SchoolYear)
            .OrderBy(e => e.EnrollmentDate)
            .ToListAsync();
    }

    public async Task UpdateAsync(Enrollment enrollment)
    {
        Update(enrollment);
        await Task.CompletedTask;
    }

    public async Task<Enrollment?> GetActiveEnrollmentAsync(Guid studentId, Guid classId)
    {
        return await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Class)
            .Include(e => e.SchoolYear)
            .FirstOrDefaultAsync(e => 
                e.StudentId == studentId && 
                e.ClassId == classId && 
                e.Status == EnrollmentStatus.Active);
    }

    public async Task<IEnumerable<Enrollment>> GetActiveEnrollmentsByStudentAndSchoolAsync(Guid studentId, Guid schoolId)
    {
        return await _context.Enrollments
            .Where(e => 
                e.StudentId == studentId && 
                e.Class.SchoolId == schoolId && 
                e.Status == EnrollmentStatus.Active)
            .Include(e => e.Student)
            .Include(e => e.Class)
            .Include(e => e.SchoolYear)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}