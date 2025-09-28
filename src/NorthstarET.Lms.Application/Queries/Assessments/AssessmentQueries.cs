namespace NorthstarET.Lms.Application.Queries.Assessments;

public class GetAssessmentQuery
{
    public Guid AssessmentId { get; }

    public GetAssessmentQuery(Guid assessmentId)
    {
        AssessmentId = assessmentId;
    }
}

public class GetAssessmentsQuery
{
    public int Page { get; } = 1;
    public int Size { get; } = 20;
    public Guid? SchoolYearId { get; }
    public string? SearchTerm { get; }
    public bool? IsImmutable { get; }

    public GetAssessmentsQuery(int page, int size, Guid? schoolYearId = null, string? searchTerm = null, bool? isImmutable = null)
    {
        Page = Math.Max(1, page);
        Size = Math.Min(100, Math.Max(1, size));
        SchoolYearId = schoolYearId;
        SearchTerm = searchTerm;
        IsImmutable = isImmutable;
    }
}

public class GetAssessmentPdfQuery
{
    public Guid AssessmentId { get; }
    public string RequestedBy { get; }

    public GetAssessmentPdfQuery(Guid assessmentId, string requestedBy)
    {
        AssessmentId = assessmentId;
        RequestedBy = requestedBy;
    }
}