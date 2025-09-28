using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Application.DTOs;

// District DTOs
public class CreateDistrictDto
{
    public string Slug { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public DistrictQuotasDto? Quotas { get; set; }
}

public class DistrictDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public DistrictQuotasDto Quotas { get; set; } = null!;
    public DistrictStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DistrictQuotasDto
{
    public int MaxStudents { get; set; }
    public int MaxStaff { get; set; }
    public int MaxAdmins { get; set; }
}

// Student DTOs
public class CreateStudentDto
{
    public string StudentNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public GradeLevel CurrentGradeLevel { get; set; }
    public string? MiddleName { get; set; }
}

public class StudentDto
{
    public Guid UserId { get; set; }
    public string StudentNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string FullName => $"{FirstName} {(string.IsNullOrEmpty(MiddleName) ? "" : MiddleName + " ")}{LastName}";
    public DateTime DateOfBirth { get; set; }
    public GradeLevel CurrentGradeLevel { get; set; }
    public UserLifecycleStatus Status { get; set; }
    public DateTime? WithdrawalDate { get; set; }
    public string? WithdrawalReason { get; set; }
}

public class StudentSearchDto
{
    public string? SearchTerm { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public UserLifecycleStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// Enrollment DTOs
public class CreateEnrollmentDto
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid SchoolYearId { get; set; }
    public GradeLevel GradeLevel { get; set; }
    public DateTime EnrollmentDate { get; set; }
}

public class EnrollmentDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid SchoolYearId { get; set; }
    public GradeLevel GradeLevel { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public EnrollmentStatus Status { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? CompletionReason { get; set; }
}

public class TransferEnrollmentDto
{
    public Guid EnrollmentId { get; set; }
    public Guid ToClassId { get; set; }
    public DateTime TransferDate { get; set; }
    public string Reason { get; set; } = null!;
}

public class GraduateStudentDto
{
    public Guid EnrollmentId { get; set; }
    public DateTime GraduationDate { get; set; }
}

public class BulkRolloverDto
{
    public Guid FromSchoolYearId { get; set; }
    public Guid ToSchoolYearId { get; set; }
    public Dictionary<GradeLevel, GradeLevel> GradeLevelMappings { get; set; } = new();
}

public class BulkRolloverResultDto
{
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

// RBAC DTOs
public class AssignRoleDto
{
    public Guid UserId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    public Guid? SchoolId { get; set; }
    public Guid? ClassId { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

public class RoleAssignmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    public string RoleName { get; set; } = null!;
    public Guid? SchoolId { get; set; }
    public Guid? ClassId { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string AssignedBy { get; set; } = null!;
    public bool IsActive { get; set; }
}

// Audit DTOs
public class CreateAuditRecordDto
{
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string UserId { get; set; } = null!;
    public string Details { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
    public string? UserAgent { get; set; }
}

public class AuditRecordDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string UserId { get; set; } = null!;
    public string Details { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = null!;
    public string? UserAgent { get; set; }
    public string Hash { get; set; } = null!;
}

public class AuditQueryDto
{
    public string? EntityType { get; set; }
    public string? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AuditExportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string[] EntityTypes { get; set; } = Array.Empty<string>();
    public string Format { get; set; } = "CSV";
}

public class AuditExportResultDto
{
    public byte[] Data { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
}

// Search DTOs
public class EnrollmentSearchDto
{
    public Guid? StudentId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? SchoolYearId { get; set; }
    public EnrollmentStatus? Status { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public string? StudentSearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public class StaffSearchDto
{
    public string? SearchTerm { get; set; }
    public UserLifecycleStatus? Status { get; set; }
    public Guid? SchoolId { get; set; }
    public string? RoleName { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public class PlatformAuditQueryDto
{
    public string? TenantId { get; set; }
    public string? ActingUserId { get; set; }
    public string? EventType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}