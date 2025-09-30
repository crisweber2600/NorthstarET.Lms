using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Application.Abstractions;

/// <summary>
/// Unit of Work pattern for managing transactions
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Save all changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin a database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for DistrictTenant entities
/// </summary>
public interface IDistrictRepository
{
    /// <summary>
    /// Get district by slug
    /// </summary>
    Task<DistrictTenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get district by ID
    /// </summary>
    Task<DistrictTenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new district
    /// </summary>
    void Add(DistrictTenant district);

    /// <summary>
    /// Update an existing district
    /// </summary>
    void Update(DistrictTenant district);

    /// <summary>
    /// Delete a district
    /// </summary>
    void Delete(DistrictTenant district);

    /// <summary>
    /// Check if a slug exists
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for SchoolYear entities
/// </summary>
public interface ISchoolYearRepository
{
    /// <summary>
    /// Get school year by ID
    /// </summary>
    Task<SchoolYear?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get school years for a tenant
    /// </summary>
    Task<IEnumerable<SchoolYear>> GetByTenantAsync(string tenantSlug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new school year
    /// </summary>
    void Add(SchoolYear schoolYear);

    /// <summary>
    /// Update an existing school year
    /// </summary>
    void Update(SchoolYear schoolYear);

    /// <summary>
    /// Check for overlapping school years
    /// </summary>
    Task<bool> HasOverlappingYearsAsync(string tenantSlug, DateTime startDate, DateTime endDate, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Student entities
/// </summary>
public interface IStudentRepository
{
    /// <summary>
    /// Get student by ID
    /// </summary>
    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get student by student number
    /// </summary>
    Task<Student?> GetByStudentNumberAsync(string tenantSlug, string studentNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get students by grade level
    /// </summary>
    Task<IEnumerable<Student>> GetByGradeLevelAsync(string tenantSlug, int gradeLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new student
    /// </summary>
    void Add(Student student);

    /// <summary>
    /// Update an existing student
    /// </summary>
    void Update(Student student);

    /// <summary>
    /// Check if student number exists
    /// </summary>
    Task<bool> StudentNumberExistsAsync(string tenantSlug, string studentNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for RoleAssignment entities
/// </summary>
public interface IRoleAssignmentRepository
{
    /// <summary>
    /// Get role assignment by ID
    /// </summary>
    Task<RoleAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role assignments for a user
    /// </summary>
    Task<IEnumerable<RoleAssignment>> GetByUserIdAsync(string tenantSlug, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired role assignments eligible for cleanup
    /// </summary>
    Task<IEnumerable<RoleAssignment>> GetExpiredAssignmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new role assignment
    /// </summary>
    void Add(RoleAssignment roleAssignment);

    /// <summary>
    /// Update an existing role assignment
    /// </summary>
    void Update(RoleAssignment roleAssignment);
}

/// <summary>
/// Service interface for tenant context access
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>
    /// Get the current tenant slug
    /// </summary>
    string? TenantSlug { get; }

    /// <summary>
    /// Set the tenant context
    /// </summary>
    void SetTenant(string tenantSlug);
}

/// <summary>
/// Service interface for domain event publishing
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publish domain events
    /// </summary>
    Task PublishEventsAsync(IEnumerable<NorthstarET.Lms.Domain.Common.DomainEvent> events, CancellationToken cancellationToken = default);
}