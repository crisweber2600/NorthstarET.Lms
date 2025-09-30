using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a bulk operation job
/// </summary>
public class BulkJob : TenantScopedEntity
{
    public string JobType { get; private set; } = string.Empty;
    public string RequestedBy { get; private set; } = string.Empty;
    public DateTime RequestedAt { get; private set; }
    public string Status { get; private set; } = "Pending";
    public string ErrorHandlingMode { get; private set; } = "StopOnError";
    public int Progress { get; private set; }
    public int TotalItems { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public decimal FailureThreshold { get; private set; }
    public bool IsDryRun { get; private set; }
    public string? ErrorDetails { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    protected BulkJob() { }

    public BulkJob(
        string tenantSlug,
        string jobType,
        string requestedBy,
        int totalItems,
        string errorHandlingMode = "StopOnError",
        decimal failureThreshold = 0.1m,
        bool isDryRun = false)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug is required", nameof(tenantSlug));
        if (string.IsNullOrWhiteSpace(jobType))
            throw new ArgumentException("Job type is required", nameof(jobType));
        if (totalItems <= 0)
            throw new ArgumentException("Total items must be greater than 0", nameof(totalItems));

        InitializeTenant(tenantSlug);
        JobType = jobType;
        RequestedBy = requestedBy;
        RequestedAt = DateTime.UtcNow;
        TotalItems = totalItems;
        ErrorHandlingMode = errorHandlingMode;
        FailureThreshold = failureThreshold;
        IsDryRun = isDryRun;
        Status = "Pending";

        AddDomainEvent(new BulkJobCreatedEvent(Id, JobType, TotalItems, IsDryRun, requestedBy));
    }

    public void Start()
    {
        if (Status != "Pending")
            throw new InvalidOperationException("Can only start pending jobs");
        
        Status = "Running";
        AddDomainEvent(new BulkJobStartedEvent(Id, JobType, TotalItems));
    }

    public void RecordSuccess()
    {
        SuccessCount++;
        Progress = SuccessCount + FailureCount;
    }

    public void RecordFailure(string error)
    {
        FailureCount++;
        Progress = SuccessCount + FailureCount;
        
        // Check failure threshold
        if (ErrorHandlingMode == "ThresholdMode" && 
            (decimal)FailureCount / TotalItems > FailureThreshold)
        {
            Fail($"Failure threshold exceeded: {FailureCount}/{TotalItems}. {error}");
        }
    }

    public void Complete()
    {
        if (Status != "Running")
            throw new InvalidOperationException("Can only complete running jobs");

        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
        AddDomainEvent(new BulkJobCompletedEvent(Id, JobType, SuccessCount, FailureCount));
    }

    public void Fail(string errorDetails)
    {
        Status = "Failed";
        ErrorDetails = errorDetails;
        CompletedAt = DateTime.UtcNow;
        AddDomainEvent(new BulkJobFailedEvent(Id, JobType, errorDetails));
    }
}
