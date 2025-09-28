using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Domain.Services;

public interface ITenantIsolationService
{
    /// <summary>
    /// Validates that the current user context is authorized to access the specified tenant
    /// </summary>
    Task<bool> ValidateTenantAccessAsync(string tenantId, string userId);
    
    /// <summary>
    /// Ensures all data queries are properly scoped to the current tenant
    /// </summary>
    void ApplyTenantFilter<TEntity>(IQueryable<TEntity> query, string tenantId) where TEntity : TenantScopedEntity;
    
    /// <summary>
    /// Validates that an entity belongs to the correct tenant before operations
    /// </summary>
    Task<bool> ValidateEntityTenantScopeAsync<TEntity>(TEntity entity, string tenantId) where TEntity : TenantScopedEntity;
}

public interface IRoleAuthorizationService
{
    /// <summary>
    /// Evaluates whether a user has the required permission for a specific operation
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permission, string? resourceContext = null);
    
    /// <summary>
    /// Gets all effective permissions for a user across all their roles
    /// </summary>
    Task<IEnumerable<string>> GetEffectivePermissionsAsync(Guid userId);
    
    /// <summary>
    /// Validates role assignments against scope constraints (School, Class, SchoolYear)
    /// </summary>
    Task<bool> ValidateRoleScopeAsync(RoleAssignment roleAssignment);
    
    /// <summary>
    /// Evaluates composite authorization when user has multiple roles
    /// </summary>
    Task<bool> EvaluateCompositeAuthorizationAsync(Guid userId, string permission, object? context = null);
    
    /// <summary>
    /// Checks if a user can delegate their role to another user
    /// </summary>
    Task<bool> CanDelegateRoleAsync(Guid delegatingUserId, Guid targetUserId, Guid roleDefinitionId);
}

public interface IAuditChainService
{
    /// <summary>
    /// Generates a tamper-evident hash for the audit record
    /// </summary>
    string GenerateAuditHash(AuditRecord auditRecord, string? previousHash = null);
    
    /// <summary>
    /// Validates the integrity of an audit chain
    /// </summary>
    Task<bool> ValidateAuditChainAsync(IEnumerable<AuditRecord> auditRecords);
    
    /// <summary>
    /// Detects any tampering in the audit chain
    /// </summary>
    Task<IEnumerable<string>> DetectTamperingAsync(IEnumerable<AuditRecord> auditRecords);
    
    /// <summary>
    /// Gets the previous audit record hash for chaining
    /// </summary>
    Task<string?> GetPreviousAuditHashAsync(string tenantId, DateTime beforeTimestamp);
}

public interface IRetentionPolicyService
{
    /// <summary>
    /// Gets the effective retention policy for an entity type
    /// </summary>
    Task<RetentionPolicy?> GetEffectiveRetentionPolicyAsync(string entityType, string tenantId);
    
    /// <summary>
    /// Determines if an entity is eligible for purging based on retention policies
    /// </summary>
    Task<bool> IsEligibleForPurgeAsync<TEntity>(TEntity entity) where TEntity : TenantScopedEntity;
    
    /// <summary>
    /// Calculates the purge date for an entity
    /// </summary>
    Task<DateTime?> CalculatePurgeDateAsync<TEntity>(TEntity entity) where TEntity : TenantScopedEntity;
    
    /// <summary>
    /// Checks if an entity has any active legal holds preventing purge
    /// </summary>
    Task<bool> HasActiveLegalHoldAsync<TEntity>(TEntity entity) where TEntity : TenantScopedEntity;
}

public interface ISecurityMonitoringService
{
    /// <summary>
    /// Logs a security event for monitoring
    /// </summary>
    Task LogSecurityEventAsync(string eventType, string userId, string details, string? resourceId = null);
    
    /// <summary>
    /// Detects anomalous access patterns
    /// </summary>
    Task<IEnumerable<SecurityAlert>> DetectAccessAnomaliesAsync(string userId, TimeSpan lookbackPeriod);
    
    /// <summary>
    /// Validates repeated authorization failures and triggers alerts
    /// </summary>
    Task<bool> ShouldTriggerSecurityAlertAsync(string userId, string failureType, int failureCount);
    
    /// <summary>
    /// Generates multi-tier security alerts based on severity
    /// </summary>
    Task GenerateSecurityAlertAsync(SecurityAlert alert);
}

public class SecurityAlert
{
    public string UserId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Minor, Major, Critical
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? ResourceContext { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}