namespace NorthstarET.Lms.Tests.Bdd.Support;

/// <summary>
/// Shared context for BDD test scenarios
/// </summary>
public class TestContext
{
    /// <summary>
    /// Current tenant slug for the test scenario
    /// </summary>
    public string? TenantSlug { get; set; }

    /// <summary>
    /// Current user for the test scenario
    /// </summary>
    public string? CurrentUser { get; set; }

    /// <summary>
    /// Current user role for the test scenario
    /// </summary>
    public string? CurrentUserRole { get; set; }

    /// <summary>
    /// Stores scenario-specific data
    /// </summary>
    public Dictionary<string, object> Data { get; } = new();
}
