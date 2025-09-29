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
    Task<bool> SlugExistsAsync(string slug);
    Task AddAsync(DistrictTenant district);
    Task UpdateAsync(DistrictTenant district);
    Task SaveChangesAsync();
    Task<bool> HasActiveRetentionPoliciesAsync(Guid districtId);
}

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id);
    Task<Student?> GetByIdWithDetailsAsync(Guid id);
    Task<Student?> GetByStudentNumberAsync(string studentNumber);
    Task<bool> StudentNumberExistsAsync(string studentNumber);
    Task<IEnumerable<Student>> GetAllAsync();
    Task<PagedResult<Student>> SearchAsync(StudentSearchDto searchDto);
    Task AddAsync(Student student);
    Task UpdateAsync(Student student);
    Task SaveChangesAsync();
}

public interface IGuardianRepository
{
    Task<Guardian?> GetByIdAsync(Guid id);
    Task<IEnumerable<Guardian>> GetAllAsync();
    Task AddAsync(Guardian guardian);
    Task UpdateAsync(Guardian guardian);
    Task SaveChangesAsync();
}

public interface IStaffRepository
{
    Task<Staff?> GetByIdAsync(Guid id);
    Task<IEnumerable<Staff>> GetBySchoolIdAsync(Guid schoolId);
    Task<IEnumerable<Staff>> GetAllAsync();
    Task AddAsync(Staff staff);
    Task UpdateAsync(Staff staff);
    Task SaveChangesAsync();
}

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id);
    Task<Enrollment?> GetActiveEnrollmentAsync(Guid studentId, Guid classId);
    Task<IEnumerable<Enrollment>> GetActiveEnrollmentsByStudentAndSchoolAsync(Guid studentId, Guid schoolId);
    Task<IEnumerable<Enrollment>> GetByClassAndSchoolYearAsync(Guid classId, Guid schoolYearId);
    Task<IEnumerable<Enrollment>> GetBySchoolYearAsync(Guid schoolYearId);
    Task<IEnumerable<Enrollment>> GetByStudentIdAsync(Guid studentId);
    Task AddAsync(Enrollment enrollment);
    Task UpdateAsync(Enrollment enrollment);
    Task SaveChangesAsync();
}

public interface IClassRepository
{
    Task<Class?> GetByIdAsync(Guid id);
    Task<IEnumerable<Class>> GetBySchoolIdAsync(Guid schoolId);
    Task<IEnumerable<Class>> GetBySchoolYearIdAsync(Guid schoolYearId);
    Task AddAsync(Class classEntity);
    Task UpdateAsync(Class classEntity);
    Task SaveChangesAsync();
}

public interface ISchoolYearRepository
{
    Task<SchoolYear?> GetByIdAsync(Guid id);
    Task<SchoolYear?> GetByNameAsync(string name);
    Task<SchoolYear?> GetCurrentAsync();
    Task<IEnumerable<SchoolYear>> GetAllAsync();
    Task AddAsync(SchoolYear schoolYear);
    Task UpdateAsync(SchoolYear schoolYear);
    Task SaveChangesAsync();
}

public interface IRoleDefinitionRepository
{
    Task<RoleDefinition?> GetByIdAsync(Guid id);
    Task<RoleDefinition?> GetByNameAsync(string name);
    Task<IEnumerable<RoleDefinition>> GetAllAsync();
    Task<IEnumerable<RoleDefinition>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<IEnumerable<RoleDefinition>> GetByScopeAsync(RoleScope scope);
    Task AddAsync(RoleDefinition roleDefinition);
    Task UpdateAsync(RoleDefinition roleDefinition);
    Task SaveChangesAsync();
}

public interface IRoleAssignmentRepository
{
    Task<RoleAssignment?> GetByIdAsync(Guid id);
    Task<IEnumerable<RoleAssignment>> GetActiveRolesByUserIdAsync(Guid userId);
    Task<IEnumerable<RoleAssignment>> GetActiveAssignmentsByUserAsync(Guid userId, DateTime effectiveDate);
    Task<RoleAssignment?> GetActiveAssignmentAsync(Guid userId, Guid roleDefinitionId, Guid? schoolId, Guid? classId, Guid? schoolYearId);
    Task<IEnumerable<RoleAssignment>> GetByRoleDefinitionIdAsync(Guid roleDefinitionId);
    Task AddAsync(RoleAssignment roleAssignment);
    Task UpdateAsync(RoleAssignment roleAssignment);
    Task SaveChangesAsync();
}

public interface IAuditRepository
{
    Task<AuditRecord?> GetByIdAsync(Guid id);
    Task<PagedResult<AuditRecord>> QueryAsync(string? entityType, Guid? entityId, string? userId, string? eventType, DateTime? startDate, DateTime? endDate, int page, int pageSize);
    Task<IEnumerable<AuditRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, string[] entityTypes);
    Task<List<AuditRecord>> GetRecordsForIntegrityCheckAsync(long? startSequence, long? endSequence);
    Task<IEnumerable<AuditRecord>> GetChainAsync(string tenantId, DateTime startDate, DateTime endDate);
    Task<AuditComplianceSummary> GetComplianceSummaryAsync(DateTime startDate, DateTime endDate);
    Task AddAsync(AuditRecord auditRecord);
    Task SaveChangesAsync();
}

public class AuditComplianceSummary
{
    public int TotalRecords { get; set; }
    public Dictionary<string, int> EventTypeCounts { get; set; } = new();
    public int DataAccessCount { get; set; }
    public int DataModificationCount { get; set; }
    public int IntegrityIssues { get; set; }
}

public interface IAuditChainIntegrityService
{
    Task<bool> VerifyRecordIntegrityAsync(AuditRecord record);
}

public interface ILegalHoldRepository
{
    Task<LegalHold?> GetByIdAsync(Guid id);
    Task<LegalHold?> GetActiveHoldAsync(string entityType, Guid entityId);
    Task<List<LegalHold>> GetActiveLegalHoldsAsync();
    Task AddAsync(LegalHold legalHold);
    Task UpdateAsync(LegalHold legalHold);
    Task SaveChangesAsync();
}

public interface IRetentionPolicyRepository
{
    Task<RetentionPolicy?> GetByIdAsync(Guid id);
    Task<IEnumerable<RetentionPolicy>> GetActivePoliciesAsync();
    Task AddAsync(RetentionPolicy policy);
    Task UpdateAsync(RetentionPolicy policy);
    Task SaveChangesAsync();
}

public interface IRoleAuthorizationService
{
    Task<bool> HasPermissionAsync(Guid userId, string permission, Guid? schoolId = null, Guid? classId = null, Guid? schoolYearId = null);
}

public interface IPlatformAuditRepository
{
    Task<PlatformAuditRecord?> GetByIdAsync(Guid id);
    Task<PagedResult<PlatformAuditRecord>> QueryAsync(AuditQueryDto queryDto);
    Task AddAsync(PlatformAuditRecord auditRecord);
    Task SaveChangesAsync();
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
    Task LogAsync(string eventType, string entityType, Guid entityId, string userId, object changeDetails);
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
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public PagedResult(IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}