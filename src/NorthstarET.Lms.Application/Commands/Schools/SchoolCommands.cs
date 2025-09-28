using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.Commands.Schools;

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

public class DeleteSchoolCommand
{
    public Guid SchoolId { get; }
    public string DeletedBy { get; }
    public string Reason { get; }

    public DeleteSchoolCommand(Guid schoolId, string deletedBy, string reason)
    {
        SchoolId = schoolId;
        DeletedBy = deletedBy;
        Reason = reason;
    }
}