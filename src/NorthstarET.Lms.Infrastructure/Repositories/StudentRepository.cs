using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Infrastructure.Data;

namespace NorthstarET.Lms.Infrastructure.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly LmsDbContext _context;

    public StudentRepository(LmsDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<Student?> GetByStudentNumberAsync(string studentNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber, cancellationToken);
    }

    public async Task<bool> StudentNumberExistsAsync(string studentNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .AnyAsync(s => s.StudentNumber == studentNumber, cancellationToken);
    }

    public async Task<PagedResult<Student>> SearchStudentsAsync(StudentSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        var query = _context.Students.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLowerInvariant();
            query = query.Where(s => 
                s.FirstName.ToLower().Contains(searchTerm) ||
                s.LastName.ToLower().Contains(searchTerm) ||
                s.StudentNumber.ToLower().Contains(searchTerm));
        }

        if (searchDto.Status.HasValue)
        {
            query = query.Where(s => s.Status == searchDto.Status.Value);
        }

        if (searchDto.GradeLevel.HasValue)
        {
            // Get students with current enrollments at the specified grade level via join
            var studentsWithGrade = _context.Enrollments
                .Where(e => e.GradeLevel == searchDto.GradeLevel.Value && 
                           e.Status == EnrollmentStatus.Active)
                .Select(e => e.StudentId)
                .Distinct();
            
            query = query.Where(s => studentsWithGrade.Contains(s.UserId));
        }

        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLowerInvariant();
            query = query.Where(s => 
                s.FirstName.ToLower().Contains(searchTerm) ||
                s.LastName.ToLower().Contains(searchTerm) ||
                s.StudentNumber.ToLower().Contains(searchTerm));
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var students = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Student>(students, searchDto.Page, searchDto.PageSize, totalCount);
    }

    public async Task<IList<Student>> GetStudentsInGradeLevelAsync(GradeLevel gradeLevel, Guid schoolYearId, CancellationToken cancellationToken = default)
    {
        // Get students via enrollment join since Student doesn't have Enrollments navigation
        var studentIds = await _context.Enrollments
            .Where(e => e.GradeLevel == gradeLevel && 
                       e.SchoolYearId == schoolYearId && 
                       e.Status == EnrollmentStatus.Active)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);
            
        return await _context.Students
            .Where(s => studentIds.Contains(s.UserId))
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<Student>> GetStudentsByStatusAsync(UserLifecycleStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .Where(s => s.Status == status)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveStudentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .CountAsync(s => s.Status == UserLifecycleStatus.Active, cancellationToken);
    }

    public async Task<IList<Student>> GetStudentsForRolloverAsync(GradeLevel fromGrade, Guid fromSchoolYearId, CancellationToken cancellationToken = default)
    {
        // Get students via enrollment join since Student doesn't have Enrollments navigation
        var studentIds = await _context.Enrollments
            .Where(e => e.GradeLevel == fromGrade && 
                       e.SchoolYearId == fromSchoolYearId && 
                       e.Status == EnrollmentStatus.Active)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);
            
        return await _context.Students
            .Where(s => studentIds.Contains(s.UserId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        await _context.Students.AddAsync(student, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Student> students, CancellationToken cancellationToken = default)
    {
        await _context.Students.AddRangeAsync(students, cancellationToken);
    }

    public void Update(Student student)
    {
        _context.Students.Update(student);
    }

    public void Remove(Student student)
    {
        _context.Students.Remove(student);
    }

    // Interface methods without CancellationToken
    public async Task<Student?> GetByIdAsync(Guid id)
    {
        return await GetByIdAsync(id, CancellationToken.None);
    }

    public async Task<Student?> GetByStudentNumberAsync(string studentNumber)
    {
        return await GetByStudentNumberAsync(studentNumber, CancellationToken.None);
    }

    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        return await _context.Students
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();
    }

    public async Task<PagedResult<Student>> SearchAsync(StudentSearchDto searchDto)
    {
        return await SearchStudentsAsync(searchDto, CancellationToken.None);
    }

    public async Task AddAsync(Student student)
    {
        await AddAsync(student, CancellationToken.None);
    }

    public async Task UpdateAsync(Student student)
    {
        Update(student);
        await Task.CompletedTask;
    }

    public async Task<Student?> GetByIdWithDetailsAsync(Guid id)
    {
        // Since Student doesn't have navigation properties, just return the basic student
        // The details would need to be loaded separately if needed
        return await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == id);
    }

    public async Task<bool> StudentNumberExistsAsync(string studentNumber)
    {
        return await _context.Students
            .AnyAsync(s => s.StudentNumber == studentNumber);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}