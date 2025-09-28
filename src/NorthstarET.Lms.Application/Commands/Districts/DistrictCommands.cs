using NorthstarET.Lms.Application.DTOs.Districts;

namespace NorthstarET.Lms.Application.Commands.Districts;

public class CreateDistrictCommand
{
    public CreateDistrictDto District { get; }
    public string CreatedBy { get; }

    public CreateDistrictCommand(CreateDistrictDto district, string createdBy)
    {
        District = district;
        CreatedBy = createdBy;
    }
}

public class UpdateDistrictQuotasCommand
{
    public Guid DistrictId { get; }
    public DistrictQuotasDto Quotas { get; }
    public string UpdatedBy { get; }

    public UpdateDistrictQuotasCommand(Guid districtId, DistrictQuotasDto quotas, string updatedBy)
    {
        DistrictId = districtId;
        Quotas = quotas;
        UpdatedBy = updatedBy;
    }
}

public class SuspendDistrictCommand
{
    public Guid DistrictId { get; }
    public string Reason { get; }
    public string SuspendedBy { get; }

    public SuspendDistrictCommand(Guid districtId, string reason, string suspendedBy)
    {
        DistrictId = districtId;
        Reason = reason;
        SuspendedBy = suspendedBy;
    }
}

public class ReactivateDistrictCommand
{
    public Guid DistrictId { get; }
    public string ReactivatedBy { get; }

    public ReactivateDistrictCommand(Guid districtId, string reactivatedBy)
    {
        DistrictId = districtId;
        ReactivatedBy = reactivatedBy;
    }
}

public class DeleteDistrictCommand
{
    public Guid DistrictId { get; }
    public string DeletedBy { get; }
    public string Reason { get; }

    public DeleteDistrictCommand(Guid districtId, string deletedBy, string reason)
    {
        DistrictId = districtId;
        DeletedBy = deletedBy;
        Reason = reason;
    }
}