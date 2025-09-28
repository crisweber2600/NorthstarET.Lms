using FluentAssertions;
using NorthstarET.Lms.Application.Services;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using Moq;

namespace NorthstarET.Lms.Application.Tests.Services;

public class StudentServiceTests
{
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ITenantContextAccessor> _mockTenantContext;
    private readonly StudentService _studentService;

    public StudentServiceTests()
    {
        _mockStudentRepository = new Mock<IStudentRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockAuditService = new Mock<IAuditService>();
        _mockTenantContext = new Mock<ITenantContextAccessor>();
        
        // This will fail until StudentService is implemented
        _studentService = new StudentService(
            _mockStudentRepository.Object,
            _mockUnitOfWork.Object,
            _mockAuditService.Object,
            _mockTenantContext.Object);
    }

    [Fact]
    public async Task CreateStudentAsync_WithValidData_ShouldCreateStudent()
    {
        // Arrange
        var createStudentDto = new CreateStudentDto
        {
            StudentNumber = "STU-2024-001",
            FirstName = "John",
            LastName = "Smith",
            CurrentGradeLevel = GradeLevel.Grade9,
            DateOfBirth = new DateTime(2008, 5, 15)
        };

        _mockStudentRepository.Setup(x => x.GetByStudentNumberAsync(It.IsAny<string>()))
            .ReturnsAsync((Student?)null);

        // Act & Assert - This will fail until StudentService.CreateStudentAsync is implemented
        var result = await _studentService.CreateStudentAsync(createStudentDto, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.StudentNumber.Should().Be("STU-2024-001");
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Smith");
        
        _mockStudentRepository.Verify(x => x.AddAsync(It.IsAny<Student>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateStudentAsync_WithDuplicateStudentNumber_ShouldReturnFailure()
    {
        // Arrange
        var createStudentDto = new CreateStudentDto
        {
            StudentNumber = "EXISTING-001",
            FirstName = "Jane",
            LastName = "Doe"
        };

        var existingStudent = new Student("EXISTING-001", "Existing", "Student", new DateTime(2008, 1, 1), DateTime.UtcNow.Date);
        _mockStudentRepository.Setup(x => x.GetByStudentNumberAsync("EXISTING-001"))
            .ReturnsAsync(existingStudent);

        // Act & Assert - This will fail until StudentService is implemented
        var result = await _studentService.CreateStudentAsync(createStudentDto, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task GetStudentAsync_WithValidId_ShouldReturnStudent()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var expectedStudent = new Student("STU-001", "Test", "Student", new DateTime(2008, 1, 1), DateTime.UtcNow.Date);
        expectedStudent.UpdateGradeLevel(GradeLevel.Grade9, "system");
        
        _mockStudentRepository.Setup(x => x.GetByIdAsync(studentId))
            .ReturnsAsync(expectedStudent);

        // Act & Assert - This will fail until StudentService is implemented
        var result = await _studentService.GetStudentAsync(studentId);
        
        result.Should().NotBeNull();
        result.StudentNumber.Should().Be("STU-001");
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("Student");
    }

    [Fact]
    public async Task UpdateStudentGradeLevelAsync_WithValidData_ShouldUpdateGradeLevel()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student("STU-001", "Test", "Student", new DateTime(2008, 1, 1), DateTime.UtcNow.Date);
        student.UpdateGradeLevel(GradeLevel.Grade9, "system");
        var newGradeLevel = GradeLevel.Grade10;

        _mockStudentRepository.Setup(x => x.GetByIdAsync(studentId))
            .ReturnsAsync(student);

        // Act & Assert - This will fail until StudentService is implemented
        var result = await _studentService.UpdateStudentGradeLevelAsync(studentId, newGradeLevel, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task WithdrawStudentAsync_WithValidData_ShouldWithdrawStudent()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student("STU-001", "Test", "Student", new DateTime(2008, 1, 1), DateTime.UtcNow.Date);
        student.UpdateGradeLevel(GradeLevel.Grade9, "system");
        var withdrawalDate = DateTime.UtcNow.Date;
        var reason = "Family relocation";

        _mockStudentRepository.Setup(x => x.GetByIdAsync(studentId))
            .ReturnsAsync(student);

        // Act & Assert - This will fail until StudentService is implemented
        var result = await _studentService.WithdrawStudentAsync(studentId, withdrawalDate, reason, "district-admin-1");
        
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SearchStudentsAsync_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var searchDto = new StudentSearchDto
        {
            GradeLevel = GradeLevel.Grade9,
            Status = UserLifecycleStatus.Active,
            Page = 1,
            PageSize = 20
        };

        var student1 = new Student("STU-001", "John", "Smith", new DateTime(2008, 1, 1), DateTime.UtcNow.Date);
        student1.UpdateGradeLevel(GradeLevel.Grade9, "system");
        var student2 = new Student("STU-002", "Jane", "Doe", new DateTime(2008, 2, 1), DateTime.UtcNow.Date);
        student2.UpdateGradeLevel(GradeLevel.Grade9, "system");
        
        var expectedStudents = new List<Student> { student1, student2 };

        _mockStudentRepository.Setup(x => x.SearchAsync(It.IsAny<StudentSearchDto>()))
            .ReturnsAsync(new PagedResult<Student>(expectedStudents, 1, 20, 2));

        // Act & Assert - This will fail until StudentService is implemented
        var result = await _studentService.SearchStudentsAsync(searchDto);
        
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.First().StudentNumber.Should().Be("STU-001");
    }
}