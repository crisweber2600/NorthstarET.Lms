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