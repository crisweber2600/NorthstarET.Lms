using NorthstarET.Lms.Domain.Common;

namespace NorthstarET.Lms.Domain.ValueObjects;

/// <summary>
/// Represents a permission that can be assigned to roles
/// </summary>
public sealed class Permission : ValueObject
{
    public string Code { get; }
    public string Description { get; }
    public string Category { get; }

    public Permission(string code, string description, string category = "General")
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code cannot be null or empty", nameof(code));

        Code = code.ToUpperInvariant();
        Description = description?.Trim() ?? string.Empty;
        Category = category?.Trim() ?? "General";
    }

    // Common system permissions
    public static Permission ViewStudents => new("VIEW_STUDENTS", "View student information", "Students");
    public static Permission EditStudents => new("EDIT_STUDENTS", "Edit student information", "Students");
    public static Permission ViewStaff => new("VIEW_STAFF", "View staff information", "Staff");
    public static Permission EditStaff => new("EDIT_STAFF", "Edit staff information", "Staff");
    public static Permission ManageClasses => new("MANAGE_CLASSES", "Manage classes and enrollment", "Classes");
    public static Permission ViewReports => new("VIEW_REPORTS", "View reports and analytics", "Reports");
    public static Permission ManageUsers => new("MANAGE_USERS", "Manage user accounts", "Administration");
    public static Permission SystemConfig => new("SYSTEM_CONFIG", "Configure system settings", "Administration");
    public static Permission EditGrades => new("EDIT_GRADES", "Edit student grades", "Academics");
    public static Permission ViewAllData => new("VIEW_ALL_DATA", "View all district data", "Administration");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => $"{Code}: {Description}";
}

/// <summary>
/// Represents the scope context for role assignments
/// </summary>
public sealed class RoleScope : ValueObject
{
    public RoleScopeType Type { get; }
    public Guid? DistrictId { get; }
    public Guid? SchoolId { get; }
    public Guid? ClassId { get; }
    public Guid? SchoolYearId { get; }

    private RoleScope(RoleScopeType type, Guid? districtId = null, Guid? schoolId = null, Guid? classId = null, Guid? schoolYearId = null)
    {
        Type = type;
        DistrictId = districtId;
        SchoolId = schoolId;
        ClassId = classId;
        SchoolYearId = schoolYearId;
    }

    /// <summary>
    /// Create a district-wide scope
    /// </summary>
    public static RoleScope District(Guid districtId) => new(RoleScopeType.District, districtId);

    /// <summary>
    /// Create a school-level scope
    /// </summary>
    public static RoleScope School(Guid districtId, Guid schoolId) => new(RoleScopeType.School, districtId, schoolId);

    /// <summary>
    /// Create a class-level scope
    /// </summary>
    public static RoleScope Class(Guid districtId, Guid schoolId, Guid classId) => new(RoleScopeType.Class, districtId, schoolId, classId);

    /// <summary>
    /// Create a school year-specific scope
    /// </summary>
    public static RoleScope SchoolYear(Guid districtId, Guid schoolYearId) => new(RoleScopeType.SchoolYear, districtId, schoolYearId: schoolYearId);

    /// <summary>
    /// Check if this scope encompasses another scope (hierarchical permission inheritance)
    /// </summary>
    public bool Encompasses(RoleScope other)
    {
        if (other == null) return false;

        // District scope encompasses all
        if (Type == RoleScopeType.District && DistrictId == other.DistrictId)
            return true;

        // School scope encompasses class scope in same school
        if (Type == RoleScopeType.School && other.Type == RoleScopeType.Class)
            return DistrictId == other.DistrictId && SchoolId == other.SchoolId;

        // Exact match
        return Equals(other);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return DistrictId;
        yield return SchoolId;
        yield return ClassId;
        yield return SchoolYearId;
    }

    public override string ToString()
    {
        return Type switch
        {
            RoleScopeType.District => $"District: {DistrictId}",
            RoleScopeType.School => $"School: {SchoolId}",
            RoleScopeType.Class => $"Class: {ClassId}",
            RoleScopeType.SchoolYear => $"SchoolYear: {SchoolYearId}",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// Role scope type enumeration
/// </summary>
public enum RoleScopeType
{
    District,
    School,
    Class,
    SchoolYear
}