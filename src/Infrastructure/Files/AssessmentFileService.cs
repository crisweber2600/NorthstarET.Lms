using Azure.Storage.Blobs;
using NorthstarET.Lms.Application.Abstractions;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Infrastructure.Persistence;

namespace NorthstarET.Lms.Infrastructure.Files;

public class AssessmentFileService : IAssessmentService
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly LmsDbContext _context;
    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB

    public AssessmentFileService(LmsDbContext context, BlobServiceClient? blobServiceClient = null)
    {
        _context = context;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<AssessmentDefinition> CreateAssessmentAsync(
        string title,
        string fileKey,
        long fileSizeBytes,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var assessment = new AssessmentDefinition(title, fileKey, fileSizeBytes, createdBy);
        assessment.SetAuditFields(createdBy, DateTimeOffset.UtcNow);

        await _context.AssessmentDefinitions.AddAsync(assessment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return assessment;
    }

    public async Task PublishAssessmentAsync(
        Guid assessmentId,
        string publishedBy,
        CancellationToken cancellationToken = default)
    {
        var assessment = await _context.AssessmentDefinitions.FindAsync([assessmentId], cancellationToken);
        if (assessment != null)
        {
            assessment.Publish(publishedBy);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string> GetAssessmentFileUrlAsync(
        Guid assessmentId,
        TimeSpan urlExpiration,
        CancellationToken cancellationToken = default)
    {
        var assessment = await _context.AssessmentDefinitions.FindAsync([assessmentId], cancellationToken);
        if (assessment == null)
        {
            throw new InvalidOperationException($"Assessment {assessmentId} not found");
        }

        // Return a mock URL for now - in production this would generate a SAS token from Azure Storage
        return $"https://storage.blob.core.windows.net/assessments/{assessment.FileKey}?expires={DateTimeOffset.UtcNow.Add(urlExpiration):O}";
    }

    public async Task<AssessmentDefinition> CreateNewVersionAsync(
        Guid existingAssessmentId,
        string fileKey,
        long fileSizeBytes,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var existingAssessment = await _context.AssessmentDefinitions.FindAsync([existingAssessmentId], cancellationToken);
        if (existingAssessment == null)
        {
            throw new InvalidOperationException($"Assessment {existingAssessmentId} not found");
        }

        var newVersion = new AssessmentDefinition(existingAssessment.Title, fileKey, fileSizeBytes, createdBy);
        newVersion.SetAuditFields(createdBy, DateTimeOffset.UtcNow);

        await _context.AssessmentDefinitions.AddAsync(newVersion, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return newVersion;
    }
}
