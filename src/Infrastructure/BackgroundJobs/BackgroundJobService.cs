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
        var bulkJob = new BulkJob(operationType, totalRows, initiatedBy, isDryRun);
        bulkJob.SetAuditFields(initiatedBy, DateTimeOffset.UtcNow);

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
            job.UpdateProgress(processedRows, failedRows, null);
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
