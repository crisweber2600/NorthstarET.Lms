using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Immutable audit record with tamper-evident chaining
/// </summary>
public class AuditRecord : TenantScopedEntity
{
    public DateTime Timestamp { get; private set; }
    public Guid ActorId { get; private set; }
    public string ActorRole { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public string PreviousHash { get; private set; } = string.Empty;
    public string CurrentHash { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;

    protected AuditRecord() { }

    public AuditRecord(
        string tenantSlug,
        Guid actorId,
        string actorRole,
        string action,
        string entityType,
        Guid entityId,
        string payload,
        string previousHash,
        string correlationId)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug is required", nameof(tenantSlug));
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty", nameof(actorId));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required", nameof(action));

        InitializeTenant(tenantSlug);
        Timestamp = DateTime.UtcNow;
        ActorId = actorId;
        ActorRole = actorRole;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Payload = payload;
        PreviousHash = previousHash;
        CorrelationId = correlationId;
        
        // Compute SHA-256 hash from previousHash + Payload
        CurrentHash = ComputeHash(previousHash, payload);
    }

    private static string ComputeHash(string previousHash, string payload)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var input = $"{previousHash}|{payload}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
