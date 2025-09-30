namespace NorthstarET.Lms.Domain.Shared;

public interface ITenantContext
{
    string TenantSlug { get; }
    void SetTenantSlug(string tenantSlug);
}
