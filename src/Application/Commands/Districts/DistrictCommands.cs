using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Commands.Districts;

/// <summary>
/// Command to provision a new district tenant
/// </summary>
public record CreateDistrictCommand : Command<DistrictDto>
{
    /// <summary>
    /// Unique slug for the district
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Display name for the district
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Custom quotas (optional, uses defaults if not provided)
    /// </summary>
    public QuotaDto? CustomQuotas { get; init; }
}

/// <summary>
/// Command to update district status
/// </summary>
public record UpdateDistrictStatusCommand : Command
{
    /// <summary>
    /// District slug
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// New status
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Reason for status change (required for suspension)
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Command to update district quotas
/// </summary>
public record UpdateDistrictQuotasCommand : Command
{
    /// <summary>
    /// District slug
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// New quotas
    /// </summary>
    public required QuotaDto Quotas { get; init; }
}