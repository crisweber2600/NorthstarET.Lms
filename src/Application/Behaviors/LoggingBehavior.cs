using MediatR;
using Microsoft.Extensions.Logging;
using NorthstarET.Lms.Application.Common;
using System.Diagnostics;

namespace NorthstarET.Lms.Application.Behaviors;

/// <summary>
/// Pipeline behavior that logs command/query execution details
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = GetCorrelationId(request);
        var tenantSlug = GetTenantSlug(request);
        var requestedBy = GetRequestedBy(request);

        _logger.LogInformation(
            "Handling {RequestName} | CorrelationId: {CorrelationId} | Tenant: {TenantSlug} | User: {RequestedBy}",
            requestName, correlationId, tenantSlug ?? "N/A", requestedBy ?? "N/A");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            var isSuccess = IsSuccessResult(response);
            var logLevel = isSuccess ? LogLevel.Information : LogLevel.Warning;

            _logger.Log(logLevel,
                "Handled {RequestName} | Success: {IsSuccess} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                requestName, isSuccess, stopwatch.ElapsedMilliseconds, correlationId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error handling {RequestName} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                requestName, stopwatch.ElapsedMilliseconds, correlationId);
            throw;
        }
    }

    private static string? GetCorrelationId(TRequest request)
    {
        var prop = request?.GetType().GetProperty("CorrelationId");
        return prop?.GetValue(request)?.ToString();
    }

    private static string? GetTenantSlug(TRequest request)
    {
        var prop = request?.GetType().GetProperty("TenantSlug");
        return prop?.GetValue(request)?.ToString();
    }

    private static string? GetRequestedBy(TRequest request)
    {
        var prop = request?.GetType().GetProperty("RequestedBy");
        return prop?.GetValue(request)?.ToString();
    }

    private static bool IsSuccessResult(TResponse response)
    {
        if (response is Result result)
        {
            return result.IsSuccess;
        }
        return true;
    }
}
