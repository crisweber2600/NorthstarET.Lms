using NorthstarET.Lms.Application.DTOs.Districts;

namespace NorthstarET.Lms.Application.Commands.Districts;

public class CreateDistrictCommand
{
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DistrictQuotasDto? Quotas { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateDistrictQuotasCommand
{
    public Guid Id { get; set; }
    public DistrictQuotasDto Quotas { get; set; } = new();
    public string UpdatedBy { get; set; } = string.Empty;
}

public class SuspendDistrictCommand
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SuspendedBy { get; set; } = string.Empty;
}

public class ReactivateDistrictCommand
{
    public Guid Id { get; set; }
    public string ReactivatedBy { get; set; } = string.Empty;
}

public class UpdateDistrictCommand
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public DistrictQuotasDto? Quotas { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class DeleteDistrictCommand
{
    public Guid Id { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}