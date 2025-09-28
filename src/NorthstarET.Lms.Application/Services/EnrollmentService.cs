using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Application.Services;

public class EnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IClassRepository _classRepository;
    private readonly ISchoolYearRepository _schoolYearRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        IClassRepository classRepository,
        ISchoolYearRepository schoolYearRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _classRepository = classRepository;
        _schoolYearRepository = schoolYearRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<Result<EnrollmentDto>> EnrollStudentAsync(CreateEnrollmentDto enrollmentDto, string createdBy)
    {
        // Validate student exists
        var student = await _studentRepository.GetByIdAsync(enrollmentDto.StudentId);
        if (student == null)
        {
            return Result.Failure<EnrollmentDto>("Student not found");
        }

        // Validate class exists
        var classEntity = await _classRepository.GetByIdAsync(enrollmentDto.ClassId);
        if (classEntity == null)
        {
            return Result.Failure<EnrollmentDto>("Class not found");
        }

        // Validate school year exists
        var schoolYear = await _schoolYearRepository.GetByIdAsync(enrollmentDto.SchoolYearId);
        if (schoolYear == null)
        {
            return Result.Failure<EnrollmentDto>("School year not found");
        }

        // Check for duplicate enrollment
        var existingEnrollment = await _enrollmentRepository.GetActiveEnrollmentAsync(
            enrollmentDto.StudentId, enrollmentDto.ClassId);
        if (existingEnrollment != null)
        {
            return Result.Failure<EnrollmentDto>("Student is already enrolled in this class for the school year");
        }

        // Create enrollment
        var enrollment = new Enrollment(
            enrollmentDto.StudentId,
            enrollmentDto.ClassId,
            enrollmentDto.SchoolYearId,
            enrollmentDto.GradeLevel,
            enrollmentDto.EnrollmentDate);

        await _enrollmentRepository.AddAsync(enrollment);
        await _unitOfWork.SaveChangesAsync();

        // Audit the enrollment
        await _auditService.LogAuditEventAsync(new CreateAuditRecordDto
        {
            Action = "ENROLL_STUDENT",
            EntityType = "Enrollment",
            EntityId = enrollment.Id,
            UserId = createdBy,
            Details = $"Enrolled student {student.StudentNumber} in class {classEntity.Name}",
            IpAddress = "127.0.0.1"
        });

        return Result.Success(MapToDto(enrollment));
    }

    public async Task<Result<EnrollmentDto>> TransferStudentAsync(TransferEnrollmentDto transferDto, string transferredBy)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(transferDto.EnrollmentId);
        if (enrollment == null)
        {
            return Result.Failure<EnrollmentDto>("Enrollment not found");
        }

        var toClass = await _classRepository.GetByIdAsync(transferDto.ToClassId);
        if (toClass == null)
        {
            return Result.Failure<EnrollmentDto>("Target class not found");
        }

        enrollment.Transfer(transferDto.ToClassId, transferDto.TransferDate, transferDto.Reason, transferredBy);
        
        await _enrollmentRepository.UpdateAsync(enrollment);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(MapToDto(enrollment));
    }

    public async Task<Result<EnrollmentDto>> GraduateStudentAsync(GraduateStudentDto graduationDto, string graduatedBy)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(graduationDto.EnrollmentId);
        if (enrollment == null)
        {
            return Result.Failure<EnrollmentDto>("Enrollment not found");
        }

        enrollment.Graduate(graduationDto.GraduationDate, graduatedBy);
        
        await _enrollmentRepository.UpdateAsync(enrollment);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(MapToDto(enrollment));
    }

    public async Task<IEnumerable<EnrollmentDto>> GetClassRosterAsync(Guid classId, Guid schoolYearId)
    {
        var enrollments = await _enrollmentRepository.GetByClassAndSchoolYearAsync(classId, schoolYearId);
        return enrollments.Select(MapToDto);
    }

    public async Task<Result<BulkRolloverResultDto>> BulkRolloverAsync(BulkRolloverDto rolloverDto, string initiatedBy)
    {
        var enrollments = await _enrollmentRepository.GetBySchoolYearAsync(rolloverDto.FromSchoolYearId);
        
        var result = new BulkRolloverResultDto
        {
            ProcessedCount = enrollments.Count(),
            SuccessCount = 0,
            FailedCount = 0,
            Errors = new List<string>()
        };

        foreach (var enrollment in enrollments)
        {
            try
            {
                if (rolloverDto.GradeLevelMappings.TryGetValue(enrollment.GradeLevel, out var newGradeLevel))
                {
                    var newEnrollment = new Enrollment(
                        enrollment.StudentId,
                        enrollment.ClassId, // TODO: This should probably map to new classes too
                        rolloverDto.ToSchoolYearId,
                        newGradeLevel,
                        DateTime.UtcNow.Date);

                    await _enrollmentRepository.AddAsync(newEnrollment);
                    result.SuccessCount++;
                }
                else
                {
                    result.FailedCount++;
                    result.Errors.Add($"No grade level mapping for {enrollment.GradeLevel}");
                }
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"Failed to rollover enrollment {enrollment.Id}: {ex.Message}");
            }
        }

        if (result.SuccessCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return Result.Success(result);
    }

    private static EnrollmentDto MapToDto(Enrollment enrollment)
    {
        return new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            ClassId = enrollment.ClassId,
            SchoolYearId = enrollment.SchoolYearId,
            GradeLevel = enrollment.GradeLevel.ToString(),
            EnrollmentDate = enrollment.EnrollmentDate,
            Status = enrollment.Status.ToString(),
            CompletionDate = enrollment.CompletionDate,
            CompletionReason = enrollment.CompletionReason
        };
    }
}