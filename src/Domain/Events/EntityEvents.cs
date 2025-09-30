using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Events;

// Enrollment Events
public sealed class EnrollmentCreatedEvent : DomainEvent
{
    public Guid EnrollmentId { get; }
    public Guid StudentId { get; }
    public Guid ClassId { get; }
    public Guid SchoolYearId { get; }
    public DateTime EntryDate { get; }
    public string CreatedBy { get; }

    public EnrollmentCreatedEvent(Guid enrollmentId, Guid studentId, Guid classId, Guid schoolYearId, DateTime entryDate, string createdBy)
    {
        EnrollmentId = enrollmentId;
        StudentId = studentId;
        ClassId = classId;
        SchoolYearId = schoolYearId;
        EntryDate = entryDate;
        CreatedBy = createdBy;
    }
}

public sealed class EnrollmentWithdrawnEvent : DomainEvent
{
    public Guid EnrollmentId { get; }
    public Guid StudentId { get; }
    public Guid ClassId { get; }
    public DateTime ExitDate { get; }
    public string Reason { get; }
    public string WithdrawnBy { get; }

    public EnrollmentWithdrawnEvent(Guid enrollmentId, Guid studentId, Guid classId, DateTime exitDate, string reason, string withdrawnBy)
    {
        EnrollmentId = enrollmentId;
        StudentId = studentId;
        ClassId = classId;
        ExitDate = exitDate;
        Reason = reason;
        WithdrawnBy = withdrawnBy;
    }
}

public sealed class EnrollmentTransferredEvent : DomainEvent
{
    public Guid EnrollmentId { get; }
    public Guid StudentId { get; }
    public Guid ClassId { get; }
    public DateTime ExitDate { get; }
    public string Reason { get; }
    public string TransferredBy { get; }

    public EnrollmentTransferredEvent(Guid enrollmentId, Guid studentId, Guid classId, DateTime exitDate, string reason, string transferredBy)
    {
        EnrollmentId = enrollmentId;
        StudentId = studentId;
        ClassId = classId;
        ExitDate = exitDate;
        Reason = reason;
        TransferredBy = transferredBy;
    }
}

// Assessment Events
public sealed class AssessmentDefinitionCreatedEvent : DomainEvent
{
    public Guid AssessmentId { get; }
    public Guid DistrictId { get; }
    public string Title { get; }
    public string Subject { get; }
    public string CreatedBy { get; }

    public AssessmentDefinitionCreatedEvent(Guid assessmentId, Guid districtId, string title, string subject, string createdBy)
    {
        AssessmentId = assessmentId;
        DistrictId = districtId;
        Title = title;
        Subject = subject;
        CreatedBy = createdBy;
    }
}

public sealed class AssessmentDefinitionPublishedEvent : DomainEvent
{
    public Guid AssessmentId { get; }
    public string Title { get; }
    public int Version { get; }
    public string PublishedBy { get; }

    public AssessmentDefinitionPublishedEvent(Guid assessmentId, string title, int version, string publishedBy)
    {
        AssessmentId = assessmentId;
        Title = title;
        Version = version;
        PublishedBy = publishedBy;
    }
}

// Identity Events
public sealed class IdentityMappingCreatedEvent : DomainEvent
{
    public Guid MappingId { get; }
    public Guid InternalUserId { get; }
    public string ExternalId { get; }
    public string Issuer { get; }
    public string CreatedBy { get; }

    public IdentityMappingCreatedEvent(Guid mappingId, Guid internalUserId, string externalId, string issuer, string createdBy)
    {
        MappingId = mappingId;
        InternalUserId = internalUserId;
        ExternalId = externalId;
        Issuer = issuer;
        CreatedBy = createdBy;
    }
}

public sealed class IdentityMappingSuspendedEvent : DomainEvent
{
    public Guid MappingId { get; }
    public Guid InternalUserId { get; }
    public string ExternalId { get; }
    public string Issuer { get; }
    public string Reason { get; }
    public string SuspendedBy { get; }

    public IdentityMappingSuspendedEvent(Guid mappingId, Guid internalUserId, string externalId, string issuer, string reason, string suspendedBy)
    {
        MappingId = mappingId;
        InternalUserId = internalUserId;
        ExternalId = externalId;
        Issuer = issuer;
        Reason = reason;
        SuspendedBy = suspendedBy;
    }
}

// Legal Hold Events
public sealed class LegalHoldAppliedEvent : DomainEvent
{
    public Guid LegalHoldId { get; }
    public string EntityType { get; }
    public Guid EntityId { get; }
    public string Reason { get; }
    public string IssuedBy { get; }

    public LegalHoldAppliedEvent(Guid legalHoldId, string entityType, Guid entityId, string reason, string issuedBy)
    {
        LegalHoldId = legalHoldId;
        EntityType = entityType;
        EntityId = entityId;
        Reason = reason;
        IssuedBy = issuedBy;
    }
}

public sealed class LegalHoldReleasedEvent : DomainEvent
{
    public Guid LegalHoldId { get; }
    public string EntityType { get; }
    public Guid EntityId { get; }
    public string ReleaseReason { get; }
    public string ReleasedBy { get; }

    public LegalHoldReleasedEvent(Guid legalHoldId, string entityType, Guid entityId, string releaseReason, string releasedBy)
    {
        LegalHoldId = legalHoldId;
        EntityType = entityType;
        EntityId = entityId;
        ReleaseReason = releaseReason;
        ReleasedBy = releasedBy;
    }
}

// Bulk Job Events
public sealed class BulkJobCreatedEvent : DomainEvent
{
    public Guid JobId { get; }
    public string JobType { get; }
    public int TotalItems { get; }
    public bool IsDryRun { get; }
    public string RequestedBy { get; }

    public BulkJobCreatedEvent(Guid jobId, string jobType, int totalItems, bool isDryRun, string requestedBy)
    {
        JobId = jobId;
        JobType = jobType;
        TotalItems = totalItems;
        IsDryRun = isDryRun;
        RequestedBy = requestedBy;
    }
}

public sealed class BulkJobStartedEvent : DomainEvent
{
    public Guid JobId { get; }
    public string JobType { get; }
    public int TotalItems { get; }

    public BulkJobStartedEvent(Guid jobId, string jobType, int totalItems)
    {
        JobId = jobId;
        JobType = jobType;
        TotalItems = totalItems;
    }
}

public sealed class BulkJobCompletedEvent : DomainEvent
{
    public Guid JobId { get; }
    public string JobType { get; }
    public int SuccessCount { get; }
    public int FailureCount { get; }

    public BulkJobCompletedEvent(Guid jobId, string jobType, int successCount, int failureCount)
    {
        JobId = jobId;
        JobType = jobType;
        SuccessCount = successCount;
        FailureCount = failureCount;
    }
}

public sealed class BulkJobFailedEvent : DomainEvent
{
    public Guid JobId { get; }
    public string JobType { get; }
    public string ErrorDetails { get; }

    public BulkJobFailedEvent(Guid jobId, string jobType, string errorDetails)
    {
        JobId = jobId;
        JobType = jobType;
        ErrorDetails = errorDetails;
    }
}
