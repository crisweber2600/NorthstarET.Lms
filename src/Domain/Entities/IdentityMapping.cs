using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Maps external identity to internal user ID
/// </summary>
public class IdentityMapping : Entity
{
    public Guid InternalUserId { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string Issuer { get; private set; } = string.Empty;
    public DateTime MappedAt { get; private set; }
    public string Status { get; private set; } = "Active";
    public string CreatedBy { get; private set; } = string.Empty;

    protected IdentityMapping() { }

    public IdentityMapping(
        Guid internalUserId,
        string externalId,
        string issuer,
        string createdBy)
    {
        if (internalUserId == Guid.Empty)
            throw new ArgumentException("Internal user ID cannot be empty", nameof(internalUserId));
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External ID is required", nameof(externalId));
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("Issuer is required", nameof(issuer));

        InternalUserId = internalUserId;
        ExternalId = externalId;
        Issuer = issuer;
        MappedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
        Status = "Active";

        AddDomainEvent(new IdentityMappingCreatedEvent(Id, InternalUserId, ExternalId, Issuer, createdBy));
    }

    public void Suspend(string reason, string suspendedBy)
    {
        Status = "Suspended";
        AddDomainEvent(new IdentityMappingSuspendedEvent(Id, InternalUserId, ExternalId, Issuer, reason, suspendedBy));
    }
}
