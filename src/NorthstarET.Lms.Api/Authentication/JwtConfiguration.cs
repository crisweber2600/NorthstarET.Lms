using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace NorthstarET.Lms.Api.Authentication;

/// <summary>
/// Configuration for JWT Bearer authentication with multi-tenant support
/// and integration with Microsoft Entra External ID.
/// </summary>
public class JwtConfiguration
{
    /// <summary>
    /// Configure JWT Bearer authentication services
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Bind JWT settings from configuration
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<EntraIdSettings>(configuration.GetSection("EntraId"));

        // Add custom JWT token validator
        services.AddScoped<IJwtTokenValidator, JwtTokenValidator>();
        services.AddScoped<ITenantClaimsProvider, TenantClaimsProvider>();

        // Configure JWT Bearer authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;
            var entraSettings = configuration.GetSection("EntraId").Get<EntraIdSettings>()!;

            options.Authority = entraSettings.Authority;
            options.Audience = entraSettings.ClientId;
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                
                ValidIssuer = entraSettings.Authority,
                ValidAudience = entraSettings.ClientId,
                
                // For development/testing with custom JWT tokens
                IssuerSigningKey = !string.IsNullOrEmpty(jwtSettings.SecretKey)
                    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                    : null,
                
                // Custom claim mapping for tenant context
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role
            };

            // Custom token validation events
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var tenantClaimsProvider = context.HttpContext.RequestServices
                        .GetRequiredService<ITenantClaimsProvider>();
                    
                    // Enrich claims with tenant-specific information
                    await tenantClaimsProvider.EnrichClaimsAsync(context);
                },
                
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtConfiguration>>();
                    
                    logger.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
                    
                    // Add custom error response
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 401;
                    
                    var response = new
                    {
                        error = "invalid_token",
                        error_description = "The provided token is invalid or expired"
                    };
                    
                    return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                },
                
                OnChallenge = context =>
                {
                    // Customize 401 challenge response
                    context.HandleResponse();
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 401;
                    
                    var response = new
                    {
                        error = "unauthorized",
                        error_description = "Access token is required"
                    };
                    
                    return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                },
                
                OnForbidden = context =>
                {
                    // Customize 403 forbidden response
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 403;
                    
                    var response = new
                    {
                        error = "forbidden",
                        error_description = "Insufficient permissions for this resource"
                    };
                    
                    return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                }
            };
        });

        // Add authorization policies
        services.AddAuthorization(options =>
        {
            // Platform-level policies
            options.AddPolicy("PlatformAdminOnly", policy =>
                policy.RequireRole("PlatformAdmin"));
            
            // District-level policies
            options.AddPolicy("DistrictAccess", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("tenant_id", c => !string.IsNullOrEmpty(c)) ||
                    context.User.IsInRole("PlatformAdmin")));
            
            // School-level policies
            options.AddPolicy("SchoolAccess", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("school_id", c => !string.IsNullOrEmpty(c)) ||
                    context.User.IsInRole("DistrictAdmin") ||
                    context.User.IsInRole("PlatformAdmin")));
            
            // Class-level policies
            options.AddPolicy("ClassAccess", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("class_id", c => !string.IsNullOrEmpty(c)) ||
                    context.User.IsInRole("SchoolUser") ||
                    context.User.IsInRole("DistrictAdmin") ||
                    context.User.IsInRole("PlatformAdmin")));
            
            // FERPA compliance policies
            options.AddPolicy("StudentDataAccess", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("DistrictAdmin") ||
                    context.User.IsInRole("SchoolUser") ||
                    (context.User.IsInRole("Staff") && 
                     context.User.HasClaim("permissions", "ViewStudentData"))));
        });
    }
}

/// <summary>
/// JWT settings configuration
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}

/// <summary>
/// Microsoft Entra External ID settings
/// </summary>
public class EntraIdSettings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Authority => $"https://login.microsoftonline.com/{TenantId}";
    public string GraphApiUrl { get; set; } = "https://graph.microsoft.com/v1.0";
    public string[] Scopes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Interface for JWT token validation
/// </summary>
public interface IJwtTokenValidator
{
    Task<TokenValidationResult> ValidateTokenAsync(string token);
    Task<ClaimsPrincipal> GetPrincipalFromTokenAsync(string token);
}

/// <summary>
/// JWT token validator implementation
/// </summary>
public class JwtTokenValidator : IJwtTokenValidator
{
    private readonly JwtSettings _jwtSettings;
    private readonly EntraIdSettings _entraSettings;
    private readonly ILogger<JwtTokenValidator> _logger;

    public JwtTokenValidator(
        IOptions<JwtSettings> jwtSettings,
        IOptions<EntraIdSettings> entraSettings,
        ILogger<JwtTokenValidator> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _entraSettings = entraSettings.Value;
        _logger = logger;
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            return new TokenValidationResult
            {
                IsValid = true,
                Principal = principal,
                ValidatedToken = validatedToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Token validation failed: {Exception}", ex.Message);
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ClaimsPrincipal> GetPrincipalFromTokenAsync(string token)
    {
        var validationResult = await ValidateTokenAsync(token);
        return validationResult.IsValid ? validationResult.Principal! : new ClaimsPrincipal();
    }
}

/// <summary>
/// Token validation result
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public SecurityToken? ValidatedToken { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Interface for providing tenant-specific claims
/// </summary>
public interface ITenantClaimsProvider
{
    Task EnrichClaimsAsync(TokenValidatedContext context);
    Task<IEnumerable<Claim>> GetTenantClaimsAsync(string userId, string externalId);
}

/// <summary>
/// Tenant claims provider implementation
/// </summary>
public class TenantClaimsProvider : ITenantClaimsProvider
{
    private readonly ILogger<TenantClaimsProvider> _logger;
    // Add dependencies for user and role repositories

    public TenantClaimsProvider(ILogger<TenantClaimsProvider> logger)
    {
        _logger = logger;
    }

    public async Task EnrichClaimsAsync(TokenValidatedContext context)
    {
        try
        {
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var externalId = context.Principal?.FindFirst("oid")?.Value; // Entra External ID

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(externalId))
            {
                _logger.LogWarning("Missing user ID or external ID in token claims");
                return;
            }

            // Get tenant-specific claims from database
            var tenantClaims = await GetTenantClaimsAsync(userId, externalId);
            
            // Add tenant claims to the principal
            var identity = context.Principal?.Identity as ClaimsIdentity;
            if (identity != null)
            {
                identity.AddClaims(tenantClaims);
            }

            _logger.LogDebug("Enhanced claims for user {UserId} with {ClaimCount} tenant claims", 
                userId, tenantClaims.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching claims for user");
            // Don't throw - allow authentication to proceed without enhanced claims
        }
    }

    public async Task<IEnumerable<Claim>> GetTenantClaimsAsync(string userId, string externalId)
    {
        var claims = new List<Claim>();

        try
        {
            // TODO: Implement database lookup for user's tenant and role information
            // This would involve:
            // 1. Look up IdentityMapping to find internal user
            // 2. Get user's role assignments with scope (district/school/class)
            // 3. Build tenant_id, tenant_slug, school_id, class_id claims
            // 4. Build role and permission claims

            // Mock implementation for now
            claims.Add(new Claim("tenant_id", "district-123"));
            claims.Add(new Claim("tenant_slug", "oakland-unified"));
            claims.Add(new Claim("tenant_role", "DistrictAdmin"));
            claims.Add(new Claim("permissions", "ManageStudents"));
            claims.Add(new Claim("permissions", "ManageStaff"));

            _logger.LogDebug("Retrieved {ClaimCount} tenant claims for user {UserId}", 
                claims.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant claims for user {UserId}", userId);
        }

        return claims;
    }
}