using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Commands;

// District Commands
public class CreateDistrictCommand
{
    public CreateDistrictDto District { get; }
    public string CreatedBy { get; }

    public CreateDistrictCommand(CreateDistrictDto district, string createdBy)
    {
        District = district;
        CreatedBy = createdBy;
    }
}

public class UpdateDistrictQuotasCommand
{
    public Guid DistrictId { get; }
    public DistrictQuotasDto Quotas { get; }
    public string UpdatedBy { get; }

    public UpdateDistrictQuotasCommand(Guid districtId, DistrictQuotasDto quotas, string updatedBy)
    {
        DistrictId = districtId;
        Quotas = quotas;
        UpdatedBy = updatedBy;
    }
}

public class SuspendDistrictCommand
{
    public Guid DistrictId { get; }
    public string Reason { get; }
    public string SuspendedBy { get; }

    public SuspendDistrictCommand(Guid districtId, string reason, string suspendedBy)
    {
        DistrictId = districtId;
        Reason = reason;
        SuspendedBy = suspendedBy;
    }
}

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