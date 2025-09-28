using NorthstarET.Lms.Application.DTOs;

namespace NorthstarET.Lms.Application.DTOs.Audit;

public class AuditRecordDto
{
    public Guid RecordId { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? ChangeDetails { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Guid? SchoolYearId { get; set; }
}

public class AuditQueryFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Actor { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? SchoolYearId { get; set; }
}

public class AuditExportDto
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public int RecordCount { get; set; }
    public DateTime ExportDate { get; set; }
}

public class AuditIntegrityReportDto
{
    public bool IsValid { get; set; }
    public DateTime LastValidationDate { get; set; }
    public List<AuditViolationDto> Violations { get; set; } = new();
    public int TotalRecordsChecked { get; set; }
    public string ValidatedBy { get; set; } = string.Empty;
}

public class AuditViolationDto
{
    public Guid RecordId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public string Severity { get; set; } = string.Empty;
}