using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Domain.Entities;

public class AuditRecord : TenantScopedEntity
{
    // Private constructor for EF Core
    private AuditRecord() { }

    public AuditRecord(
        string action,
        string entityType,
        Guid entityId,
        string userId,
        object? changeDetails = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required", nameof(action));
            
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required", nameof(entityType));
            
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
            
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity ID cannot be empty", nameof(entityId));

        // Convert action string to enum with better mapping
        Action = action; // Store original action string
        EventType = action.ToUpperInvariant() switch
        {
            var a when a.StartsWith("CREATE") => AuditEventType.Create,
            var a when a.StartsWith("UPDATE") => AuditEventType.Update,
            var a when a.StartsWith("DELETE") => AuditEventType.Delete,
            var a when a.Contains("ROLE") && a.Contains("ASSIGN") => AuditEventType.RoleAssigned,
            var a when a.Contains("ROLE") && a.Contains("REVOKE") => AuditEventType.RoleRevoked,
            var a when a.Contains("LOGIN") => AuditEventType.LoginAttempt,
            var a when a.Contains("BULK") => AuditEventType.BulkOperation,
            var a when a.Contains("SECURITY") || a.Contains("VIOLATION") => AuditEventType.SecurityViolation,
            var a when a.Contains("PURGE") => AuditEventType.DataPurged,
            _ => Enum.TryParse<AuditEventType>(action.Replace("_", ""), true, out var parsed) ? parsed : AuditEventType.Update
        };
        
        EntityType = entityType;
        EntityId = entityId;
        UserId = userId;
        Timestamp = DateTime.UtcNow;
        ChangeDetails = changeDetails != null ? JsonSerializer.Serialize(changeDetails) : null;
        IpAddress = ipAddress;
        AdditionalMetadata = userAgent != null ? JsonSerializer.Serialize(new { UserAgent = userAgent }) : null;
    }

    public AuditRecord(
        AuditEventType eventType,
        string entityType,
        Guid entityId,
        string userId,
        object? changeDetails = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required", nameof(entityType));
            
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
            
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity ID cannot be empty", nameof(entityId));

        EventType = eventType;
        Action = eventType.ToString(); // Set action from enum
        EntityType = entityType;
        EntityId = entityId;
        UserId = userId;
        Timestamp = DateTime.UtcNow;
        ChangeDetails = changeDetails != null ? JsonSerializer.Serialize(changeDetails) : null;
    }

    public AuditEventType EventType { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public string? ChangeDetails { get; private set; }
    public string? RecordHash { get; private set; }
    public string? PreviousRecordHash { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public string? AdditionalMetadata { get; private set; }
    
    // Additional properties expected by application services
    public string Action { get; private set; } = string.Empty;
    public string? Details => ChangeDetails;
    public string? IpAddress { get; private set; }
    public string Hash => RecordHash ?? string.Empty;
    
    // Audit chain properties for infrastructure layer
    public long SequenceNumber { get; private set; }

    public bool IsSecurityEvent => EventType == AuditEventType.SecurityViolation;
    public bool IsPartOfBulkOperation => CorrelationId.HasValue;

    public void SetHashChain(string? previousHash)
    {
        if (!string.IsNullOrEmpty(RecordHash))
            throw new InvalidOperationException("Audit record hash chain already set and cannot be modified");

        PreviousRecordHash = previousHash;
        RecordHash = CalculateHash(previousHash);
    }

    public void SetHash(string hash)
    {
        if (!string.IsNullOrEmpty(RecordHash))
            throw new InvalidOperationException("Audit record hash already set and cannot be modified");
            
        RecordHash = hash;
    }
    
    // Methods needed by audit chain integrity service
    public void SetSequenceNumber(long sequenceNumber)
    {
        if (SequenceNumber != 0)
            throw new InvalidOperationException("Sequence number already set and cannot be modified");
            
        SequenceNumber = sequenceNumber;
    }
    
    public void SetPreviousRecordHash(string? previousHash)
    {
        if (!string.IsNullOrEmpty(PreviousRecordHash))
            throw new InvalidOperationException("Previous record hash already set and cannot be modified");
            
        PreviousRecordHash = previousHash;
    }
    
    public void SetRecordHash(string hash)
    {
        if (!string.IsNullOrEmpty(RecordHash))
            throw new InvalidOperationException("Record hash already set and cannot be modified");
            
        RecordHash = hash;
    }

    public bool VerifyIntegrity(string? expectedPreviousHash)
    {
        if (string.IsNullOrEmpty(RecordHash))
            return false;

        var calculatedHash = CalculateHash(expectedPreviousHash);
        return RecordHash == calculatedHash && PreviousRecordHash == expectedPreviousHash;
    }

    public void AddCorrelationId(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public void AddMetadata(object metadata)
    {
        AdditionalMetadata = JsonSerializer.Serialize(metadata);
    }

    public string GetAuditSummary()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {EventType} on {EntityType} by {UserId}";
    }

    public string ToComplianceReport()
    {
        return JsonSerializer.Serialize(new
        {
            Id = Id.ToString(),
            EventType = EventType.ToString(),
            EntityType,
            EntityId = EntityId.ToString(),
            UserId,
            Timestamp = Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff UTC"),
            RecordHash,
            PreviousRecordHash,
            CorrelationId = CorrelationId?.ToString(),
            TamperEvident = !string.IsNullOrEmpty(RecordHash),
            ChangeDetails,
            AdditionalMetadata
        });
    }

    private string CalculateHash(string? previousHash)
    {
        var hashInput = $"{Id}|{EventType}|{EntityType}|{EntityId}|{UserId}|{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{ChangeDetails}|{previousHash}";
        
        if (string.IsNullOrEmpty(previousHash))
        {
            // Genesis record
            return $"genesis-{ComputeSha256Hash(hashInput)}";
        }

        return ComputeSha256Hash(hashInput);
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes).ToLowerInvariant();
    }
}