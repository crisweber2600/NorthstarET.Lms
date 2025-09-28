using NorthstarET.Lms.Application.Common;

namespace NorthstarET.Lms.Api.Common;

/// <summary>
/// Pagination support infrastructure for REST APIs implementing FR-037
/// Provides consistent pagination across all API endpoints
/// </summary>
public static class PaginationSupport
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MinPageSize = 1;

    /// <summary>
    /// Creates a standardized paginated API response
    /// </summary>
    public static PaginatedApiResponse<T> CreateResponse<T>(
        PagedResult<T> pagedResult, 
        string requestPath)
    {
        return new PaginatedApiResponse<T>
        {
            Data = pagedResult.Items,
            Pagination = new PaginationMetadata
            {
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalItems = pagedResult.TotalCount,
                TotalPages = pagedResult.TotalPages,
                HasNextPage = pagedResult.HasNextPage,
                HasPreviousPage = pagedResult.HasPreviousPage,
                FirstPageUrl = BuildPageUrl(requestPath, 1, pagedResult.PageSize),
                LastPageUrl = BuildPageUrl(requestPath, pagedResult.TotalPages, pagedResult.PageSize),
                NextPageUrl = pagedResult.HasNextPage 
                    ? BuildPageUrl(requestPath, pagedResult.Page + 1, pagedResult.PageSize) 
                    : null,
                PreviousPageUrl = pagedResult.HasPreviousPage 
                    ? BuildPageUrl(requestPath, pagedResult.Page - 1, pagedResult.PageSize) 
                    : null
            }
        };
    }

    /// <summary>
    /// Validates and normalizes pagination parameters
    /// </summary>
    public static (int page, int pageSize) ValidateParameters(int? page, int? pageSize)
    {
        var validatedPage = Math.Max(page ?? 1, 1);
        var validatedPageSize = Math.Clamp(pageSize ?? DefaultPageSize, MinPageSize, MaxPageSize);
        
        return (validatedPage, validatedPageSize);
    }

    private static string BuildPageUrl(string basePath, int page, int pageSize)
    {
        var separator = basePath.Contains('?') ? "&" : "?";
        return $"{basePath}{separator}page={page}&pageSize={pageSize}";
    }
}

/// <summary>
/// Standardized paginated API response format
/// </summary>
public class PaginatedApiResponse<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public PaginationMetadata Pagination { get; set; } = new();
}

/// <summary>
/// Pagination metadata following REST API best practices
/// </summary>
public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? FirstPageUrl { get; set; }
    public string? LastPageUrl { get; set; }
    public string? NextPageUrl { get; set; }
    public string? PreviousPageUrl { get; set; }
}