using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Commands;

// Student Commands
public class CreateStudentCommand
{
    public CreateStudentDto Student { get; }
    public string CreatedBy { get; }

    public CreateStudentCommand(CreateStudentDto student, string createdBy)
    {
        Student = student;
        CreatedBy = createdBy;
    }
}

public class UpdateStudentGradeLevelCommand
{
    public Guid StudentId { get; }
    public Guid NewGradeLevel { get; }
    public string UpdatedBy { get; }

    public UpdateStudentGradeLevelCommand(Guid studentId, Guid newGradeLevel, string updatedBy)
    {
        StudentId = studentId;
        NewGradeLevel = newGradeLevel;
        UpdatedBy = updatedBy;
    }
}

public class WithdrawStudentCommand
{
    public Guid StudentId { get; }
    public DateTime WithdrawalDate { get; }
    public string Reason { get; }
    public string WithdrawnBy { get; }

    public WithdrawStudentCommand(Guid studentId, DateTime withdrawalDate, string reason, string withdrawnBy)
    {
        StudentId = studentId;
        WithdrawalDate = withdrawalDate;
        Reason = reason;
        WithdrawnBy = withdrawnBy;
    }
}

// Enrollment Commands
public class CreateEnrollmentCommand
{
    public CreateEnrollmentDto Enrollment { get; }
    public string CreatedBy { get; }

    public CreateEnrollmentCommand(CreateEnrollmentDto enrollment, string createdBy)
    {
        Enrollment = enrollment;
        CreatedBy = createdBy;
    }
}

public class TransferEnrollmentCommand
{
    public TransferEnrollmentDto Transfer { get; }
    public string TransferredBy { get; }

    public TransferEnrollmentCommand(TransferEnrollmentDto transfer, string transferredBy)
    {
        Transfer = transfer;
        TransferredBy = transferredBy;
    }
}

public class BulkRolloverCommand
{
    public BulkRolloverDto Rollover { get; }
    public string InitiatedBy { get; }

    public BulkRolloverCommand(BulkRolloverDto rollover, string initiatedBy)
    {
        Rollover = rollover;
        InitiatedBy = initiatedBy;
    }
}

// RBAC Commands
public class AssignRoleCommand
{
    public AssignRoleDto RoleAssignment { get; }
    public string AssignedBy { get; }

    public AssignRoleCommand(AssignRoleDto roleAssignment, string assignedBy)
    {
        RoleAssignment = roleAssignment;
        AssignedBy = assignedBy;
    }
}

public class RevokeRoleCommand
{
    public Guid RoleAssignmentId { get; }
    public string Reason { get; }
    public string RevokedBy { get; }

    public RevokeRoleCommand(Guid roleAssignmentId, string reason, string revokedBy)
    {
        RoleAssignmentId = roleAssignmentId;
        Reason = reason;
        RevokedBy = revokedBy;
    }
}

// Audit Commands
public class CreateAuditRecordCommand
{
    public CreateAuditRecordDto AuditRecord { get; }

    public CreateAuditRecordCommand(CreateAuditRecordDto auditRecord)
    {
        AuditRecord = auditRecord;
    }
}

// Additional Missing Commands for API Layer
public class PreviewBulkRolloverCommand
{
    public BulkRolloverDto Rollover { get; }
    public string InitiatedBy { get; }

    public PreviewBulkRolloverCommand(BulkRolloverDto rollover, string initiatedBy)
    {
        Rollover = rollover;
        InitiatedBy = initiatedBy;
    }
}

public class ExecuteBulkRolloverCommand
{
    public BulkRolloverDto Rollover { get; }
    public string InitiatedBy { get; }

    public ExecuteBulkRolloverCommand(BulkRolloverDto rollover, string initiatedBy)
    {
        Rollover = rollover;
        InitiatedBy = initiatedBy;
    }
}

public class UpdateStudentCommand
{
    public Guid StudentId { get; }
    public UpdateStudentDto Student { get; }
    public string UpdatedBy { get; }

    public UpdateStudentCommand(Guid studentId, UpdateStudentDto student, string updatedBy)
    {
        StudentId = studentId;
        Student = student;
        UpdatedBy = updatedBy;
    }
}

// Staff Commands
public class CreateStaffCommand
{
    public CreateStaffDto Staff { get; }
    public string CreatedBy { get; }

    public CreateStaffCommand(CreateStaffDto staff, string createdBy)
    {
        Staff = staff;
        CreatedBy = createdBy;
    }
}

public class UpdateStaffCommand
{
    public Guid StaffId { get; }
    public UpdateStaffDto Staff { get; }
    public string UpdatedBy { get; }

    public UpdateStaffCommand(Guid staffId, UpdateStaffDto staff, string updatedBy)
    {
        StaffId = staffId;
        Staff = staff;
        UpdatedBy = updatedBy;
    }
}

public class TerminateStaffCommand
{
    public Guid StaffId { get; }
    public DateTime TerminationDate { get; }
    public string Reason { get; }
    public string TerminatedBy { get; }

    public TerminateStaffCommand(Guid staffId, DateTime terminationDate, string reason, string terminatedBy)
    {
        StaffId = staffId;
        TerminationDate = terminationDate;
        Reason = reason;
        TerminatedBy = terminatedBy;
    }
}

// School Commands
public class DeactivateSchoolCommand
{
    public Guid SchoolId { get; }
    public string Reason { get; }
    public string DeactivatedBy { get; }

    public DeactivateSchoolCommand(Guid schoolId, string reason, string deactivatedBy)
    {
        SchoolId = schoolId;
        Reason = reason;
        DeactivatedBy = deactivatedBy;
    }
}

// District Commands
public class UpdateDistrictCommand
{
    public Guid DistrictId { get; }
    public UpdateDistrictDto District { get; }
    public string UpdatedBy { get; }

    public UpdateDistrictCommand(Guid districtId, UpdateDistrictDto district, string updatedBy)
    {
        DistrictId = districtId;
        District = district;
        UpdatedBy = updatedBy;
    }
}

// More Student Commands
public class WithdrawEnrollmentCommand
{
    public Guid EnrollmentId { get; }
    public DateTime WithdrawalDate { get; }
    public string Reason { get; }
    public string WithdrawnBy { get; }

    public WithdrawEnrollmentCommand(Guid enrollmentId, DateTime withdrawalDate, string reason, string withdrawnBy)
    {
        EnrollmentId = enrollmentId;
        WithdrawalDate = withdrawalDate;
        Reason = reason;
        WithdrawnBy = withdrawnBy;
    }
}

public class TransferStudentCommand
{
    public TransferStudentRequest Transfer { get; }
    public string TransferredBy { get; }

    public TransferStudentCommand(TransferStudentRequest transfer, string transferredBy)
    {
        Transfer = transfer;
        TransferredBy = transferredBy;
    }
}

public class PromoteStudentCommand
{
    public Guid StudentId { get; }
    public Guid ToSchoolYearId { get; }
    public string PromotedBy { get; }

    public PromoteStudentCommand(Guid studentId, Guid toSchoolYearId, string promotedBy)
    {
        StudentId = studentId;
        ToSchoolYearId = toSchoolYearId;
        PromotedBy = promotedBy;
    }
}

public class BulkImportStudentsCommand
{
    public byte[] FileContent { get; }
    public string FileName { get; }
    public string ImportedBy { get; }

    public BulkImportStudentsCommand(byte[] fileContent, string fileName, string importedBy)
    {
        FileContent = fileContent;
        FileName = fileName;
        ImportedBy = importedBy;
    }
}

public class MapStudentIdentityCommand
{
    public Guid StudentId { get; }
    public string ExternalSystemId { get; }
    public string SystemName { get; }
    public string MappedBy { get; }

    public MapStudentIdentityCommand(Guid studentId, string externalSystemId, string systemName, string mappedBy)
    {
        StudentId = studentId;
        ExternalSystemId = externalSystemId;
        SystemName = systemName;
        MappedBy = mappedBy;
    }
}

public class StudentLifecycleEventCommand
{
    public Guid StudentId { get; }
    public string EventType { get; }
    public DateTime EventDate { get; }
    public string Details { get; }
    public string RecordedBy { get; }

    public StudentLifecycleEventCommand(Guid studentId, string eventType, DateTime eventDate, string details, string recordedBy)
    {
        StudentId = studentId;
        EventType = eventType;
        EventDate = eventDate;
        Details = details;
        RecordedBy = recordedBy;
    }
}

public class CreateGuardianRelationshipCommand
{
    public Guid StudentId { get; }
    public GuardianDto Guardian { get; }
    public string CreatedBy { get; }

    public CreateGuardianRelationshipCommand(Guid studentId, GuardianDto guardian, string createdBy)
    {
        StudentId = studentId;
        Guardian = guardian;
        CreatedBy = createdBy;
    }
}

public class UpdateGuardianRelationshipCommand
{
    public Guid RelationshipId { get; }
    public GuardianDto Guardian { get; }
    public string UpdatedBy { get; }

    public UpdateGuardianRelationshipCommand(Guid relationshipId, GuardianDto guardian, string updatedBy)
    {
        RelationshipId = relationshipId;
        Guardian = guardian;
        UpdatedBy = updatedBy;
    }
}
