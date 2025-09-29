using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Queries;

// District Queries
public class GetDistrictByIdQuery
{
    public Guid DistrictId { get; }

    public GetDistrictByIdQuery(Guid districtId)
    {
        DistrictId = districtId;
    }
}

public class GetDistrictBySlugQuery
{
    public string Slug { get; }

    public GetDistrictBySlugQuery(string slug)
    {
        Slug = slug;
    }
}

public class GetAllDistrictsQuery
{
    // No parameters for get all
}

// Student Queries
public class GetStudentByIdQuery
{
    public Guid StudentId { get; }

    public GetStudentByIdQuery(Guid studentId)
    {
        StudentId = studentId;
    }
}

public class SearchStudentsQuery
{
    public StudentSearchDto SearchCriteria { get; }

    public SearchStudentsQuery(StudentSearchDto searchCriteria)
    {
        SearchCriteria = searchCriteria;
    }
}

// Enrollment Queries
public class GetClassRosterQuery
{
    public Guid ClassId { get; }
    public Guid SchoolYearId { get; }

    public GetClassRosterQuery(Guid classId, Guid schoolYearId)
    {
        ClassId = classId;
        SchoolYearId = schoolYearId;
    }
}

public class GetStudentEnrollmentsQuery
{
    public Guid StudentId { get; }

    public GetStudentEnrollmentsQuery(Guid studentId)
    {
        StudentId = studentId;
    }
}

// RBAC Queries
public class GetUserPermissionsQuery
{
    public Guid UserId { get; }

    public GetUserPermissionsQuery(Guid userId)
    {
        UserId = userId;
    }
}

public class GetUserRoleAssignmentsQuery
{
    public Guid UserId { get; }

    public GetUserRoleAssignmentsQuery(Guid userId)
    {
        UserId = userId;
    }
}

public class ValidatePermissionQuery
{
    public Guid UserId { get; }
    public string Permission { get; }

    public ValidatePermissionQuery(Guid userId, string permission)
    {
        UserId = userId;
        Permission = permission;
    }
}

// Audit Queries
public class QueryAuditRecordsQuery
{
    public AuditQueryDto Query { get; }

    public QueryAuditRecordsQuery(AuditQueryDto query)
    {
        Query = query;
    }
}

public class ExportAuditRecordsQuery
{
    public AuditExportDto Export { get; }

    public ExportAuditRecordsQuery(AuditExportDto export)
    {
        Export = export;
    }
}

public class ValidateAuditChainQuery
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public ValidateAuditChainQuery(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}

// Additional Student Queries
public class ListStudentsQuery
{
    public int Page { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }

    public ListStudentsQuery(int page, int pageSize, string? searchTerm = null)
    {
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
        SearchTerm = searchTerm;
    }
}

// Staff Queries
public class ListStaffQuery
{
    public int Page { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }

    public ListStaffQuery(int page, int pageSize, string? searchTerm = null)
    {
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
        SearchTerm = searchTerm;
    }
}

public class GetStaffRoleAssignmentsQuery
{
    public Guid StaffId { get; }
    public int Page { get; }
    public int PageSize { get; }

    public GetStaffRoleAssignmentsQuery(Guid staffId, int page, int pageSize)
    {
        StaffId = staffId;
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
    }
}

// School Queries
public class ListSchoolsQuery
{
    public int Page { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }

    public ListSchoolsQuery(int page, int pageSize, string? searchTerm = null)
    {
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
        SearchTerm = searchTerm;
    }
}

public class GetSchoolClassesQuery
{
    public Guid SchoolId { get; }
    public Guid? SchoolYearId { get; }
    public int Page { get; }
    public int PageSize { get; }

    public GetSchoolClassesQuery(Guid schoolId, Guid? schoolYearId, int page, int pageSize)
    {
        SchoolId = schoolId;
        SchoolYearId = schoolYearId;
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
    }
}

// District Queries
public class ListDistrictsQuery
{
    public int Page { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }

    public ListDistrictsQuery(int page, int pageSize, string? searchTerm = null)
    {
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
        SearchTerm = searchTerm;
    }
}

// Additional Audit Queries
public class SearchAuditRecordsQuery
{
    public AuditQueryDto Query { get; }

    public SearchAuditRecordsQuery(AuditQueryDto query)
    {
        Query = query;
    }
}

public class GetEntityAuditTrailQuery
{
    public string EntityType { get; }
    public Guid EntityId { get; }
    public int Page { get; }
    public int PageSize { get; }

    public GetEntityAuditTrailQuery(string entityType, Guid entityId, int page, int pageSize)
    {
        EntityType = entityType;
        EntityId = entityId;
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
    }
}

public class VerifyAuditChainQuery
{
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }

    public VerifyAuditChainQuery(DateTime? startDate = null, DateTime? endDate = null)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}

public class GetAuditStatisticsQuery
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public GetAuditStatisticsQuery(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}

public class GetUserActivityQuery
{
    public string UserId { get; }
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }
    public int Page { get; }
    public int PageSize { get; }

    public GetUserActivityQuery(string userId, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        UserId = userId;
        StartDate = startDate;
        EndDate = endDate;
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
    }
}

public class GenerateComplianceReportQuery
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public GenerateComplianceReportQuery(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}
