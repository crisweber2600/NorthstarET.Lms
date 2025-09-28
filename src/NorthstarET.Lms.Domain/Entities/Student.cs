using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public class Student : TenantScopedEntity
{
    private readonly List<string> _accommodationTags = new();
    
    // Private constructor for EF Core
    private Student() { }

    public Student(
        string studentNumber,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        DateTime enrollmentDate)
    {
        if (string.IsNullOrWhiteSpace(studentNumber))
            throw new ArgumentException("Student number is required", nameof(studentNumber));
            
        if (dateOfBirth > DateTime.Today)
            throw new ArgumentException("Date of birth cannot be in the future", nameof(dateOfBirth));
            
        if (dateOfBirth.Year < 1950)
            throw new ArgumentException("Date of birth is unreasonably old", nameof(dateOfBirth));

        UserId = Guid.NewGuid();
        StudentNumber = studentNumber;
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        DateOfBirth = dateOfBirth;
        EnrollmentDate = enrollmentDate;
        Status = UserLifecycleStatus.Active;
        CurrentGradeLevel = CalculateGradeLevel(dateOfBirth, enrollmentDate);
    }

    public Guid UserId { get; private set; }
    public string StudentNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string? MiddleName { get; private set; }
    public string LastName { get; private set; } = string.Empty;
    public DateTime DateOfBirth { get; private set; }
    public DateTime EnrollmentDate { get; private set; }
    public UserLifecycleStatus Status { get; private set; }
    public GradeLevel CurrentGradeLevel { get; private set; }
    public DateTime? WithdrawalDate { get; private set; }
    public string? WithdrawalReason { get; private set; }
    
    // Program participation flags
    public bool IsSpecialEducation { get; private set; }
    public bool IsGifted { get; private set; }
    public bool IsEnglishLanguageLearner { get; private set; }

    public string FullName => string.IsNullOrEmpty(MiddleName) 
        ? $"{FirstName} {LastName}" 
        : $"{FirstName} {MiddleName} {LastName}";
    public IReadOnlyList<string> AccommodationTags => _accommodationTags.AsReadOnly();

    public void UpdateGradeLevel(GradeLevel newGradeLevel, string updatedByUserId)
    {
        var oldGradeLevel = CurrentGradeLevel;
        CurrentGradeLevel = newGradeLevel;
        MarkAsModified();
        
        AddDomainEvent(new StudentGradeUpdatedEvent(UserId, oldGradeLevel, newGradeLevel, updatedByUserId));
    }

    public void SetProgramParticipation(bool isSpecialEducation, bool isGifted, bool isEnglishLanguageLearner)
    {
        IsSpecialEducation = isSpecialEducation;
        IsGifted = isGifted;
        IsEnglishLanguageLearner = isEnglishLanguageLearner;
        MarkAsModified();
    }

    public void AddAccommodationTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !_accommodationTags.Contains(tag))
        {
            _accommodationTags.Add(tag);
            MarkAsModified();
        }
    }

    public void RemoveAccommodationTag(string tag)
    {
        if (_accommodationTags.Remove(tag))
        {
            MarkAsModified();
        }
    }

    public void UpdateMiddleName(string? middleName, string updatedByUserId)
    {
        MiddleName = middleName;
        MarkAsModified();
        
        AddDomainEvent(new StudentGradeUpdatedEvent(UserId, CurrentGradeLevel, CurrentGradeLevel, updatedByUserId));
    }

    public void Withdraw(DateTime withdrawalDate, string reason)
    {
        Status = UserLifecycleStatus.Withdrawn;
        WithdrawalDate = withdrawalDate;
        WithdrawalReason = reason;
        MarkAsModified();
        
        AddDomainEvent(new StudentWithdrawnEvent(UserId, withdrawalDate, reason));
    }

    private static GradeLevel CalculateGradeLevel(DateTime dateOfBirth, DateTime enrollmentDate)
    {
        // Simplified grade level calculation based on age at enrollment
        var ageAtEnrollment = enrollmentDate.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > enrollmentDate.AddYears(-ageAtEnrollment))
            ageAtEnrollment--;

        return ageAtEnrollment switch
        {
            < 5 => GradeLevel.PreK,
            5 => GradeLevel.Kindergarten,
            6 => GradeLevel.Grade1,
            7 => GradeLevel.Grade2,
            8 => GradeLevel.Grade3,
            9 => GradeLevel.Grade4,
            10 => GradeLevel.Grade5,
            11 => GradeLevel.Grade6,
            12 => GradeLevel.Grade7,
            13 => GradeLevel.Grade8,
            14 => GradeLevel.Grade9,
            15 => GradeLevel.Grade10,
            16 => GradeLevel.Grade11,
            17 => GradeLevel.Grade12,
            _ => GradeLevel.Grade12
        };
    }
}