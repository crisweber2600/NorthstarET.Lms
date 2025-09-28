using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Application.Services;

public class StudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ITenantContextAccessor _tenantContext;

    public StudentService(
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ITenantContextAccessor tenantContext)
    {
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _tenantContext = tenantContext;
    }

    public async Task<Result<StudentDto>> CreateStudentAsync(CreateStudentDto createStudentDto, string createdBy)
    {
        // Check for duplicate student number
        var existingStudent = await _studentRepository.GetByStudentNumberAsync(createStudentDto.StudentNumber);
        if (existingStudent != null)
        {
            return Result.Failure<StudentDto>($"Student with number '{createStudentDto.StudentNumber}' already exists");
        }

        // Create student entity
        var student = new Student(
            createStudentDto.StudentNumber,
            createStudentDto.FirstName,
            createStudentDto.LastName,
            createStudentDto.DateOfBirth,
            DateTime.UtcNow.Date); // Use current date as enrollment date
            
        // Update grade level after creation
        if (createStudentDto.CurrentGradeLevel != student.CurrentGradeLevel)
        {
            student.UpdateGradeLevel(createStudentDto.CurrentGradeLevel, createdBy);
        }

        if (!string.IsNullOrEmpty(createStudentDto.MiddleName))
        {
            student.UpdateMiddleName(createStudentDto.MiddleName, createdBy);
        }

        // Save to repository
        await _studentRepository.AddAsync(student);
        await _unitOfWork.SaveChangesAsync();

        // Create audit record
        await _auditService.LogAuditEventAsync(new CreateAuditRecordDto
        {
            Action = "CREATE_STUDENT",
            EntityType = "Student",
            EntityId = student.UserId,
            UserId = createdBy,
            Details = $"Created student: {createStudentDto.FirstName} {createStudentDto.LastName} ({createStudentDto.StudentNumber})",
            IpAddress = "127.0.0.1" // TODO: Get from HTTP context
        });

        return Result.Success(MapToDto(student));
    }

    public async Task<StudentDto?> GetStudentAsync(Guid studentId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        return student != null ? MapToDto(student) : null;
    }

    public async Task<Result<StudentDto>> UpdateStudentGradeLevelAsync(Guid studentId, GradeLevel newGradeLevel, string updatedBy)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            return Result.Failure<StudentDto>("Student not found");
        }

        var oldGradeLevel = student.CurrentGradeLevel;
        student.UpdateGradeLevel(newGradeLevel, updatedBy);
        
        await _studentRepository.UpdateAsync(student);
        await _unitOfWork.SaveChangesAsync();

        // Audit the change
        await _auditService.LogAuditEventAsync(new CreateAuditRecordDto
        {
            Action = "UPDATE_STUDENT_GRADE",
            EntityType = "Student",
            EntityId = student.UserId,
            UserId = updatedBy,
            Details = $"Updated grade level from {oldGradeLevel} to {newGradeLevel}",
            IpAddress = "127.0.0.1"
        });

        return Result.Success(MapToDto(student));
    }

    public async Task<Result<StudentDto>> WithdrawStudentAsync(Guid studentId, DateTime withdrawalDate, string reason, string withdrawnBy)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            return Result.Failure<StudentDto>("Student not found");
        }

        student.Withdraw(withdrawalDate, reason);
        
        await _studentRepository.UpdateAsync(student);
        await _unitOfWork.SaveChangesAsync();

        // Audit the withdrawal
        await _auditService.LogAuditEventAsync(new CreateAuditRecordDto
        {
            Action = "WITHDRAW_STUDENT",
            EntityType = "Student",
            EntityId = student.UserId,
            UserId = withdrawnBy,
            Details = $"Withdrew student: {reason}",
            IpAddress = "127.0.0.1"
        });

        return Result.Success(MapToDto(student));
    }

    public async Task<PagedResult<StudentDto>> SearchStudentsAsync(StudentSearchDto searchDto)
    {
        var studentResults = await _studentRepository.SearchAsync(searchDto);
        
        var studentDtos = studentResults.Items.Select(MapToDto);
        
        return new PagedResult<StudentDto>(
            studentDtos,
            studentResults.Page,
            studentResults.PageSize,
            studentResults.TotalCount);
    }

    private static StudentDto MapToDto(Student student)
    {
        return new StudentDto
        {
            UserId = student.UserId,
            StudentNumber = student.StudentNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            MiddleName = student.MiddleName,
            DateOfBirth = student.DateOfBirth,
            CurrentGradeLevel = student.CurrentGradeLevel,
            Status = student.Status,
            WithdrawalDate = student.WithdrawalDate,
            WithdrawalReason = student.WithdrawalReason
        };
    }
}