using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.ValueObjects;

/// <summary>
/// Represents a student's grade level
/// </summary>
public sealed class GradeLevel : ValueObject
{
    public int Value { get; }

    public GradeLevel(int value)
    {
        if (value < -1 || value > 12)
            throw new ArgumentException("Grade level must be between -1 (PreK) and 12", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Pre-Kindergarten grade level
    /// </summary>
    public static GradeLevel PreK => new(-1);

    /// <summary>
    /// Kindergarten grade level
    /// </summary>
    public static GradeLevel Kindergarten => new(0);

    /// <summary>
    /// Create a grade level for grades 1-12
    /// </summary>
    public static GradeLevel Grade(int grade) => new(grade);

    /// <summary>
    /// Get the next grade level for promotion
    /// </summary>
    public GradeLevel GetNextGrade()
    {
        if (Value >= 12)
            throw new InvalidOperationException("Cannot promote beyond 12th grade");

        return new GradeLevel(Value + 1);
    }

    /// <summary>
    /// Check if this is a graduating grade
    /// </summary>
    public bool IsGraduating => Value == 12;

    /// <summary>
    /// Get display name for the grade level
    /// </summary>
    public string DisplayName => Value switch
    {
        -1 => "Pre-K",
        0 => "Kindergarten",
        _ => $"Grade {Value}"
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator int(GradeLevel grade) => grade.Value;
    public static implicit operator GradeLevel(int grade) => new(grade);

    public override string ToString() => DisplayName;
}

/// <summary>
/// Represents special program flags for a student
/// </summary>
public sealed class ProgramFlag : ValueObject
{
    public string Code { get; }
    public string Description { get; }

    public ProgramFlag(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Program code cannot be null or empty", nameof(code));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Program description cannot be null or empty", nameof(description));

        Code = code.ToUpperInvariant();
        Description = description.Trim();
    }

    // Common program flags
    public static ProgramFlag SpecialEducation => new("SPED", "Special Education");
    public static ProgramFlag EnglishLearner => new("EL", "English Learner");
    public static ProgramFlag GiftedAndTalented => new("GT", "Gifted and Talented");
    public static ProgramFlag FreeOrReducedLunch => new("FRL", "Free or Reduced Lunch");
    public static ProgramFlag McKinneyVento => new("MV", "McKinney-Vento Homeless");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => $"{Code}: {Description}";
}

/// <summary>
/// Represents accommodation tags for a student
/// </summary>
public sealed class AccommodationTag : ValueObject
{
    public string Code { get; }
    public string Description { get; }
    public string Category { get; }

    public AccommodationTag(string code, string description, string category)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Accommodation code cannot be null or empty", nameof(code));

        Code = code.ToUpperInvariant();
        Description = description?.Trim() ?? string.Empty;
        Category = category?.Trim() ?? "General";
    }

    // Common accommodation categories
    public static AccommodationTag ExtendedTime => new("EXT_TIME", "Extended Time", "Testing");
    public static AccommodationTag AlternativeFormat => new("ALT_FORMAT", "Alternative Format", "Testing");
    public static AccommodationTag AssistiveTechnology => new("ASSIST_TECH", "Assistive Technology", "Learning");
    public static AccommodationTag PreferredSeating => new("PREF_SEAT", "Preferred Seating", "Classroom");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Category;
    }

    public override string ToString() => $"{Code}: {Description}";
}