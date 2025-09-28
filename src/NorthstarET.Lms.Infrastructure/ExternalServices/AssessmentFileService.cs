using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using NorthstarET.Lms.Application.Interfaces;
using System.Security.Cryptography;

namespace NorthstarET.Lms.Infrastructure.ExternalServices;

/// <summary>
/// Implementation of assessment file storage using Azure Blob Storage
/// </summary>
public class AssessmentFileService : IAssessmentFileService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AssessmentFileService> _logger;
    private const string AssessmentContainerName = "assessments";

    public AssessmentFileService(
        BlobServiceClient blobServiceClient,
        ILogger<AssessmentFileService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string?> StoreAssessmentFileAsync(
        string fileName,
        byte[] fileContent,
        string contentType,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = await GetOrCreateContainerAsync(tenantId, cancellationToken);
            
            // Generate unique blob name with tenant prefix
            var blobName = $"{tenantId}/{Guid.NewGuid()}/{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Calculate file hash for integrity verification
            using var sha256 = SHA256.Create();
            var fileHash = Convert.ToBase64String(sha256.ComputeHash(fileContent));

            // Set blob metadata
            var metadata = new Dictionary<string, string>
            {
                ["OriginalFileName"] = fileName,
                ["TenantId"] = tenantId,
                ["FileHash"] = fileHash,
                ["UploadedAt"] = DateTime.UtcNow.ToString("O"),
                ["FileSize"] = fileContent.Length.ToString()
            };

            // Upload with server-side encryption
            using var stream = new MemoryStream(fileContent);
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    ContentHash = sha256.ComputeHash(fileContent)
                },
                Metadata = metadata,
                AccessTier = AccessTier.Hot // For frequently accessed assessment files
            };

            var result = await blobClient.UploadAsync(stream, blobUploadOptions, cancellationToken);

            _logger.LogInformation("Successfully stored assessment file {FileName} for tenant {TenantId}. Blob: {BlobName}", 
                fileName, tenantId, blobName);

            return blobName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store assessment file {FileName} for tenant {TenantId}", fileName, tenantId);
            return null;
        }
    }

    public async Task<(byte[]? Content, string? ContentType, string? FileName)> RetrieveAssessmentFileAsync(
        string blobName,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access to blob
            if (!blobName.StartsWith($"{tenantId}/"))
            {
                _logger.LogWarning("Attempted unauthorized access to blob {BlobName} by tenant {TenantId}", blobName, tenantId);
                return (null, null, null);
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(AssessmentContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Assessment file {BlobName} not found for tenant {TenantId}", blobName, tenantId);
                return (null, null, null);
            }

            // Download blob content
            var downloadResponse = await blobClient.DownloadContentAsync(cancellationToken);
            var content = downloadResponse.Value.Content.ToArray();

            // Get blob properties and metadata
            var properties = await blobClient.GetPropertiesAsync(cancellationToken);
            var contentType = properties.Value.ContentType;
            var originalFileName = properties.Value.Metadata.GetValueOrDefault("OriginalFileName", "unknown");

            // Verify file integrity if hash is available
            if (properties.Value.Metadata.TryGetValue("FileHash", out var expectedHash))
            {
                using var sha256 = SHA256.Create();
                var actualHash = Convert.ToBase64String(sha256.ComputeHash(content));
                
                if (expectedHash != actualHash)
                {
                    _logger.LogError("File integrity check failed for {BlobName}. Expected: {ExpectedHash}, Actual: {ActualHash}", 
                        blobName, expectedHash, actualHash);
                    return (null, null, null);
                }
            }

            _logger.LogInformation("Successfully retrieved assessment file {BlobName} for tenant {TenantId}", blobName, tenantId);

            return (content, contentType, originalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve assessment file {BlobName} for tenant {TenantId}", blobName, tenantId);
            return (null, null, null);
        }
    }

    public async Task<bool> DeleteAssessmentFileAsync(string blobName, string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access to blob
            if (!blobName.StartsWith($"{tenantId}/"))
            {
                _logger.LogWarning("Attempted unauthorized deletion of blob {BlobName} by tenant {TenantId}", blobName, tenantId);
                return false;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(AssessmentContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var deleteResponse = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

            if (deleteResponse.Value)
            {
                _logger.LogInformation("Successfully deleted assessment file {BlobName} for tenant {TenantId}", blobName, tenantId);
            }
            else
            {
                _logger.LogWarning("Assessment file {BlobName} was not found for deletion by tenant {TenantId}", blobName, tenantId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete assessment file {BlobName} for tenant {TenantId}", blobName, tenantId);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string blobName, string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access to blob
            if (!blobName.StartsWith($"{tenantId}/"))
            {
                return false;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(AssessmentContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.ExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check existence of assessment file {BlobName} for tenant {TenantId}", blobName, tenantId);
            return false;
        }
    }

    public async Task<long> GetFileSizeAsync(string blobName, string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access to blob
            if (!blobName.StartsWith($"{tenantId}/"))
            {
                return 0;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(AssessmentContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var properties = await blobClient.GetPropertiesAsync(cancellationToken);
            return properties.Value.ContentLength;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get size of assessment file {BlobName} for tenant {TenantId}", blobName, tenantId);
            return 0;
        }
    }

    public async Task<string> GenerateDownloadUrlAsync(
        string blobName, 
        string tenantId, 
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate tenant access to blob
            if (!blobName.StartsWith($"{tenantId}/"))
            {
                throw new UnauthorizedAccessException($"Tenant {tenantId} does not have access to blob {blobName}");
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(AssessmentContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Assessment file {blobName} not found");
            }

            // Generate SAS token for secure, time-limited access
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = AssessmentContainerName,
                    BlobName = blobName,
                    Resource = "b", // Blob resource
                    ExpiresOn = DateTimeOffset.UtcNow.Add(expiration)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }

            throw new InvalidOperationException("Unable to generate SAS token for blob access");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate download URL for assessment file {BlobName} for tenant {TenantId}", blobName, tenantId);
            throw;
        }
    }

    private async Task<BlobContainerClient> GetOrCreateContainerAsync(string tenantId, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(AssessmentContainerName);

        // Create container if it doesn't exist
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        // Set container metadata to include tenant information
        var metadata = new Dictionary<string, string>
        {
            ["Purpose"] = "AssessmentStorage",
            ["CreatedAt"] = DateTime.UtcNow.ToString("O")
        };

        try
        {
            await containerClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
        }
        catch
        {
            // Metadata update failure is not critical
        }

        return containerClient;
    }

    // IAssessmentFileService interface implementations
    public async Task<string> StoreAssessmentFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Read stream to byte array
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            var fileContent = memoryStream.ToArray();

            // Use default tenant ID for now - this should come from tenant context
            var tenantId = "default";
            var result = await StoreAssessmentFileAsync(fileName, fileContent, contentType, tenantId, cancellationToken);
            
            return result ?? throw new InvalidOperationException("Failed to store assessment file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store assessment file {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream> RetrieveAssessmentFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use default tenant ID for now - this should come from tenant context
            var tenantId = "default";
            var (content, contentType, fileName) = await RetrieveAssessmentFileAsync(fileId, tenantId, cancellationToken);
            
            if (content == null)
                throw new FileNotFoundException($"Assessment file {fileId} not found");

            return new MemoryStream(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve assessment file {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> DeleteAssessmentFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use default tenant ID for now - this should come from tenant context
            var tenantId = "default";
            return await DeleteAssessmentFileAsync(fileId, tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete assessment file {FileId}", fileId);
            return false;
        }
    }

    public async Task<string> GenerateAccessUrlAsync(string fileId, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use default tenant ID for now - this should come from tenant context
            var tenantId = "default";
            return await GenerateDownloadUrlAsync(fileId, tenantId, expiration, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate access URL for assessment file {FileId}", fileId);
            throw;
        }
    }
}