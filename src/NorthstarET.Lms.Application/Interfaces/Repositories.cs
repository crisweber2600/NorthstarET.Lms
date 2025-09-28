using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Common;

namespace NorthstarET.Lms.Application.Interfaces;

public interface IDistrictRepository
{
    Task<DistrictTenant?> GetByIdAsync(Guid id);
    Task<DistrictTenant?> GetBySlugAsync(string slug);
    Task<IEnumerable<DistrictTenant>> GetAllAsync();
    Task AddAsync(DistrictTenant district);
    Task UpdateAsync(DistrictTenant district);
    Task<bool> HasActiveRetentionPoliciesAsync(Guid districtId);
}

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id);
    Task<Student?> GetByStudentNumberAsync(string studentNumber);
    Task<IEnumerable<Student>> GetAllAsync();
    Task<PagedResult<Student>> SearchAsync(StudentSearchDto searchDto);
    Task AddAsync(Student student);
    Task UpdateAsync(Student student);
}

public interface IStaffRepository
{
    Task<Staff?> GetByIdAsync(Guid id);
    Task<IEnumerable<Staff>> GetBySchoolIdAsync(Guid schoolId);
    Task<IEnumerable<Staff>> GetAllAsync();
    Task AddAsync(Staff staff);
    Task UpdateAsync(Staff staff);
}

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id);
    Task<Enrollment?> GetActiveEnrollmentAsync(Guid studentId, Guid classId, Guid schoolYearId);
    Task<IEnumerable<Enrollment>> GetByClassAndSchoolYearAsync(Guid classId, Guid schoolYearId);
    Task<IEnumerable<Enrollment>> GetBySchoolYearAsync(Guid schoolYearId);
    Task<IEnumerable<Enrollment>> GetByStudentIdAsync(Guid studentId);
    Task AddAsync(Enrollment enrollment);
    Task UpdateAsync(Enrollment enrollment);
}

public interface IClassRepository
{
    Task<Class?> GetByIdAsync(Guid id);
    Task<IEnumerable<Class>> GetBySchoolIdAsync(Guid schoolId);
    Task<IEnumerable<Class>> GetBySchoolYearIdAsync(Guid schoolYearId);
    Task AddAsync(Class classEntity);
    Task UpdateAsync(Class classEntity);
}

public interface ISchoolYearRepository
{
    Task<SchoolYear?> GetByIdAsync(Guid id);
    Task<SchoolYear?> GetByNameAsync(string name);
    Task<SchoolYear?> GetCurrentAsync();
    Task<IEnumerable<SchoolYear>> GetAllAsync();
    Task AddAsync(SchoolYear schoolYear);
    Task UpdateAsync(SchoolYear schoolYear);
}

public interface IRoleDefinitionRepository
{
    Task<RoleDefinition?> GetByIdAsync(Guid id);
    Task<RoleDefinition?> GetByNameAsync(string name);
    Task<IEnumerable<RoleDefinition>> GetAllAsync();
    Task<IEnumerable<RoleDefinition>> GetByScopeAsync(RoleScope scope);
    Task AddAsync(RoleDefinition roleDefinition);
    Task UpdateAsync(RoleDefinition roleDefinition);
}

public interface IRoleAssignmentRepository
{
    Task<RoleAssignment?> GetByIdAsync(Guid id);
    Task<IEnumerable<RoleAssignment>> GetActiveRolesByUserIdAsync(Guid userId);
    Task<RoleAssignment?> GetActiveRoleAssignmentAsync(Guid userId, Guid roleDefinitionId, Guid? schoolId, Guid? classId);
    Task<IEnumerable<RoleAssignment>> GetByRoleDefinitionIdAsync(Guid roleDefinitionId);
    Task AddAsync(RoleAssignment roleAssignment);
    Task UpdateAsync(RoleAssignment roleAssignment);
}

public interface IAuditRepository
{
    Task<AuditRecord?> GetByIdAsync(Guid id);
    Task<PagedResult<AuditRecord>> QueryAsync(AuditQueryDto queryDto);
    Task<IEnumerable<AuditRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, string[] entityTypes);
    Task<IEnumerable<AuditRecord>> GetChainAsync(string tenantId, DateTime startDate, DateTime endDate);
    Task AddAsync(AuditRecord auditRecord);
}

public interface IPlatformAuditRepository
{
    Task<PlatformAuditRecord?> GetByIdAsync(Guid id);
    Task<PagedResult<PlatformAuditRecord>> QueryAsync(AuditQueryDto queryDto);
    Task AddAsync(PlatformAuditRecord auditRecord);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

public interface IAuditService
{
    Task<AuditRecord> LogAuditEventAsync(CreateAuditRecordDto auditDto);
    Task<PlatformAuditRecord> LogPlatformAuditEventAsync(CreateAuditRecordDto auditDto);
    Task<PagedResult<AuditRecord>> QueryAuditRecordsAsync(AuditQueryDto queryDto);
    Task<AuditExportResultDto> ExportAuditRecordsAsync(AuditExportDto exportDto);
    Task<bool> ValidateAuditChainAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<string>> DetectAuditTamperingAsync(DateTime startDate, DateTime endDate);
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public PagedResult(IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}