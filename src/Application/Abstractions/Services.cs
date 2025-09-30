using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Application.Abstractions;

/// <summary>
/// Service interface for managing districts
/// </summary>
public interface IDistrictManagementService
{
    Task<DistrictTenant> ProvisionDistrictAsync(string slug, string displayName, string createdBy, CancellationToken cancellationToken = default);
    Task<DistrictTenant> GetDistrictBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task UpdateDistrictStatusAsync(string slug, string status, string? reason, string updatedBy, CancellationToken cancellationToken = default);
    Task UpdateDistrictQuotasAsync(string slug, int students, int staff, int admins, string updatedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for managing identity mappings
/// </summary>
public interface IIdentityMappingService
{
    Task<IdentityMapping> CreateMappingAsync(string issuer, string externalUserId, Guid internalUserId, string createdBy, CancellationToken cancellationToken = default);
    Task<IdentityMapping?> GetMappingByExternalIdAsync(string issuer, string externalUserId, CancellationToken cancellationToken = default);
    Task<Guid?> ResolveInternalUserIdAsync(string issuer, string externalUserId, CancellationToken cancellationToken = default);
    Task SuspendMappingAsync(Guid mappingId, DateTime suspendedUntil, string reason, string suspendedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for managing academic calendars
/// </summary>
public interface IAcademicCalendarService
{
    Task<AcademicCalendar> CreateCalendarAsync(Guid schoolYearId, string createdBy, CancellationToken cancellationToken = default);
    Task AddInstructionalDaysAsync(Guid calendarId, List<DateTime> dates, string addedBy, CancellationToken cancellationToken = default);
    Task AddClosuresAsync(Guid calendarId, List<DateTime> dates, string reason, string addedBy, CancellationToken cancellationToken = default);
    Task<List<DateTime>> GetEffectiveDaysAsync(Guid calendarId, CancellationToken cancellationToken = default);
    Task PublishCalendarAsync(Guid calendarId, string publishedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for managing role assignments
/// </summary>
public interface IRoleManagementService
{
    Task<RoleAssignment> AssignRoleAsync(Guid userId, Guid roleDefinitionId, Guid? schoolId, Guid? classId, string assignedBy, CancellationToken cancellationToken = default);
    Task RevokeRoleAsync(Guid assignmentId, string reason, string revokedBy, CancellationToken cancellationToken = default);
    Task<List<RoleAssignment>> GetEffectiveRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CheckPermissionAsync(Guid userId, string permission, Guid? resourceId, CancellationToken cancellationToken = default);
    Task CleanupExpiredAssignmentsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for managing enrollments
/// </summary>
public interface IEnrollmentService
{
    Task<Enrollment> EnrollStudentAsync(Guid studentId, Guid classId, DateTime? enrollmentDate, bool allowOverCapacity, string enrolledBy, CancellationToken cancellationToken = default);
    Task WithdrawStudentAsync(Guid enrollmentId, DateTime withdrawalDate, string? reason, string withdrawnBy, CancellationToken cancellationToken = default);
    Task<Enrollment> TransferStudentAsync(Guid currentEnrollmentId, Guid newClassId, DateTime transferDate, string? reason, string transferredBy, CancellationToken cancellationToken = default);
    Task<List<Student>> GetClassRosterAsync(Guid classId, CancellationToken cancellationToken = default);
    Task<List<Enrollment>> GetStudentScheduleAsync(Guid studentId, Guid? schoolYearId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for managing assessments
/// </summary>
public interface IAssessmentService
{
    Task<AssessmentDefinition> CreateAssessmentAsync(string title, string fileKey, long fileSizeBytes, string createdBy, CancellationToken cancellationToken = default);
    Task PublishAssessmentAsync(Guid assessmentId, string publishedBy, CancellationToken cancellationToken = default);
    Task<string> GetAssessmentFileUrlAsync(Guid assessmentId, TimeSpan urlExpiration, CancellationToken cancellationToken = default);
    Task<AssessmentDefinition> CreateNewVersionAsync(Guid existingAssessmentId, string fileKey, long fileSizeBytes, string createdBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for managing bulk operations
/// </summary>
public interface IBulkOperationService
{
    Task<BulkJob> ExecuteBulkOperationAsync(string operationType, int totalRows, string errorStrategy, int? errorThreshold, bool isDryRun, string initiatedBy, CancellationToken cancellationToken = default);
    Task UpdateProgressAsync(Guid jobId, int processedRows, int failedRows, CancellationToken cancellationToken = default);
    Task CompleteJobAsync(Guid jobId, string? errorDetails, CancellationToken cancellationToken = default);
    Task<BulkJob> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for managing audit records
/// </summary>
public interface IAuditService
{
    Task<AuditRecord> CreateAuditRecordAsync(string entityType, Guid entityId, string action, string? entitySnapshot, string actor, CancellationToken cancellationToken = default);
    Task<bool> VerifyAuditChainIntegrityAsync(string tenantSlug, CancellationToken cancellationToken = default);
    Task<List<AuditRecord>> QueryAuditRecordsAsync(string? entityType, Guid? entityId, string? actor, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for managing data retention
/// </summary>
public interface IRetentionService
{
    Task<RetentionPolicy> ApplyRetentionPolicyAsync(string entityType, int retentionDays, int graceDays, string reason, string appliedBy, CancellationToken cancellationToken = default);
    Task<LegalHold> ApplyLegalHoldAsync(string entityType, Guid entityId, string caseNumber, string reason, string appliedBy, CancellationToken cancellationToken = default);
    Task ReleaseLegalHoldAsync(Guid legalHoldId, string releaseReason, string releasedBy, CancellationToken cancellationToken = default);
    Task<List<Guid>> IdentifyEntitiesForPurgeAsync(string entityType, CancellationToken cancellationToken = default);
    Task ExecutePurgeAsync(string entityType, List<Guid> entityIds, string executedBy, CancellationToken cancellationToken = default);
}
