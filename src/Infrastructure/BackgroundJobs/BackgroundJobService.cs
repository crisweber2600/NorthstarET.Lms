using Microsoft.EntityFrameworkCore;
using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Persistence;

namespace NorthstarET.Lms.Infrastructure.BackgroundJobs;

public class BackgroundJobService : IBulkOperationService
{
    private readonly LmsDbContext _context;

    public BackgroundJobService(LmsDbContext context)
    {
        _context = context;
    }

    public async Task<BulkJob> ExecuteBulkOperationAsync(
        string operationType,
        int totalRows,
        string errorStrategy,
        int? errorThreshold,
        bool isDryRun,
        string initiatedBy,
        CancellationToken cancellationToken = default)
    {
        // Get tenant slug from context
        var tenantSlug = "default-tenant";
        var threshold = errorThreshold.HasValue ? (decimal)errorThreshold.Value / 100m : 0.1m;
        
        var bulkJob = new BulkJob(tenantSlug, operationType, initiatedBy, totalRows, errorStrategy, threshold, isDryRun);

        await _context.BulkJobs.AddAsync(bulkJob, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return bulkJob;
    }

    public async Task UpdateProgressAsync(
        Guid jobId,
        int processedRows,
        int failedRows,
        CancellationToken cancellationToken = default)
    {
        var job = await _context.BulkJobs.FindAsync([jobId], cancellationToken);
        if (job != null)
        {
            // Update success and failure counts
            for (int i = 0; i < processedRows - failedRows; i++)
                job.RecordSuccess();
            for (int i = 0; i < failedRows; i++)
                job.RecordFailure("Error details");
                
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CompleteJobAsync(
        Guid jobId,
        string? errorDetails,
        CancellationToken cancellationToken = default)
    {
        var job = await _context.BulkJobs.FindAsync([jobId], cancellationToken);
        if (job != null)
        {
            if (string.IsNullOrEmpty(errorDetails))
            {
                job.Complete();
            }
            else
            {
                job.Fail(errorDetails);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<BulkJob> GetJobStatusAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _context.BulkJobs.FindAsync([jobId], cancellationToken);
        return job ?? throw new InvalidOperationException($"Bulk job {jobId} not found");
    }
}
