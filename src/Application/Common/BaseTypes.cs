using MediatR;

namespace NorthstarET.Lms.Application.Common;

/// <summary>
/// Base class for commands that modify data
/// </summary>
public abstract class Command : IRequest<Result>
{
    /// <summary>
    /// Correlation ID for tracking across systems
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User performing the command
    /// </summary>
    public string? RequestedBy { get; init; }

    /// <summary>
    /// Tenant context (automatically set by middleware)
    /// </summary>
    public string? TenantSlug { get; init; }
}

/// <summary>
/// Base class for commands that return a specific result
/// </summary>
public abstract class Command<TResponse> : IRequest<Result<TResponse>>
{
    /// <summary>
    /// Correlation ID for tracking across systems
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User performing the command
    /// </summary>
    public string? RequestedBy { get; init; }

    /// <summary>
    /// Tenant context (automatically set by middleware)
    /// </summary>
    public string? TenantSlug { get; init; }
}

/// <summary>
/// Base class for queries that retrieve data
/// </summary>
public abstract class Query<TResponse> : IRequest<Result<TResponse>>
{
    /// <summary>
    /// Correlation ID for tracking across systems
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User performing the query
    /// </summary>
    public string? RequestedBy { get; init; }

    /// <summary>
    /// Tenant context (automatically set by middleware)
    /// </summary>
    public string? TenantSlug { get; init; }
}

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

/// <summary>
/// Represents the result of an operation with a return value
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error)
    {
        Value = value;
    }
}