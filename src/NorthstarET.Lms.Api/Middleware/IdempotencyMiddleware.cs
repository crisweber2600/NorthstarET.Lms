using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NorthstarET.Lms.Api.Middleware;

/// <summary>
/// Middleware to handle idempotency keys for preventing duplicate operations (FR-038)
/// Supports both client-provided keys and auto-generated keys for GET operations
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly IdempotencyOptions _options;

    public IdempotencyMiddleware(
        RequestDelegate next, 
        ILogger<IdempotencyMiddleware> logger,
        IMemoryCache cache,
        IOptions<IdempotencyOptions> options)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply idempotency to specified HTTP methods
        if (!_options.IdempotentMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = GetIdempotencyKey(context);
        
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            // For operations that should be idempotent but have no key, generate one
            if (_options.RequireIdempotencyKey)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Idempotency-Key header is required for this operation");
                return;
            }
            
            // Generate key for GET operations or when auto-generation is enabled
            idempotencyKey = GenerateIdempotencyKey(context);
        }

        var cacheKey = $"idempotency:{idempotencyKey}";
        
        // Check if we've seen this key before
        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            _logger.LogInformation("Returning cached result for idempotency key {IdempotencyKey}", idempotencyKey);
            
            var cached = (IdempotencyResult)cachedResult!;
            context.Response.StatusCode = cached.StatusCode;
            
            // Set response headers
            foreach (var header in cached.Headers)
            {
                context.Response.Headers.TryAdd(header.Key, header.Value);
            }
            
            if (!string.IsNullOrEmpty(cached.ResponseBody))
            {
                context.Response.ContentType = cached.ContentType;
                await context.Response.WriteAsync(cached.ResponseBody);
            }
            
            return;
        }

        // Capture the response for caching
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            // Only cache successful responses (2xx status codes)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                
                var result = new IdempotencyResult
                {
                    StatusCode = context.Response.StatusCode,
                    ResponseBody = responseBody,
                    ContentType = context.Response.ContentType ?? "application/json",
                    Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
                };

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.CacheDuration,
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, result, cacheOptions);
                
                _logger.LogInformation("Cached result for idempotency key {IdempotencyKey} with expiration {Expiration}", 
                    idempotencyKey, _options.CacheDuration);
            }

            // Copy the captured response back to the original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBody);
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    }

    private string? GetIdempotencyKey(HttpContext context)
    {
        // Check for client-provided idempotency key
        if (context.Request.Headers.TryGetValue("Idempotency-Key", out var headerValue))
        {
            var key = headerValue.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(key) && IsValidIdempotencyKey(key))
            {
                return key;
            }
        }

        return null;
    }

    private string GenerateIdempotencyKey(HttpContext context)
    {
        // Generate deterministic key based on request characteristics
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(context.Request.Method);
        keyBuilder.Append(context.Request.Path);
        keyBuilder.Append(context.Request.QueryString);
        
        // Include user context for tenant isolation
        var userId = context.User?.Identity?.Name ?? "anonymous";
        keyBuilder.Append(userId);

        // For POST/PUT/PATCH operations, include request body hash
        if (context.Request.Method != HttpMethods.Get && 
            context.Request.Method != HttpMethods.Head &&
            context.Request.ContentLength > 0)
        {
            // Note: This is a simplified approach. In production, you might want to
            // read and hash the request body, but this requires careful stream handling
            keyBuilder.Append(context.Request.ContentLength);
            keyBuilder.Append(context.Request.ContentType);
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyBuilder.ToString()));
        return Convert.ToBase64String(hash)[..16]; // Use first 16 characters of base64 hash
    }

    private bool IsValidIdempotencyKey(string key)
    {
        // Basic validation: key should be 8-255 characters, alphanumeric with limited special chars
        return key.Length >= 8 && 
               key.Length <= 255 && 
               key.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }

    private class IdempotencyResult
    {
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; } = "";
        public string ContentType { get; set; } = "";
        public Dictionary<string, string> Headers { get; set; } = new();
    }
}

/// <summary>
/// Configuration options for idempotency middleware
/// </summary>
public class IdempotencyOptions
{
    /// <summary>
    /// HTTP methods that should be treated as idempotent
    /// </summary>
    public HashSet<string> IdempotentMethods { get; set; } = new() 
    { 
        HttpMethods.Get, 
        HttpMethods.Put, 
        HttpMethods.Delete,
        HttpMethods.Post  // POST operations with idempotency keys
    };

    /// <summary>
    /// Whether to require explicit idempotency keys for POST operations
    /// </summary>
    public bool RequireIdempotencyKey { get; set; } = false;

    /// <summary>
    /// How long to cache idempotent responses
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Extension methods for registering idempotency middleware
/// </summary>
public static class IdempotencyMiddlewareExtensions
{
    /// <summary>
    /// Registers the idempotency middleware with the application pipeline
    /// </summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IdempotencyMiddleware>();
    }

    /// <summary>
    /// Registers idempotency services with the DI container
    /// </summary>
    public static IServiceCollection AddIdempotency(this IServiceCollection services, 
        Action<IdempotencyOptions>? configureOptions = null)
    {
        services.AddMemoryCache(); // Required for caching responses
        
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<IdempotencyOptions>(_ => { });
        }

        return services;
    }
}