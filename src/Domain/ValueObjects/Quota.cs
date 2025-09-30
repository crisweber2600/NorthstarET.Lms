using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.ValueObjects;

/// <summary>
/// Represents resource quotas for a district
/// </summary>
public sealed class Quota : ValueObject
{
    public int Students { get; }
    public int Staff { get; }
    public int Admins { get; }

    public Quota(int students, int staff, int admins)
    {
        if (students < 0) throw new ArgumentException("Students quota cannot be negative", nameof(students));
        if (staff < 0) throw new ArgumentException("Staff quota cannot be negative", nameof(staff));
        if (admins < 0) throw new ArgumentException("Admins quota cannot be negative", nameof(admins));

        Students = students;
        Staff = staff;
        Admins = admins;
    }

    /// <summary>
    /// Default quotas for new districts
    /// </summary>
    public static Quota Default => new(50000, 5000, 100);

    /// <summary>
    /// Check if current usage is within quota limits
    /// </summary>
    public bool IsWithinLimits(int currentStudents, int currentStaff, int currentAdmins)
    {
        return currentStudents <= Students && 
               currentStaff <= Staff && 
               currentAdmins <= Admins;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Students;
        yield return Staff;
        yield return Admins;
    }
}