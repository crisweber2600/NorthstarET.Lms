using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents an assessment definition with file storage
/// </summary>
public class AssessmentDefinition : TenantScopedEntity
{
    public Guid DistrictId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string GradeLevels { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? PinnedSchoolYearId { get; private set; }
    public string StorageUri { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string UploadDigest { get; private set; } = string.Empty;
    public bool IsPublished { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    protected AssessmentDefinition() { }

    public AssessmentDefinition(
        string tenantSlug,
        Guid districtId,
        string title,
        string subject,
        string gradeLevels,
        string storageUri,
        long fileSize,
        string uploadDigest,
        string createdBy,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug is required", nameof(tenantSlug));
        if (districtId == Guid.Empty)
            throw new ArgumentException("District ID cannot be empty", nameof(districtId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));
        if (fileSize > 100 * 1024 * 1024) // 100MB limit
            throw new ArgumentException("File size exceeds 100MB limit", nameof(fileSize));

        InitializeTenant(tenantSlug);
        DistrictId = districtId;
        Title = title;
        Version = 1;
        Subject = subject;
        GradeLevels = gradeLevels;
        Description = description;
        StorageUri = storageUri;
        FileSize = fileSize;
        UploadDigest = uploadDigest;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsPublished = false;

        AddDomainEvent(new AssessmentDefinitionCreatedEvent(Id, DistrictId, Title, Subject, createdBy));
    }

    public void Publish(string publishedBy)
    {
        if (IsPublished)
            throw new InvalidOperationException("Assessment is already published");

        IsPublished = true;
        AddDomainEvent(new AssessmentDefinitionPublishedEvent(Id, Title, Version, publishedBy));
    }
}
