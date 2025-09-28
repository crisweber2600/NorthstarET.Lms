using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;
using NorthstarET.Lms.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace NorthstarET.Lms.Domain.Entities;

public class DistrictTenant : TenantScopedEntity
{
    // Private constructor for EF Core
    private DistrictTenant() { }

    public DistrictTenant(
        string slug,
        string displayName,
        DistrictQuotas quotas,
        string createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("District slug is required", nameof(slug));
            
        if (!IsValidSlug(slug))
            throw new ArgumentException("Invalid slug format", nameof(slug));
            
        Slug = slug.ToLowerInvariant();
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Quotas = quotas ?? throw new ArgumentNullException(nameof(quotas));
        Status = DistrictStatus.Active;
        CreatedByUserId = createdByUserId ?? throw new ArgumentNullException(nameof(createdByUserId));
        
        // Domain event for audit trail
        AddDomainEvent(new DistrictProvisionedEvent(Id, slug, displayName, createdByUserId));
    }

    public string Slug { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DistrictStatus Status { get; private set; }
    public DistrictQuotas Quotas { get; private set; } = null!;
    public string CreatedByUserId { get; private set; } = string.Empty;

    private static bool IsValidSlug(string slug)
    {
        return Regex.IsMatch(slug, @"^[a-z0-9-]+$");
    }

    public void Suspend(string reason, string suspendedByUserId)
    {
        if (Status == DistrictStatus.Suspended)
            throw new InvalidOperationException("District is already suspended");
            
        Status = DistrictStatus.Suspended;
        MarkAsModified();
        AddDomainEvent(new DistrictSuspendedEvent(Id, reason, suspendedByUserId));
    }

    public void Reactivate(string reason, string reactivatedByUserId)
    {
        if (Status != DistrictStatus.Suspended)
            throw new InvalidOperationException("Only suspended districts can be reactivated");
            
        Status = DistrictStatus.Active;
        MarkAsModified();
        // Add reactivated event when implemented
    }

    public void UpdateQuotas(DistrictQuotas newQuotas, string updatedByUserId)
    {
        ArgumentNullException.ThrowIfNull(newQuotas);
        
        var oldQuotas = Quotas;
        Quotas = newQuotas;
        MarkAsModified();
        // Add quota updated event when implemented
    }
}