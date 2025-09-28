namespace NorthstarET.Lms.Infrastructure.Security;

/// <summary>
/// Contract for entities that are tenant-scoped for multi-tenant isolation
/// </summary>
public interface ITenantScoped
{
    string TenantId { get; set; }
}