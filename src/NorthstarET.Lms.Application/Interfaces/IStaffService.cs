using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Common;

namespace NorthstarET.Lms.Application.Interfaces;

public interface IStaffService
{
    Task<Result<StaffDto>> CreateStaffAsync(NorthstarET.Lms.Application.Commands.CreateStaffCommand command);
    Task<Result<StaffDto>> GetStaffAsync(Guid staffId);
    Task<Result<StaffDto>> UpdateStaffAsync(NorthstarET.Lms.Application.Commands.UpdateStaffCommand command);
    Task<Result> DeleteStaffAsync(Guid staffId, string deletedBy, string reason);
    Task<Result<PagedResult<StaffListDto>>> SearchStaffAsync(SearchStaffQuery query);
    Task<Result<StaffDetailDto>> GetStaffDetailAsync(Guid staffId);
    Task<Result> AssignStaffToClassAsync(Guid staffId, Guid classId, string role, string assignedBy);
    Task<Result> RemoveStaffFromClassAsync(Guid staffId, Guid classId, string removedBy);
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