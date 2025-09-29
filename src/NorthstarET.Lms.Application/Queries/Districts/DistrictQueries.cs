namespace NorthstarET.Lms.Application.Queries.Districts;

public class GetDistrictQuery
{
    public Guid Id { get; set; }
}

public class ListDistrictsQuery
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
}

public class GetDistrictsQuery
{
    public int Page { get; } = 1;
    public int Size { get; } = 20;
    public string? SearchTerm { get; }
    public string? Status { get; }

    public GetDistrictsQuery(int page, int size, string? searchTerm = null, string? status = null)
    {
        Page = Math.Max(1, page);
        Size = Math.Min(100, Math.Max(1, size));
        SearchTerm = searchTerm;
        Status = status;
    }
}

public class GetDistrictQuotaStatusQuery
{
    public Guid DistrictId { get; set; }
}