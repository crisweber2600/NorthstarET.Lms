using NorthstarET.Lms.Application.DTOs.Assessments;

namespace NorthstarET.Lms.Application.Commands.Assessments;

public class CreateAssessmentCommand
{
    public CreateAssessmentDto Assessment { get; }
    public string CreatedBy { get; }

    public CreateAssessmentCommand(CreateAssessmentDto assessment, string createdBy)
    {
        Assessment = assessment;
        CreatedBy = createdBy;
    }
}

public class UpdateAssessmentCommand
{
    public Guid AssessmentId { get; }
    public UpdateAssessmentDto Assessment { get; }
    public string UpdatedBy { get; }

    public UpdateAssessmentCommand(Guid assessmentId, UpdateAssessmentDto assessment, string updatedBy)
    {
        AssessmentId = assessmentId;
        Assessment = assessment;
        UpdatedBy = updatedBy;
    }
}

public class DeleteAssessmentCommand
{
    public Guid AssessmentId { get; }
    public string DeletedBy { get; }
    public string Reason { get; }

    public DeleteAssessmentCommand(Guid assessmentId, string deletedBy, string reason)
    {
        AssessmentId = assessmentId;
        DeletedBy = deletedBy;
        Reason = reason;
    }
}