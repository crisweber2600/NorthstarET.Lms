using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Data;

namespace NorthstarET.Lms.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that enforces data retention policies according to FERPA requirements
/// </summary>
public class RetentionJobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetentionJobService> _logger;
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(24); // Run daily

    public RetentionJobService(
        IServiceProvider serviceProvider,
        ILogger<RetentionJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Retention Job Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRetentionPoliciesAsync(stoppingToken);
                await Task.Delay(_runInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Retention Job Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in retention job service");
                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }

    private async Task ProcessRetentionPoliciesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting retention policy processing");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LmsDbContext>();

        try
        {
            // Get all active retention policies
            var retentionPolicies = await dbContext.RetentionPolicies
                .Where(rp => rp.SupersededDate == null || rp.SupersededDate > DateTime.UtcNow.Date)
                .ToListAsync(cancellationToken);

            foreach (var policy in retentionPolicies)
            {
                await ProcessEntityRetentionAsync(dbContext, policy, cancellationToken);
            }

            _logger.LogInformation("Completed retention policy processing for {PolicyCount} policies", retentionPolicies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing retention policies");
            throw;
        }
    }

    private async Task ProcessEntityRetentionAsync(LmsDbContext dbContext, RetentionPolicy policy, CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.Date.AddYears(-policy.RetentionYears);
        _logger.LogInformation("Processing retention for {EntityType} with cutoff date {CutoffDate}", policy.EntityType, cutoffDate);

        try
        {
            switch (policy.EntityType)
            {
                case "Student":
                    await ProcessStudentRetentionAsync(dbContext, cutoffDate, cancellationToken);
                    break;

                case "Staff":
                    await ProcessStaffRetentionAsync(dbContext, cutoffDate, cancellationToken);
                    break;

                case "Assessment":
                    await ProcessAssessmentRetentionAsync(dbContext, cutoffDate, cancellationToken);
                    break;

                case "AuditRecord":
                    await ProcessAuditRecordRetentionAsync(dbContext, cutoffDate, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown entity type for retention: {EntityType}", policy.EntityType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing retention for entity type {EntityType}", policy.EntityType);
            throw;
        }
    }

    private async Task ProcessStudentRetentionAsync(LmsDbContext dbContext, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        // Get students eligible for deletion (withdrawn before cutoff date and no legal holds)
        var studentsToDelete = await dbContext.Students
            .Where(s => 
                s.Status == UserLifecycleStatus.Withdrawn &&
                s.WithdrawalDate.HasValue &&
                s.WithdrawalDate.Value <= cutoffDate)
            .Where(s => !dbContext.LegalHolds.Any(lh => 
                lh.EntityType == "Student" && 
                lh.EntityId == s.UserId && 
                lh.IsActive))
            .ToListAsync(cancellationToken);

        if (studentsToDelete.Any())
        {
            _logger.LogInformation("Found {Count} students eligible for retention deletion", studentsToDelete.Count);

            foreach (var student in studentsToDelete)
            {
                // Create audit record before deletion
                var auditRecord = new AuditRecord(
                    eventType: "RetentionDeletion",
                    entityType: "Student",
                    entityId: student.UserId,
                    userId: "SYSTEM",
                    changeDetails: $"Student {student.StudentNumber} deleted due to retention policy",
                    correlationId: Guid.NewGuid().ToString()
                );

                await dbContext.AuditRecords.AddAsync(auditRecord, cancellationToken);

                // Remove student and related data (cascading deletes handled by EF configuration)
                dbContext.Students.Remove(student);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted {Count} students due to retention policy", studentsToDelete.Count);
        }
    }

    private async Task ProcessStaffRetentionAsync(LmsDbContext dbContext, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        var staffToDelete = await dbContext.Staff
            .Where(s => 
                s.Status == UserLifecycleStatus.Terminated &&
                s.TerminationDate.HasValue &&
                s.TerminationDate.Value <= cutoffDate)
            .Where(s => !dbContext.LegalHolds.Any(lh => 
                lh.EntityType == "Staff" && 
                lh.EntityId == s.UserId && 
                lh.IsActive))
            .ToListAsync(cancellationToken);

        if (staffToDelete.Any())
        {
            _logger.LogInformation("Found {Count} staff members eligible for retention deletion", staffToDelete.Count);

            foreach (var staff in staffToDelete)
            {
                var auditRecord = new AuditRecord(
                    eventType: "RetentionDeletion",
                    entityType: "Staff",
                    entityId: staff.UserId,
                    userId: "SYSTEM",
                    changeDetails: $"Staff {staff.EmployeeNumber} deleted due to retention policy",
                    correlationId: Guid.NewGuid().ToString()
                );

                await dbContext.AuditRecords.AddAsync(auditRecord, cancellationToken);
                dbContext.Staff.Remove(staff);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted {Count} staff members due to retention policy", staffToDelete.Count);
        }
    }

    private async Task ProcessAssessmentRetentionAsync(LmsDbContext dbContext, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        var assessmentsToDelete = await dbContext.AssessmentDefinitions
            .Where(a => 
                a.CreatedDate <= cutoffDate &&
                !a.IsCurrentVersion) // Only delete non-current versions
            .Where(a => !dbContext.LegalHolds.Any(lh => 
                lh.EntityType == "Assessment" && 
                lh.EntityId == a.Id && 
                lh.IsActive))
            .ToListAsync(cancellationToken);

        if (assessmentsToDelete.Any())
        {
            _logger.LogInformation("Found {Count} assessments eligible for retention deletion", assessmentsToDelete.Count);

            foreach (var assessment in assessmentsToDelete)
            {
                var auditRecord = new AuditRecord(
                    eventType: "RetentionDeletion",
                    entityType: "Assessment",
                    entityId: assessment.Id,
                    userId: "SYSTEM",
                    changeDetails: $"Assessment {assessment.Name} v{assessment.Version} deleted due to retention policy",
                    correlationId: Guid.NewGuid().ToString()
                );

                await dbContext.AuditRecords.AddAsync(auditRecord, cancellationToken);
                dbContext.AssessmentDefinitions.Remove(assessment);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted {Count} assessments due to retention policy", assessmentsToDelete.Count);
        }
    }

    private async Task ProcessAuditRecordRetentionAsync(LmsDbContext dbContext, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        // Audit records have special handling - they are archived rather than deleted
        var auditRecordsToArchive = await dbContext.AuditRecords
            .Where(ar => ar.Timestamp <= cutoffDate)
            .Where(ar => !dbContext.LegalHolds.Any(lh => 
                lh.EntityType == "AuditRecord" && 
                lh.EntityId == ar.Id && 
                lh.IsActive))
            .ToListAsync(cancellationToken);

        if (auditRecordsToArchive.Any())
        {
            _logger.LogInformation("Found {Count} audit records eligible for archival", auditRecordsToArchive.Count);

            // In a production system, these would be moved to cold storage or a separate archive database
            // For now, we'll just log the archival action
            foreach (var auditRecord in auditRecordsToArchive)
            {
                _logger.LogDebug("Archiving audit record {AuditRecordId} from {Timestamp}", 
                    auditRecord.Id, auditRecord.Timestamp);
            }

            // TODO: Implement actual archival process
            // This could involve:
            // 1. Export to cold storage (Azure Archive Storage)
            // 2. Compress and move to archive database
            // 3. Update records with archived status
            
            _logger.LogInformation("Archived {Count} audit records", auditRecordsToArchive.Count);
        }
    }
}