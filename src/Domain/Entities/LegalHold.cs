using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Legal hold preventing data purge
/// </summary>
public class LegalHold : Entity
{
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string IssuedBy { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string Status { get; private set; } = "Active";
    public string? ReleasedBy { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    protected LegalHold() { }

    public LegalHold(
        string entityType,
        Guid entityId,
        string reason,
        string issuedBy,
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required", nameof(entityType));
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity ID cannot be empty", nameof(entityId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required", nameof(reason));

        EntityType = entityType;
        EntityId = entityId;
        Reason = reason;
        IssuedBy = issuedBy;
        IssuedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        Status = "Active";

        AddDomainEvent(new LegalHoldAppliedEvent(Id, EntityType, EntityId, Reason, issuedBy));
    }

    public void Release(string releasedBy, string releaseReason)
    {
        if (Status != "Active")
            throw new InvalidOperationException("Legal hold is not active");

        Status = "Released";
        ReleasedBy = releasedBy;
        ReleasedAt = DateTime.UtcNow;

        AddDomainEvent(new LegalHoldReleasedEvent(Id, EntityType, EntityId, releaseReason, releasedBy));
    }
}
