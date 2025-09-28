using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Application.DTOs.Districts;

public class CreateDistrictDto
{
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DistrictQuotasDto Quotas { get; set; } = new();
}

public class DistrictQuotasDto
{
    public int MaxStudents { get; set; }
    public int MaxStaff { get; set; }
    public int MaxAdmins { get; set; }
}

public class DistrictDto
{
    public Guid DistrictId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DistrictStatus Status { get; set; }
    public DistrictQuotasDto Quotas { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

public class DistrictListDto
{
    public Guid DistrictId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DistrictStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public int SchoolCount { get; set; }
    public int StudentCount { get; set; }
    public int StaffCount { get; set; }
}

public class DistrictQuotaStatusDto
{
    public DistrictQuotasDto Quotas { get; set; } = new();
    public DistrictUsageDto Usage { get; set; } = new();
    public Dictionary<string, double> UtilizationPercentages { get; set; } = new();
}

public class DistrictUsageDto
{
    public int CurrentStudents { get; set; }
    public int CurrentStaff { get; set; }
    public int CurrentAdmins { get; set; }
}