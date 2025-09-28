using NorthstarET.Lms.Api.Common;
using System.Text.Json;

namespace NorthstarET.Lms.Api.Middleware;

/// <summary>
/// Middleware to handle pagination parameters and response formatting
/// Implements consistent pagination across all API endpoints per FR-037
/// </summary>
public class PaginationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PaginationMiddleware> _logger;

    public PaginationMiddleware(RequestDelegate next, ILogger<PaginationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add pagination parameters to HttpContext for easy access by controllers
        if (context.Request.Method == HttpMethods.Get)
        {
            var (page, pageSize) = ExtractPaginationParameters(context.Request);
            context.Items["Pagination.Page"] = page;
            context.Items["Pagination.PageSize"] = pageSize;
            context.Items["Pagination.RequestPath"] = GetRequestPath(context.Request);
        }

        await _next(context);
    }

    private (int page, int pageSize) ExtractPaginationParameters(HttpRequest request)
    {
        var pageParam = request.Query["page"].FirstOrDefault();
        var pageSizeParam = request.Query["pageSize"].FirstOrDefault();

        int? page = null;
        int? pageSize = null;

        if (int.TryParse(pageParam, out var parsedPage))
        {
            page = parsedPage;
        }

        if (int.TryParse(pageSizeParam, out var parsedPageSize))
        {
            pageSize = parsedPageSize;
        }

        var (validatedPage, validatedPageSize) = PaginationSupport.ValidateParameters(page, pageSize);

        // Log if parameters were adjusted
        if (page.HasValue && page.Value != validatedPage)
        {
            _logger.LogWarning("Page parameter adjusted from {OriginalPage} to {ValidatedPage}", 
                page.Value, validatedPage);
        }

        if (pageSize.HasValue && pageSize.Value != validatedPageSize)
        {
            _logger.LogWarning("PageSize parameter adjusted from {OriginalPageSize} to {ValidatedPageSize}", 
                pageSize.Value, validatedPageSize);
        }

        return (validatedPage, validatedPageSize);
    }

    private string GetRequestPath(HttpRequest request)
    {
        var path = request.Path.Value ?? "";
        var queryString = request.QueryString.Value ?? "";
        
        // Remove existing page and pageSize parameters to avoid duplication
        if (!string.IsNullOrEmpty(queryString))
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);
            query.Remove("page");
            query.Remove("pageSize");
            
            if (query.Any())
            {
                var rebuiltQuery = string.Join("&", 
                    query.SelectMany(kvp => 
                        kvp.Value.Select(v => $"{kvp.Key}={Uri.EscapeDataString(v!)}")));
                return $"{path}?{rebuiltQuery}";
            }
        }

        return path;
    }
}

/// <summary>
/// Extension methods for registering pagination middleware
/// </summary>
public static class PaginationMiddlewareExtensions
{
    /// <summary>
    /// Registers the pagination middleware with the application pipeline
    /// </summary>
    public static IApplicationBuilder UsePagination(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PaginationMiddleware>();
    }
}

/// <summary>
/// Extension methods for controllers to easily access pagination parameters
/// </summary>
public static class HttpContextPaginationExtensions
{
    /// <summary>
    /// Gets the validated pagination parameters from HttpContext
    /// </summary>
    public static (int page, int pageSize) GetPaginationParameters(this HttpContext context)
    {
        var page = (int)(context.Items["Pagination.Page"] ?? PaginationSupport.DefaultPageSize);
        var pageSize = (int)(context.Items["Pagination.PageSize"] ?? PaginationSupport.DefaultPageSize);
        
        return (page, pageSize);
    }

    /// <summary>
    /// Gets the request path for pagination URL generation
    /// </summary>
    public static string GetPaginationRequestPath(this HttpContext context)
    {
        return (string)(context.Items["Pagination.RequestPath"] ?? context.Request.Path.Value ?? "");
    }
}