using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.ValueObjects;

/// <summary>
/// Represents a tenant slug with validation rules
/// </summary>
public sealed class TenantSlug : ValueObject
{
    public string Value { get; }

    public TenantSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tenant slug cannot be null or empty", nameof(value));

        if (!IsValidSlug(value))
            throw new ArgumentException("Slug must contain only lowercase letters, numbers, and hyphens", nameof(value));

        Value = value;
    }

    private static bool IsValidSlug(string slug)
    {
        if (slug.Length < 2 || slug.Length > 63)
            return false;

        if (slug.StartsWith('-') || slug.EndsWith('-'))
            return false;

        return slug.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-');
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(TenantSlug slug) => slug.Value;
    public static implicit operator TenantSlug(string slug) => new(slug);

    public override string ToString() => Value;
}