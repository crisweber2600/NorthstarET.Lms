using FluentValidation;
using MediatR;
using NorthstarET.Lms.Application.Common;

namespace NorthstarET.Lms.Application.Behaviors;

/// <summary>
/// Pipeline behavior that validates commands/queries before they are handled
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            var errors = string.Join("; ", failures.Select(f => f.ErrorMessage));
            
            // Use reflection to create the appropriate Result type
            var resultType = typeof(TResponse);
            if (resultType.IsGenericType)
            {
                var genericType = resultType.GetGenericArguments()[0];
                var failureMethod = typeof(Result).GetMethod(nameof(Result.Failure))!
                    .MakeGenericMethod(genericType);
                return (TResponse)failureMethod.Invoke(null, new object[] { $"Validation failed: {errors}" })!;
            }
            else
            {
                return (TResponse)(object)Result.Failure($"Validation failed: {errors}");
            }
        }

        return await next();
    }
}
