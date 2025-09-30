using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Infrastructure.Persistence.Repositories;

public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}

public class Repository<T> : IRepository<T> where T : Entity
{
    protected readonly LmsDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(LmsDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public interface IDistrictRepository : IRepository<DistrictTenant>
{
    Task<DistrictTenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public class DistrictRepository : Repository<DistrictTenant>, IDistrictRepository
{
    public DistrictRepository(LmsDbContext context) : base(context) { }

    public async Task<DistrictTenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.TenantSlug == slug, cancellationToken);
    }
}

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByStudentNumberAsync(string studentNumber, CancellationToken cancellationToken = default);
    Task<List<Student>> GetByGradeLevelAsync(GradeLevel gradeLevel, CancellationToken cancellationToken = default);
}

public class StudentRepository : Repository<Student>, IStudentRepository
{
    public StudentRepository(LmsDbContext context) : base(context) { }

    public async Task<Student?> GetByStudentNumberAsync(string studentNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.StudentNumber == studentNumber, cancellationToken);
    }

    public async Task<List<Student>> GetByGradeLevelAsync(GradeLevel gradeLevel, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.GradeLevel == gradeLevel).ToListAsync(cancellationToken);
    }
}
