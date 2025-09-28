using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Application.UseCases.Students;

public class CreateStudentUseCase
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IAuditService _auditService;

    public CreateStudentUseCase(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IAuditService auditService)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _auditService = auditService;
    }

    public async Task<Result<StudentDto>> ExecuteAsync(CreateStudentRequest request, string createdByUserId)
    {
        // Validate business rules
        if (await _studentRepository.StudentNumberExistsAsync(request.StudentNumber))
        {
            return Result.Failure<StudentDto>("Student number already exists");
        }

        // Create student entity
        var student = new Student(
            request.StudentNumber,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.EnrollmentDate);

        // Set program flags
        student.SetProgramParticipation(
            request.Programs.IsSpecialEducation,
            request.Programs.IsGifted,
            request.Programs.IsEnglishLanguageLearner);

        // Add accommodation tags
        foreach (var tag in request.Programs.AccommodationTags)
        {
            student.AddAccommodationTag(tag);
        }

        // Persist the student
        await _studentRepository.AddAsync(student);

        // Create guardian relationships (simplified - skip complex relationships for now)
        // TODO: Implement full guardian relationship management
        if (request.Guardians.Length > 0)
        {
            // For now, just create simple guardian records without complex relationships
            foreach (var guardianRequest in request.Guardians)
            {
                var guardian = new Guardian(
                    guardianRequest.FirstName,
                    guardianRequest.LastName,
                    guardianRequest.Email,
                    guardianRequest.Phone);

                await _guardianRepository.AddAsync(guardian);
                // Skip the relationship part for now - needs more complex entity design
            }
        }

        await _studentRepository.SaveChangesAsync();

        // Generate audit record
        await _auditService.LogAsync(
            "StudentCreated",
            typeof(Student).Name,
            student.UserId,
            createdByUserId,
            new { request.StudentNumber, request.FirstName, request.LastName });

        // Map to DTO
        var dto = new StudentDto
        {
            UserId = student.UserId,
            StudentNumber = student.StudentNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            DateOfBirth = student.DateOfBirth,
            Status = student.Status.ToString(),
            EnrollmentDate = student.EnrollmentDate,
            Programs = new StudentProgramsDto
            {
                IsSpecialEducation = student.IsSpecialEducation,
                IsGifted = student.IsGifted,
                IsEnglishLanguageLearner = student.IsEnglishLanguageLearner,
                AccommodationTags = student.AccommodationTags.ToArray()
            }
        };

        return Result<StudentDto>.Success(dto);
    }
}

public class GetStudentUseCase
{
    private readonly IStudentRepository _studentRepository;

    public GetStudentUseCase(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<Result<StudentDetailDto>> ExecuteAsync(Guid userId)
    {
        var student = await _studentRepository.GetByIdWithDetailsAsync(userId);
        if (student == null)
        {
            return Result.Failure<StudentDetailDto>("Student not found");
        }

        var dto = new StudentDetailDto
        {
            UserId = student.UserId,
            StudentNumber = student.StudentNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            DateOfBirth = student.DateOfBirth,
            Status = student.Status.ToString(),
            EnrollmentDate = student.EnrollmentDate,
            CurrentGradeLevel = student.CurrentGradeLevel.ToString(),
            Programs = new StudentProgramsDto
            {
                IsSpecialEducation = student.IsSpecialEducation,
                IsGifted = student.IsGifted,
                IsEnglishLanguageLearner = student.IsEnglishLanguageLearner,
                AccommodationTags = student.AccommodationTags.ToArray()
            },
            CurrentEnrollments = new List<EnrollmentDto>() // Simplified - no enrollments navigation
        };

        return Result<StudentDetailDto>.Success(dto);
    }
}

public class UpdateStudentProgramsUseCase
{
    private readonly IStudentRepository _studentRepository;
    private readonly IAuditService _auditService;

    public UpdateStudentProgramsUseCase(
        IStudentRepository studentRepository,
        IAuditService auditService)
    {
        _studentRepository = studentRepository;
        _auditService = auditService;
    }

    public async Task<Result> ExecuteAsync(
        Guid userId,
        UpdateStudentProgramsRequest request,
        string updatedByUserId)
    {
        var student = await _studentRepository.GetByIdAsync(userId);
        if (student == null)
        {
            return Result.Failure("Student not found");
        }

        var oldPrograms = new
        {
            IsSpecialEducation = student.IsSpecialEducation,
            IsGifted = student.IsGifted,
            IsEnglishLanguageLearner = student.IsEnglishLanguageLearner,
            AccommodationTags = student.AccommodationTags.ToArray()
        };

        student.SetProgramParticipation(
            request.IsSpecialEducation,
            request.IsGifted,
            request.IsEnglishLanguageLearner);

        // Update accommodation tags
        foreach (var tag in request.AccommodationTags)
        {
            student.AddAccommodationTag(tag);
        }

        await _studentRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            "StudentProgramsUpdated",
            typeof(Student).Name,
            student.UserId,
            updatedByUserId,
            new { OldPrograms = oldPrograms, NewPrograms = request });

        return Result.Success();
    }
}