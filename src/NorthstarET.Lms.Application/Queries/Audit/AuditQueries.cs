using NorthstarET.Lms.Application.DTOs.Audit;

namespace NorthstarET.Lms.Application.Queries.Audit;

public class GetAuditRecordsQuery
{
    public int Page { get; } = 1;
    public int Size { get; } = 20;
    public AuditQueryFilterDto Filters { get; }

    public GetAuditRecordsQuery(int page, int size, AuditQueryFilterDto filters)
    {
        Page = Math.Max(1, page);
        Size = Math.Min(100, Math.Max(1, size));
        Filters = filters;
    }
}

public class ExportAuditRecordsQuery
{
    public AuditQueryFilterDto Filters { get; }
    public string Format { get; } = "csv";
    public string RequestedBy { get; }

    public ExportAuditRecordsQuery(AuditQueryFilterDto filters, string format, string requestedBy)
    {
        Filters = filters;
        Format = format.ToLowerInvariant();
        RequestedBy = requestedBy;
    }
}

public class ValidateAuditIntegrityQuery
{
    public DateTime? FromDate { get; }
    public DateTime? ToDate { get; }
    public string ValidatedBy { get; }

    public ValidateAuditIntegrityQuery(DateTime? fromDate, DateTime? toDate, string validatedBy)
    {
        FromDate = fromDate;
        ToDate = toDate;
        ValidatedBy = validatedBy;
    }
}