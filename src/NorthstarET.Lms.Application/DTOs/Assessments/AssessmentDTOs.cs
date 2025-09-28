using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.DTOs.Assessments;

public class CreateAssessmentDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public Guid? PinnedSchoolYearId { get; set; }
    public string PdfFileName { get; set; } = string.Empty;
}

public class UpdateAssessmentDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public Guid? PinnedSchoolYearId { get; set; }
}

public class AssessmentDto
{
    public Guid AssessmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsImmutable { get; set; }
    public Guid? PinnedSchoolYearId { get; set; }
    public string? PdfFileUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime LastModifiedDate { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
}

public class AssessmentListDto
{
    public Guid AssessmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsImmutable { get; set; }
    public Guid? PinnedSchoolYearId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class AssessmentPdfUploadDto
{
    public Guid AssessmentId { get; set; }
    public byte[] PdfContent { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
}