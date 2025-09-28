using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Application.DTOs;

// Request/Response DTOs for Use Cases

// District Request DTOs
public class CreateDistrictRequest
{
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DistrictQuotasDto Quotas { get; set; } = new();
}

// Student Request DTOs  
public class CreateStudentRequest
{
    public string StudentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public StudentProgramsDto Programs { get; set; } = new();
    public GuardianDto[] Guardians { get; set; } = Array.Empty<GuardianDto>();
}

public class UpdateStudentProgramsRequest
{
    public bool IsSpecialEducation { get; set; }
    public bool IsGifted { get; set; }
    public bool IsEnglishLanguageLearner { get; set; }
    public string[] AccommodationTags { get; set; } = Array.Empty<string>();
}

public class StudentDetailDto : StudentDto
{
    public List<EnrollmentDto> CurrentEnrollments { get; set; } = new();
    public List<GuardianRelationshipDto> GuardianRelationships { get; set; } = new();
}

public class StudentProgramsDto
{
    public bool IsSpecialEducation { get; set; }
    public bool IsGifted { get; set; }
    public bool IsEnglishLanguageLearner { get; set; }
    public string[] AccommodationTags { get; set; } = Array.Empty<string>();
}

public class GuardianDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool CanPickup { get; set; }
}

public class GuardianRelationshipDto
{
    public Guid GuardianId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool CanPickup { get; set; }
    public DateTime EffectiveDate { get; set; }
}

// Enrollment Request DTOs
public class EnrollStudentRequest
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public string GradeLevel { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
}

public class WithdrawStudentRequest
{
    public Guid EnrollmentId { get; set; }
    public DateTime WithdrawalDate { get; set; }
    public string WithdrawalReason { get; set; } = string.Empty;
}

public class TransferStudentRequest
{
    public Guid StudentId { get; set; }
    public Guid FromSchoolId { get; set; }
    public Guid ToSchoolId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool MaintainGradeLevel { get; set; }
    public List<ClassTransferDto> TransferClasses { get; set; } = new();
}

public class ClassTransferDto
{
    public Guid FromClassId { get; set; }
    public Guid ToClassId { get; set; }
}

public class TransferResultDto
{
    public Guid StudentId { get; set; }
    public DateTime TransferDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<EnrollmentTransferDto> EnrollmentTransfers { get; set; } = new();
}

public class EnrollmentTransferDto
{
    public Guid FromClassId { get; set; }
    public Guid ToClassId { get; set; }
    public Guid? NewEnrollmentId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

// RBAC Request DTOs
public class AssignRoleRequest
{
    public Guid UserId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    public Guid? SchoolId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? SchoolYearId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public Guid? DelegatedByUserId { get; set; }
    public DateTime? DelegationExpiry { get; set; }
}

public class UserRoleDto
{
    public Guid AssignmentId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public Guid? SchoolId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? SchoolYearId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsDelegated { get; set; }
    public Guid? DelegatedByUserId { get; set; }
    public DateTime? DelegationExpiry { get; set; }
}

// Audit Request DTOs
public class AuditQueryRequest
{
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? EventType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AuditIntegrityCheckRequest
{
    public long? StartSequence { get; set; }
    public long? EndSequence { get; set; }
}

public class AuditIntegrityResultDto
{
    public DateTime CheckStartTime { get; set; }
    public int RecordsChecked { get; set; }
    public long StartSequence { get; set; }
    public long EndSequence { get; set; }
    public int IssuesFound { get; set; }
    public List<AuditIntegrityIssue> Issues { get; set; } = new();
    public string OverallIntegrity { get; set; } = string.Empty;
}

public class AuditIntegrityIssue
{
    public Guid RecordId { get; set; }
    public long SequenceNumber { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreateLegalHoldRequest
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class LegalHoldDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime HoldDate { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool IsActive { get; set; }
    public string AuthorizingUser { get; set; } = string.Empty;
}

public class ComplianceReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class ComplianceReportDto
{
    public DateTime GeneratedDate { get; set; }
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public AuditComplianceSummaryDto AuditSummary { get; set; } = new();
    public LegalHoldSummaryDto LegalHoldSummary { get; set; } = new();
    public RetentionSummaryDto RetentionSummary { get; set; } = new();
}

public class AuditComplianceSummaryDto
{
    public int TotalAuditRecords { get; set; }
    public Dictionary<string, int> RecordsByEventType { get; set; } = new();
    public int DataAccessEvents { get; set; }
    public int DataModificationEvents { get; set; }
    public int IntegrityIssues { get; set; }
}

public class LegalHoldSummaryDto
{
    public int ActiveLegalHolds { get; set; }
    public Dictionary<string, int> HoldsByEntityType { get; set; } = new();
    public DateTime? OldestActiveHold { get; set; }
}

public class RetentionSummaryDto
{
    public int ActivePolicies { get; set; }
    public Dictionary<string, int> PoliciesByEntityType { get; set; } = new();
}

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
    public string Status { get; set; } = null!; // Will be converted from enum
    public DistrictQuotasDto Quotas { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public string CreatedByUserId { get; set; } = null!;
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
    public DateTime EnrollmentDate { get; set; }
    public string CurrentGradeLevel { get; set; } = null!; // Will be converted from enum
    public string Status { get; set; } = null!; // Will be converted from enum
    public DateTime? WithdrawalDate { get; set; }
    public string? WithdrawalReason { get; set; }
    public StudentProgramsDto Programs { get; set; } = new();
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
    public Guid Id { get; set; } // Change from EnrollmentId to Id
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid? SchoolYearId { get; set; }
    public string GradeLevel { get; set; } = null!; // Will be converted from enum
    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = null!; // Will be converted from enum
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
    public Guid? SchoolYearId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive { get; set; }
    public Guid? DelegatedByUserId { get; set; }
    public DateTime? DelegationExpiry { get; set; }
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

// Additional Missing DTOs for API Controllers
public class StudentSummaryDto
{
    public Guid UserId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CurrentGradeLevel { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public UserLifecycleStatus Status { get; set; }
}

public class UpdateStudentDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class BulkRolloverPreviewDto
{
    public int TotalStudents { get; set; }
    public int EligibleForPromotion { get; set; }
    public int RequiringIntervention { get; set; }
    public Dictionary<string, int> PromotionsByGrade { get; set; } = new();
}

public class GuardianRelationshipDto
{
    public Guid RelationshipId { get; set; }
    public Guid StudentId { get; set; }
    public Guid GuardianId { get; set; }
    public string GuardianName { get; set; } = string.Empty;
    public RelationshipType RelationshipType { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class StaffSummaryDto
{
    public Guid UserId { get; set; }
    public string StaffNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public UserLifecycleStatus Status { get; set; }
}
