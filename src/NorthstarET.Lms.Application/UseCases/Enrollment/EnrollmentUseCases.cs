using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Application.UseCases.Enrollment;

public class EnrollStudentUseCase
{
    private readonly IStudentRepository _studentRepository;
    private readonly IClassRepository _classRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAuditService _auditService;

    public EnrollStudentUseCase(
        IStudentRepository studentRepository,
        IClassRepository classRepository,
        IEnrollmentRepository enrollmentRepository,
        IAuditService auditService)
    {
        _studentRepository = studentRepository;
        _classRepository = classRepository;
        _enrollmentRepository = enrollmentRepository;
        _auditService = auditService;
    }

    public async Task<Result<EnrollmentDto>> ExecuteAsync(
        EnrollStudentRequest request,
        string enrolledByUserId)
    {
        // Validate student exists
        var student = await _studentRepository.GetByIdAsync(request.StudentId);
        if (student == null)
        {
            return Result<EnrollmentDto>.Failure("Student not found");
        }

        // Validate class exists
        var classEntity = await _classRepository.GetByIdAsync(request.ClassId);
        if (classEntity == null)
        {
            return Result<EnrollmentDto>.Failure("Class not found");
        }

        // Check for existing active enrollment
        var existingEnrollment = await _enrollmentRepository.GetActiveEnrollmentAsync(
            request.StudentId, request.ClassId);
        if (existingEnrollment != null)
        {
            return Result<EnrollmentDto>.Failure("Student is already enrolled in this class");
        }

        // Create enrollment
        var enrollment = new Domain.Entities.Enrollment(
            request.StudentId,
            request.ClassId,
            classEntity.SchoolYearId,
            Enum.Parse<GradeLevel>(request.GradeLevel),
            request.EnrollmentDate);

        await _enrollmentRepository.AddAsync(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        // Generate audit record
        await _auditService.LogAsync(
            "StudentEnrolled",
            typeof(Domain.Entities.Enrollment).Name,
            enrollment.Id,
            enrolledByUserId,
            new { request.StudentId, request.ClassId, request.GradeLevel });

        var dto = new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            ClassId = enrollment.ClassId,
            SchoolYearId = enrollment.SchoolYearId,
            GradeLevel = enrollment.GradeLevel.ToString(),
            Status = enrollment.Status.ToString(),
            EnrollmentDate = enrollment.EnrollmentDate
        };

        return Result<EnrollmentDto>.Success(dto);
    }
}

public class WithdrawStudentUseCase
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAuditService _auditService;

    public WithdrawStudentUseCase(
        IEnrollmentRepository enrollmentRepository,
        IAuditService auditService)
    {
        _enrollmentRepository = enrollmentRepository;
        _auditService = auditService;
    }

    public async Task<Result> ExecuteAsync(
        WithdrawStudentRequest request,
        string withdrawnByUserId)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(request.EnrollmentId);
        if (enrollment == null)
        {
            return Result.Failure("Enrollment not found");
        }

        if (enrollment.Status != EnrollmentStatus.Active)
        {
            return Result.Failure("Can only withdraw active enrollments");
        }

        enrollment.Withdraw(request.WithdrawalDate, request.WithdrawalReason, withdrawnByUserId);
        await _enrollmentRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            "StudentWithdrawn",
            typeof(Domain.Entities.Enrollment).Name,
            enrollment.Id,
            withdrawnByUserId,
            new { enrollment.StudentId, enrollment.ClassId, request.WithdrawalReason });

        return Result.Success();
    }
}

public class TransferStudentUseCase
{
    private readonly IStudentRepository _studentRepository;
    private readonly IClassRepository _classRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAuditService _auditService;

    public TransferStudentUseCase(
        IStudentRepository studentRepository,
        IClassRepository classRepository,
        IEnrollmentRepository enrollmentRepository,
        IAuditService auditService)
    {
        _studentRepository = studentRepository;
        _classRepository = classRepository;
        _enrollmentRepository = enrollmentRepository;
        _auditService = auditService;
    }

    public async Task<Result<TransferResultDto>> ExecuteAsync(
        TransferStudentRequest request,
        string transferredByUserId)
    {
        var student = await _studentRepository.GetByIdAsync(request.StudentId);
        if (student == null)
        {
            return Result<TransferResultDto>.Failure("Student not found");
        }

        // Get current enrollments in the from school
        var currentEnrollments = await _enrollmentRepository.GetActiveEnrollmentsByStudentAndSchoolAsync(
            request.StudentId, request.FromSchoolId);

        var transferResults = new List<EnrollmentTransferDto>();

        // Process each class transfer
        foreach (var classTransfer in request.TransferClasses)
        {
            var currentEnrollment = currentEnrollments.FirstOrDefault(e => e.ClassId == classTransfer.FromClassId);
            if (currentEnrollment == null)
            {
                transferResults.Add(new EnrollmentTransferDto
                {
                    FromClassId = classTransfer.FromClassId,
                    ToClassId = classTransfer.ToClassId,
                    Success = false,
                    Error = "Student not enrolled in source class"
                });
                continue;
            }

            // Validate target class exists
            var targetClass = await _classRepository.GetByIdAsync(classTransfer.ToClassId);
            if (targetClass == null)
            {
                transferResults.Add(new EnrollmentTransferDto
                {
                    FromClassId = classTransfer.FromClassId,
                    ToClassId = classTransfer.ToClassId,
                    Success = false,
                    Error = "Target class not found"
                });
                continue;
            }

            // Withdraw from current class
            currentEnrollment.Withdraw(request.EffectiveDate, $"Transfer to {targetClass.Name}", transferredByUserId);

            // Enroll in new class
            var gradeLevel = request.MaintainGradeLevel ? currentEnrollment.GradeLevel : targetClass.GradeLevel;
            var newEnrollment = new Domain.Entities.Enrollment(
                request.StudentId,
                classTransfer.ToClassId,
                targetClass.SchoolYearId,
                gradeLevel,
                request.EffectiveDate);

            await _enrollmentRepository.AddAsync(newEnrollment);

            transferResults.Add(new EnrollmentTransferDto
            {
                FromClassId = classTransfer.FromClassId,
                ToClassId = classTransfer.ToClassId,
                NewEnrollmentId = newEnrollment.Id,
                Success = true
            });
        }

        await _enrollmentRepository.SaveChangesAsync();

        // Generate audit record
        await _auditService.LogAsync(
            "StudentTransferred",
            typeof(Student).Name,
            student.UserId,
            transferredByUserId,
            new { request.FromSchoolId, request.ToSchoolId, request.Reason, TransferResults = transferResults });

        var resultDto = new TransferResultDto
        {
            StudentId = request.StudentId,
            TransferDate = request.EffectiveDate,
            Reason = request.Reason,
            EnrollmentTransfers = transferResults
        };

        return Result<TransferResultDto>.Success(resultDto);
    }
}