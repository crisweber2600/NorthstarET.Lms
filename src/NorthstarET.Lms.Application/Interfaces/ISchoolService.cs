using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Common;

namespace NorthstarET.Lms.Application.Interfaces;

public interface ISchoolService
{
    Task<Result<SchoolDto>> CreateSchoolAsync(CreateSchoolCommand command);
    Task<Result<SchoolDto>> GetSchoolAsync(Guid schoolId);
    Task<Result<SchoolDto>> UpdateSchoolAsync(UpdateSchoolCommand command);
    Task<Result> DeleteSchoolAsync(Guid schoolId, string deletedBy, string reason);
    Task<Result<PagedResult<SchoolDto>>> SearchSchoolsAsync(SearchSchoolsQuery query);
}

public class CreateSchoolCommand
{
    public CreateSchoolDto School { get; }
    public string CreatedBy { get; }

    public CreateSchoolCommand(CreateSchoolDto school, string createdBy)
    {
        School = school;
        CreatedBy = createdBy;
    }
}

public class UpdateSchoolCommand
{
    public Guid SchoolId { get; }
    public UpdateSchoolDto School { get; }
    public string UpdatedBy { get; }

    public UpdateSchoolCommand(Guid schoolId, UpdateSchoolDto school, string updatedBy)
    {
        SchoolId = schoolId;
        School = school;
        UpdatedBy = updatedBy;
    }
}

public class SearchSchoolsQuery
{
    public int Page { get; }
    public int Size { get; }
    public string? SearchTerm { get; }

    public SearchSchoolsQuery(int page, int size, string? searchTerm = null)
    {
        Page = Math.Max(1, page);
        Size = Math.Min(100, Math.Max(1, size));
        SearchTerm = searchTerm;
    }
}