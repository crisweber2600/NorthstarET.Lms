using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace NorthstarET.Lms.Application.Services;

/// <summary>
/// Service implementing bulk import error handling strategies per FR-033
/// Provides four user-selectable strategies for bulk operations
/// </summary>
public class BulkImportStrategyService : IBulkImportStrategyService
{
    private readonly ILogger<BulkImportStrategyService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BulkImportStrategyService(
        ILogger<BulkImportStrategyService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes bulk import using the specified strategy
    /// </summary>
    public async Task<BulkImportResult<T>> ExecuteImportAsync<T>(
        BulkImportRequest<T> request,
        Func<T, CancellationToken, Task<ValidationResult>> validator,
        Func<IEnumerable<T>, CancellationToken, Task<int>> processor,
        CancellationToken cancellationToken = default) where T : class
    {
        var strategy = CreateStrategy(request.Strategy);
        
        _logger.LogInformation("Starting bulk import with strategy {Strategy}. Items: {ItemCount}, CorrelationId: {CorrelationId}", 
            request.Strategy, request.Items.Count(), request.CorrelationId);

        var result = await strategy.ExecuteAsync(request, validator, processor, cancellationToken);
        
        _logger.LogInformation("Bulk import completed. Strategy: {Strategy}, Success: {Success}, Processed: {ProcessedCount}, Failed: {FailedCount}", 
            request.Strategy, result.IsSuccess, result.ProcessedCount, result.FailedItems.Count);

        return result;
    }

    private IBulkImportStrategy CreateStrategy(BulkImportStrategy strategyType)
    {
        return strategyType switch
        {
            BulkImportStrategy.AllOrNothing => new AllOrNothingStrategy(_logger, _serviceProvider),
            BulkImportStrategy.BestEffort => new BestEffortStrategy(_logger),
            BulkImportStrategy.ThresholdBased => new ThresholdBasedStrategy(_logger, _serviceProvider),
            BulkImportStrategy.PreviewMode => new PreviewModeStrategy(_logger),
            _ => throw new ArgumentOutOfRangeException(nameof(strategyType))
        };
    }
}

/// <summary>
/// Interface for bulk import strategy service
/// </summary>
public interface IBulkImportStrategyService
{
    Task<BulkImportResult<T>> ExecuteImportAsync<T>(
        BulkImportRequest<T> request,
        Func<T, CancellationToken, Task<ValidationResult>> validator,
        Func<IEnumerable<T>, CancellationToken, Task<int>> processor,
        CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Base interface for bulk import strategies
/// </summary>
public interface IBulkImportStrategy
{
    Task<BulkImportResult<T>> ExecuteAsync<T>(
        BulkImportRequest<T> request,
        Func<T, CancellationToken, Task<ValidationResult>> validator,
        Func<IEnumerable<T>, CancellationToken, Task<int>> processor,
        CancellationToken cancellationToken) where T : class;
}

/// <summary>
/// All-or-Nothing Strategy: Single transaction, complete rollback on any failure
/// </summary>
public class AllOrNothingStrategy : IBulkImportStrategy
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public AllOrNothingStrategy(ILogger logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<BulkImportResult<T>> ExecuteAsync<T>(
        BulkImportRequest<T> request,
        Func<T, CancellationToken, Task<ValidationResult>> validator,
        Func<IEnumerable<T>, CancellationToken, Task<int>> processor,
        CancellationToken cancellationToken) where T : class
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        await unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Validate all items first
            var validationTasks = request.Items.Select(async item => 
            {
                var validation = await validator(item, cancellationToken);
                return new { Item = item, Validation = validation };
            });

            var validationResults = await Task.WhenAll(validationTasks);
            var failedValidations = validationResults.Where(r => r.Validation != ValidationResult.Success).ToList();

            if (failedValidations.Any())
            {
                await unitOfWork.RollbackTransactionAsync();
                
                return new BulkImportResult<T>
                {
                    IsSuccess = false,
                    ProcessedCount = 0,
                    FailedItems = failedValidations.Select(f => new BulkImportError<T>
                    {
                        Item = f.Item,
                        Error = string.Join("; ", f.Validation.ErrorMessage ?? "Validation failed"),
                        ErrorType = BulkImportErrorType.ValidationError
                    }).ToList(),
                    Summary = $"Validation failed for {failedValidations.Count} items. All items rolled back.",
                    CorrelationId = request.CorrelationId
                };
            }

            // Process all valid items
            var processedCount = await processor(request.Items, cancellationToken);
            await unitOfWork.CommitTransactionAsync();

            return new BulkImportResult<T>
            {
                IsSuccess = true,
                ProcessedCount = processedCount,
                FailedItems = new List<BulkImportError<T>>(),
                Summary = $"Successfully processed {processedCount} items.",
                CorrelationId = request.CorrelationId
            };
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "All-or-Nothing bulk import failed");
            
            return new BulkImportResult<T>
            {
                IsSuccess = false,
                ProcessedCount = 0,
                FailedItems = request.Items.Select(item => new BulkImportError<T>
                {
                    Item = item,
                    Error = ex.Message,
                    ErrorType = BulkImportErrorType.ProcessingError
                }).ToList(),
                Summary = $"Import failed: {ex.Message}",
                CorrelationId = request.CorrelationId
            };
        }
    }
}

/// <summary>
/// Best-Effort Strategy: Continue with valid records, report failed records
/// </summary>
public class BestEffortStrategy : IBulkImportStrategy
{
    private readonly ILogger _logger;

    public BestEffortStrategy(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<BulkImportResult<T>> ExecuteAsync<T>(
        BulkImportRequest<T> request,
        Func<T, CancellationToken, Task<ValidationResult>> validator,
        Func<IEnumerable<T>, CancellationToken, Task<int>> processor,
        CancellationToken cancellationToken) where T : class
    {
        var validItems = new List<T>();
        var failedItems = new List<BulkImportError<T>>();

        // Process items individually to isolate failures
        foreach (var item in request.Items)
        {
            try
            {
                var validation = await validator(item, cancellationToken);
                if (validation == ValidationResult.Success)
                {
                    validItems.Add(item);
                }
                else
                {
                    failedItems.Add(new BulkImportError<T>
                    {
                        Item = item,
                        Error = validation.ErrorMessage ?? "Validation failed",
                        ErrorType = BulkImportErrorType.ValidationError
                    });
                }
            }
            catch (Exception ex)
            {
                failedItems.Add(new BulkImportError<T>
                {
                    Item = item,
                    Error = ex.Message,
                    ErrorType = BulkImportErrorType.ValidationError
                });
            }
        }

        var processedCount = 0;
        if (validItems.Any())
        {
            try
            {
                processedCount = await processor(validItems, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing failed for valid items in best-effort strategy");
                
                // Move valid items to failed items
                failedItems.AddRange(validItems.Select(item => new BulkImportError<T>
                {
                    Item = item,
                    Error = ex.Message,
                    ErrorType = BulkImportErrorType.ProcessingError
                }));
            }
        }

        return new BulkImportResult<T>
        {
            IsSuccess = processedCount > 0,
            ProcessedCount = processedCount,
            FailedItems = failedItems,
            Summary = $"Processed {processedCount} items successfully, {failedItems.Count} failed.",
            CorrelationId = request.CorrelationId
        };
    }
}

/// <summary>
/// Threshold-Based Strategy: Fail if error rate exceeds threshold
/// </summary>
public class ThresholdBasedStrategy : IBulkImportStrategy
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public ThresholdBasedStrategy(ILogger logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<BulkImportResult<T>> ExecuteAsync<T>(
        BulkImportRequest<T> request,
        Func<T, CancellationToken, Task<ValidationResult>> validator,
        Func<IEnumerable<T>, CancellationToken, Task<int>> processor,
        CancellationToken cancellationToken) where T : class
    {
        var threshold = request.ErrorThresholdPercent ?? 5.0; // Default 5%
        var totalItems = request.Items.Count();
        var maxAllowedErrors = (int)Math.Floor(totalItems * threshold / 100.0);

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var failedItems = new List<BulkImportError<T>>();
            var validItems = new List<T>();

            // Validate items and track error rate
            foreach (var item in request.Items)
            {
                try
                {
                    var validation = await validator(item, cancellationToken);
                    if (validation == ValidationResult.Success)
                    {
                        validItems.Add(item);
                    }
                    else
                    {
                        failedItems.Add(new BulkImportError<T>
                        {
                            Item = item,
                            Error = validation.ErrorMessage ?? "Validation failed",
                            ErrorType = BulkImportErrorType.ValidationError
                        });

                        // Check if we've exceeded the error threshold
                        if (failedItems.Count > maxAllowedErrors)
                        {
                            await unitOfWork.RollbackTransactionAsync();
                            
                            return new BulkImportResult<T>
                            {
                                IsSuccess = false,
                                ProcessedCount = 0,
                                FailedItems = failedItems,
                                Summary = $"Error threshold exceeded: {failedItems.Count}/{totalItems} errors > {threshold}% threshold. All items rolled back.",
                                CorrelationId = request.CorrelationId
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedItems.Add(new BulkImportError<T>
                    {
                        Item = item,
                        Error = ex.Message,
                        ErrorType = BulkImportErrorType.ValidationError
                    });
                }
            }

            // Process valid items if within threshold
            var processedCount = validItems.Any() ? await processor(validItems, cancellationToken) : 0;
            await unitOfWork.CommitTransactionAsync();

            return new BulkImportResult<T>
            {
                IsSuccess = true,
                ProcessedCount = processedCount,
                FailedItems = failedItems,
                Summary = $"Processed {processedCount} items. {failedItems.Count} errors within {threshold}% threshold.",
                CorrelationId = request.CorrelationId
            };
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Threshold-based bulk import failed");
            
            return new BulkImportResult<T>
            {
                IsSuccess = false,
                ProcessedCount = 0,
                FailedItems = request.Items.Select(item => new BulkImportError<T>
                {
                    Item = item,
                    Error = ex.Message,
                    ErrorType = BulkImportErrorType.ProcessingError
                }).ToList(),
                Summary = $"Import failed: {ex.Message}",
                CorrelationId = request.CorrelationId
            };
        }
    }
}

/// <summary>
/// Preview Mode Strategy: Validation-only pass with detailed change report
/// </summary>
public class PreviewModeStrategy : IBulkImportStrategy
{
    private readonly ILogger _logger;

    public PreviewModeStrategy(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<BulkImportResult<T>> ExecuteAsync<T>(
        BulkImportRequest<T> request,
        Func<T, CancellationToken, Task<ValidationResult>> validator,
        Func<IEnumerable<T>, CancellationToken, Task<int>> processor,
        CancellationToken cancellationToken) where T : class
    {
        var validItems = new List<T>();
        var failedItems = new List<BulkImportError<T>>();

        // Validate all items without processing
        foreach (var item in request.Items)
        {
            try
            {
                var validation = await validator(item, cancellationToken);
                if (validation == ValidationResult.Success)
                {
                    validItems.Add(item);
                }
                else
                {
                    failedItems.Add(new BulkImportError<T>
                    {
                        Item = item,
                        Error = validation.ErrorMessage ?? "Validation failed",
                        ErrorType = BulkImportErrorType.ValidationError
                    });
                }
            }
            catch (Exception ex)
            {
                failedItems.Add(new BulkImportError<T>
                {
                    Item = item,
                    Error = ex.Message,
                    ErrorType = BulkImportErrorType.ValidationError
                });
            }
        }

        return new BulkImportResult<T>
        {
            IsSuccess = true, // Preview mode always "succeeds"
            ProcessedCount = 0, // No actual processing in preview mode
            FailedItems = failedItems,
            Summary = $"PREVIEW: {validItems.Count} items would be processed successfully, {failedItems.Count} would fail.",
            CorrelationId = request.CorrelationId,
            IsPreview = true,
            PreviewData = new BulkImportPreviewData
            {
                WouldProcessCount = validItems.Count,
                WouldFailCount = failedItems.Count,
                EstimatedDurationSeconds = EstimateProcessingDuration(validItems.Count)
            }
        };
    }

    private int EstimateProcessingDuration(int itemCount)
    {
        // Rough estimate: 100 items per second
        return Math.Max(1, itemCount / 100);
    }
}

/// <summary>
/// Bulk import request model
/// </summary>
public class BulkImportRequest<T> where T : class
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public BulkImportStrategy Strategy { get; set; } = BulkImportStrategy.BestEffort;
    public double? ErrorThresholdPercent { get; set; } // For threshold-based strategy
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Bulk import result model
/// </summary>
public class BulkImportResult<T> where T : class
{
    public bool IsSuccess { get; set; }
    public int ProcessedCount { get; set; }
    public List<BulkImportError<T>> FailedItems { get; set; } = new();
    public string Summary { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public bool IsPreview { get; set; } = false;
    public BulkImportPreviewData? PreviewData { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Bulk import error model
/// </summary>
public class BulkImportError<T> where T : class
{
    public T Item { get; set; } = default!;
    public string Error { get; set; } = "";
    public BulkImportErrorType ErrorType { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Preview mode additional data
/// </summary>
public class BulkImportPreviewData
{
    public int WouldProcessCount { get; set; }
    public int WouldFailCount { get; set; }
    public int EstimatedDurationSeconds { get; set; }
}

/// <summary>
/// Available bulk import strategies
/// </summary>
public enum BulkImportStrategy
{
    AllOrNothing,
    BestEffort,
    ThresholdBased,
    PreviewMode
}

/// <summary>
/// Types of bulk import errors
/// </summary>
public enum BulkImportErrorType
{
    ValidationError,
    ProcessingError,
    DuplicateError,
    ReferenceError
}