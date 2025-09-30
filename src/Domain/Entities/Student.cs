using NorthstarET.Lms.Domain.Common;
using NorthstarET.Lms.Domain.ValueObjects;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

/// <summary>
/// Represents a student with grade progression and program participation tracking
/// </summary>
public class Student : TenantScopedEntity
{
    /// <summary>
    /// Internal user ID linking to identity system
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// District-unique student number
    /// </summary>
    public string StudentNumber { get; private set; }

    /// <summary>
    /// Student's first name
    /// </summary>
    public string FirstName { get; private set; }

    /// <summary>
    /// Student's last name
    /// </summary>
    public string LastName { get; private set; }

    /// <summary>
    /// Student's date of birth
    /// </summary>
    public DateTime DateOfBirth { get; private set; }

    /// <summary>
    /// Current grade level
    /// </summary>
    public GradeLevel GradeLevel { get; private set; }

    /// <summary>
    /// Special program flags
    /// </summary>
    private readonly List<ProgramFlag> _programFlags = new();
    public IReadOnlyList<ProgramFlag> ProgramFlags => _programFlags.AsReadOnly();

    /// <summary>
    /// Accommodation tags
    /// </summary>
    private readonly List<AccommodationTag> _accommodationTags = new();
    public IReadOnlyList<AccommodationTag> AccommodationTags => _accommodationTags.AsReadOnly();

    /// <summary>
    /// Current status of the student
    /// </summary>
    public StudentStatus Status { get; private set; }

    /// <summary>
    /// Full name for display purposes
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    // EF Core constructor
    protected Student()
    {
        StudentNumber = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        GradeLevel = GradeLevel.Kindergarten;
    }

    /// <summary>
    /// Create a new student
    /// </summary>
    public Student(
        string tenantSlug,
        Guid userId,
        string studentNumber,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        GradeLevel gradeLevel,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(studentNumber))
            throw new ArgumentException("Student number cannot be null or empty", nameof(studentNumber));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be null or empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be null or empty", nameof(lastName));

        if (dateOfBirth >= DateTime.Today)
            throw new ArgumentException("Date of birth must be in the past", nameof(dateOfBirth));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        UserId = userId;
        StudentNumber = studentNumber.Trim();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth.Date;
        GradeLevel = gradeLevel ?? throw new ArgumentNullException(nameof(gradeLevel));
        Status = StudentStatus.Active;

        InitializeTenant(tenantSlug);
        SetAuditFields(createdBy);
    }

    /// <summary>
    /// Promote student to the next grade level
    /// </summary>
    public void PromoteGrade(Guid schoolYearId, string updatedBy)
    {
        if (Status != StudentStatus.Active)
            throw new InvalidOperationException("Only active students can be promoted");

        var oldGrade = GradeLevel;
        
        if (GradeLevel.IsGraduating)
        {
            Graduate(schoolYearId, updatedBy);
            return;
        }

        GradeLevel = GradeLevel.GetNextGrade();
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new StudentPromotedEvent(Id, oldGrade, GradeLevel, schoolYearId)
        {
            TenantSlug = TenantSlug,
            TriggeredBy = updatedBy
        });
    }

    /// <summary>
    /// Graduate the student
    /// </summary>
    public void Graduate(Guid schoolYearId, string updatedBy)
    {
        if (Status != StudentStatus.Active)
            throw new InvalidOperationException("Only active students can graduate");

        Status = StudentStatus.Graduated;
        UpdateAuditFields(updatedBy);

        AddDomainEvent(new StudentGraduatedEvent(Id, DateTime.UtcNow, schoolYearId)
        {
            TenantSlug = TenantSlug,
            TriggeredBy = updatedBy
        });
    }

    /// <summary>
    /// Add a program flag
    /// </summary>
    public void AddProgramFlag(ProgramFlag programFlag, string updatedBy)
    {
        if (programFlag == null)
            throw new ArgumentNullException(nameof(programFlag));

        if (_programFlags.Any(p => p.Code == programFlag.Code))
            return; // Already has this flag

        _programFlags.Add(programFlag);
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Remove a program flag
    /// </summary>
    public void RemoveProgramFlag(string programCode, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(programCode))
            throw new ArgumentException("Program code cannot be null or empty", nameof(programCode));

        var flag = _programFlags.FirstOrDefault(p => p.Code == programCode.ToUpperInvariant());
        if (flag != null)
        {
            _programFlags.Remove(flag);
            UpdateAuditFields(updatedBy);
        }
    }

    /// <summary>
    /// Add an accommodation tag
    /// </summary>
    public void AddAccommodationTag(AccommodationTag accommodationTag, string updatedBy)
    {
        if (accommodationTag == null)
            throw new ArgumentNullException(nameof(accommodationTag));

        if (_accommodationTags.Any(a => a.Code == accommodationTag.Code))
            return; // Already has this accommodation

        _accommodationTags.Add(accommodationTag);
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Remove an accommodation tag
    /// </summary>
    public void RemoveAccommodationTag(string accommodationCode, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(accommodationCode))
            throw new ArgumentException("Accommodation code cannot be null or empty", nameof(accommodationCode));

        var tag = _accommodationTags.FirstOrDefault(a => a.Code == accommodationCode.ToUpperInvariant());
        if (tag != null)
        {
            _accommodationTags.Remove(tag);
            UpdateAuditFields(updatedBy);
        }
    }

    /// <summary>
    /// Update student's personal information
    /// </summary>
    public void UpdatePersonalInfo(string firstName, string lastName, DateTime dateOfBirth, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be null or empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be null or empty", nameof(lastName));

        if (dateOfBirth >= DateTime.Today)
            throw new ArgumentException("Date of birth must be in the past", nameof(dateOfBirth));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth.Date;
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Withdraw the student
    /// </summary>
    public void Withdraw(string updatedBy)
    {
        if (Status == StudentStatus.Withdrawn)
            throw new InvalidOperationException("Student is already withdrawn");

        Status = StudentStatus.Withdrawn;
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Reactivate a withdrawn student
    /// </summary>
    public void Reactivate(string updatedBy)
    {
        if (Status == StudentStatus.Active)
            throw new InvalidOperationException("Student is already active");

        if (Status == StudentStatus.Graduated)
            throw new InvalidOperationException("Cannot reactivate a graduated student");

        Status = StudentStatus.Active;
        UpdateAuditFields(updatedBy);
    }

    /// <summary>
    /// Calculate student's age as of a given date
    /// </summary>
    public int CalculateAge(DateTime asOfDate)
    {
        var age = asOfDate.Year - DateOfBirth.Year;
        if (asOfDate.Date < DateOfBirth.AddYears(age))
            age--;
        return age;
    }

    /// <summary>
    /// Check if student is eligible for enrollment
    /// </summary>
    public bool IsEligibleForEnrollment()
    {
        return Status == StudentStatus.Active;
    }
}

/// <summary>
/// Student status enumeration
/// </summary>
public enum StudentStatus
{
    Active,
    Withdrawn,
    Graduated
}