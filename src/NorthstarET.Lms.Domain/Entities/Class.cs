using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Domain.Entities;

public class Class : TenantScopedEntity
{
    // Private constructor for EF Core
    private Class() { }

    public Class(
        string code,
        string name,
        string subject,
        GradeLevel gradeLevel,
        Guid schoolId,
        Guid schoolYearId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Class code is required", nameof(code));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Class name is required", nameof(name));
        
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required", nameof(subject));
        
        if (schoolId == Guid.Empty)
            throw new ArgumentException("SchoolId cannot be empty", nameof(schoolId));
        
        if (schoolYearId == Guid.Empty)
            throw new ArgumentException("SchoolYearId cannot be empty", nameof(schoolYearId));

        Code = code;
        Name = name;
        Subject = subject;
        GradeLevel = gradeLevel;
        SchoolId = schoolId;
        SchoolYearId = schoolYearId;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public GradeLevel GradeLevel { get; private set; }
    
    // Foreign keys
    public Guid SchoolId { get; private set; }
    public Guid SchoolYearId { get; private set; }
    
    // Navigation properties
    public School School { get; private set; } = null!;
    public SchoolYear SchoolYear { get; private set; } = null!;
    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();
}

public class School : TenantScopedEntity
{
    // Private constructor for EF Core
    private School() { }

    public School(string code, string name, SchoolType type)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("School code is required", nameof(code));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("School name is required", nameof(name));

        Code = code;
        Name = name;
        Type = type;
        IsActive = true;
        EstablishedDate = DateTime.UtcNow.Date;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public SchoolType Type { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime EstablishedDate { get; private set; }
    
    // Navigation properties
    public ICollection<Class> Classes { get; private set; } = new List<Class>();
}