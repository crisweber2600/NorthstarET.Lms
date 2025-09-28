using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Common;

namespace NorthstarET.Lms.Application.Interfaces;

public interface IStaffService
{
    Task<Result<StaffDto>> CreateStaffAsync(CreateStaffCommand command);
    Task<Result<StaffDto>> GetStaffAsync(Guid staffId);
    Task<Result<StaffDto>> UpdateStaffAsync(UpdateStaffCommand command);
    Task<Result> DeleteStaffAsync(Guid staffId, string deletedBy, string reason);
    Task<Result<PagedResult<StaffListDto>>> SearchStaffAsync(SearchStaffQuery query);
    Task<Result<StaffDetailDto>> GetStaffDetailAsync(Guid staffId);
    Task<Result> AssignStaffToClassAsync(Guid staffId, Guid classId, string role, string assignedBy);
    Task<Result> RemoveStaffFromClassAsync(Guid staffId, Guid classId, string removedBy);
}

public class CreateStaffCommand
{
    public CreateStaffDto Staff { get; }
    public string CreatedBy { get; }

    public CreateStaffCommand(CreateStaffDto staff, string createdBy)
    {
        Staff = staff;
        CreatedBy = createdBy;
    }
}

public class UpdateStaffCommand
{
    public Guid StaffId { get; }
    public UpdateStaffDto Staff { get; }
    public string UpdatedBy { get; }

    public UpdateStaffCommand(Guid staffId, UpdateStaffDto staff, string updatedBy)
    {
        StaffId = staffId;
        Staff = staff;
        UpdatedBy = updatedBy;
    }
}

public class SearchStaffQuery
{
    public int Page { get; }
    public int Size { get; }
    public string? SearchTerm { get; }
    public Guid? SchoolId { get; }
    public string? Role { get; }

    public SearchStaffQuery(int page, int size, string? searchTerm = null, Guid? schoolId = null, string? role = null)
    {
        Page = Math.Max(1, page);
        Size = Math.Min(100, Math.Max(1, size));
        SearchTerm = searchTerm;
        SchoolId = schoolId;
        Role = role;
    }
}