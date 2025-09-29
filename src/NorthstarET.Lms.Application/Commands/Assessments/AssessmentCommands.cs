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

public class UploadAssessmentVersionCommand
{
    public Guid AssessmentId { get; }
    public byte[] FileContent { get; }
    public string FileName { get; }
    public string UploadedBy { get; }

    public UploadAssessmentVersionCommand(Guid assessmentId, byte[] fileContent, string fileName, string uploadedBy)
    {
        AssessmentId = assessmentId;
        FileContent = fileContent;
        FileName = fileName;
        UploadedBy = uploadedBy;
    }
}

public class PinAssessmentCommand
{
    public Guid AssessmentId { get; }
    public Guid SchoolYearId { get; }
    public string PinnedBy { get; }

    public PinAssessmentCommand(Guid assessmentId, Guid schoolYearId, string pinnedBy)
    {
        AssessmentId = assessmentId;
        SchoolYearId = schoolYearId;
        PinnedBy = pinnedBy;
    }
}

public class ArchiveAssessmentCommand
{
    public Guid AssessmentId { get; }
    public string ArchivedBy { get; }
    public string Reason { get; }

    public ArchiveAssessmentCommand(Guid assessmentId, string archivedBy, string reason)
    {
        AssessmentId = assessmentId;
        ArchivedBy = archivedBy;
        Reason = reason;
    }
}