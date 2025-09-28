using FluentAssertions;
using NorthstarET.Lms.Application.Services;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using Moq;

namespace NorthstarET.Lms.Application.Tests.Services;

public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _mockEnrollmentRepository;
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly Mock<IClassRepository> _mockClassRepository;
    private readonly Mock<ISchoolYearRepository> _mockSchoolYearRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly EnrollmentService _enrollmentService;

    public EnrollmentServiceTests()
    {
        _mockEnrollmentRepository = new Mock<IEnrollmentRepository>();
        _mockStudentRepository = new Mock<IStudentRepository>();
        _mockClassRepository = new Mock<IClassRepository>();
        _mockSchoolYearRepository = new Mock<ISchoolYearRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockAuditService = new Mock<IAuditService>();
        
        // This will fail until EnrollmentService is implemented
        _enrollmentService = new EnrollmentService(
            _mockEnrollmentRepository.Object,
            _mockStudentRepository.Object,
            _mockClassRepository.Object,
            _mockSchoolYearRepository.Object,
            _mockUnitOfWork.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task EnrollStudentAsync_WithValidData_ShouldCreateEnrollment()
    {
        // Arrange
        var enrollmentDto = new CreateEnrollmentDto
        {
            StudentId = Guid.NewGuid(),
            ClassId = Guid.NewGuid(),
            SchoolYearId = Guid.NewGuid(),
            GradeLevel = GradeLevel.Grade9,
            EnrollmentDate = DateTime.UtcNow.Date
        };

        var student = new Student("STU-001", "John", "Smith", new DateTime(2008, 1, 1), GradeLevel.Grade9);
        var classEntity = new Class("Math 101", Guid.NewGuid(), Guid.NewGuid());
        var schoolYear = new SchoolYear("2024-2025", new DateTime(2024, 8, 15), new DateTime(2025, 6, 15));

        _mockStudentRepository.Setup(x => x.GetByIdAsync(enrollmentDto.StudentId))
            .ReturnsAsync(student);
        _mockClassRepository.Setup(x => x.GetByIdAsync(enrollmentDto.ClassId))
            .ReturnsAsync(classEntity);
        _mockSchoolYearRepository.Setup(x => x.GetByIdAsync(enrollmentDto.SchoolYearId))
            .ReturnsAsync(schoolYear);
        _mockEnrollmentRepository.Setup(x => x.GetActiveEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Enrollment?)null);

        // Act & Assert - This will fail until EnrollmentService.EnrollStudentAsync is implemented
        var result = await _enrollmentService.EnrollStudentAsync(enrollmentDto, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.StudentId.Should().Be(enrollmentDto.StudentId);
        result.Value.ClassId.Should().Be(enrollmentDto.ClassId);
        
        _mockEnrollmentRepository.Verify(x => x.AddAsync(It.IsAny<Enrollment>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task EnrollStudentAsync_WithDuplicateEnrollment_ShouldReturnFailure()
    {
        // Arrange
        var enrollmentDto = new CreateEnrollmentDto
        {
            StudentId = Guid.NewGuid(),
            ClassId = Guid.NewGuid(),
            SchoolYearId = Guid.NewGuid(),
            GradeLevel = GradeLevel.Grade9,
            EnrollmentDate = DateTime.UtcNow.Date
        };

        var existingEnrollment = new Enrollment(
            enrollmentDto.StudentId,
            enrollmentDto.ClassId,
            enrollmentDto.SchoolYearId,
            enrollmentDto.GradeLevel,
            enrollmentDto.EnrollmentDate);

        _mockEnrollmentRepository.Setup(x => x.GetActiveEnrollmentAsync(
            enrollmentDto.StudentId, enrollmentDto.ClassId, enrollmentDto.SchoolYearId))
            .ReturnsAsync(existingEnrollment);

        // Act & Assert - This will fail until EnrollmentService is implemented
        var result = await _enrollmentService.EnrollStudentAsync(enrollmentDto, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already enrolled");
    }

    [Fact]
    public async Task TransferStudentAsync_WithValidData_ShouldTransferEnrollment()
    {
        // Arrange
        var transferDto = new TransferEnrollmentDto
        {
            EnrollmentId = Guid.NewGuid(),
            ToClassId = Guid.NewGuid(),
            TransferDate = DateTime.UtcNow.Date,
            Reason = "Schedule change"
        };

        var enrollment = new Enrollment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            GradeLevel.Grade9,
            DateTime.UtcNow.AddDays(-30).Date);

        var toClass = new Class("English 101", Guid.NewGuid(), Guid.NewGuid());

        _mockEnrollmentRepository.Setup(x => x.GetByIdAsync(transferDto.EnrollmentId))
            .ReturnsAsync(enrollment);
        _mockClassRepository.Setup(x => x.GetByIdAsync(transferDto.ToClassId))
            .ReturnsAsync(toClass);

        // Act & Assert - This will fail until EnrollmentService is implemented
        var result = await _enrollmentService.TransferStudentAsync(transferDto, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GraduateStudentAsync_WithValidData_ShouldGraduateEnrollment()
    {
        // Arrange
        var graduationDto = new GraduateStudentDto
        {
            EnrollmentId = Guid.NewGuid(),
            GraduationDate = DateTime.UtcNow.Date
        };

        var enrollment = new Enrollment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            GradeLevel.Grade12,
            DateTime.UtcNow.AddMonths(-9).Date);

        _mockEnrollmentRepository.Setup(x => x.GetByIdAsync(graduationDto.EnrollmentId))
            .ReturnsAsync(enrollment);

        // Act & Assert - This will fail until EnrollmentService is implemented
        var result = await _enrollmentService.GraduateStudentAsync(graduationDto, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetClassRosterAsync_WithValidClassId_ShouldReturnRoster()
    {
        // Arrange
        var classId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        var expectedEnrollments = new List<Enrollment>
        {
            new(Guid.NewGuid(), classId, schoolYearId, GradeLevel.Grade9, DateTime.UtcNow.AddDays(-30).Date),
            new(Guid.NewGuid(), classId, schoolYearId, GradeLevel.Grade9, DateTime.UtcNow.AddDays(-25).Date)
        };

        _mockEnrollmentRepository.Setup(x => x.GetByClassAndSchoolYearAsync(classId, schoolYearId))
            .ReturnsAsync(expectedEnrollments);

        // Act & Assert - This will fail until EnrollmentService is implemented
        var result = await _enrollmentService.GetClassRosterAsync(classId, schoolYearId);
        
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(x => x.ClassId == classId).Should().BeTrue();
        result.All(x => x.SchoolYearId == schoolYearId).Should().BeTrue();
    }

    [Fact]
    public async Task BulkRolloverAsync_WithValidData_ShouldPromoteStudents()
    {
        // Arrange
        var rolloverDto = new BulkRolloverDto
        {
            FromSchoolYearId = Guid.NewGuid(),
            ToSchoolYearId = Guid.NewGuid(),
            GradeLevelMappings = new Dictionary<GradeLevel, GradeLevel>
            {
                { GradeLevel.Grade9, GradeLevel.Grade10 },
                { GradeLevel.Grade10, GradeLevel.Grade11 }
            }
        };

        var enrollmentsToRollover = new List<Enrollment>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), rolloverDto.FromSchoolYearId, GradeLevel.Grade9, DateTime.UtcNow.AddMonths(-9).Date),
            new(Guid.NewGuid(), Guid.NewGuid(), rolloverDto.FromSchoolYearId, GradeLevel.Grade10, DateTime.UtcNow.AddMonths(-9).Date)
        };

        _mockEnrollmentRepository.Setup(x => x.GetBySchoolYearAsync(rolloverDto.FromSchoolYearId))
            .ReturnsAsync(enrollmentsToRollover);

        // Act & Assert - This will fail until EnrollmentService is implemented
        var result = await _enrollmentService.BulkRolloverAsync(rolloverDto, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ProcessedCount.Should().Be(2);
        result.Value.SuccessCount.Should().Be(2);
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
    }
}