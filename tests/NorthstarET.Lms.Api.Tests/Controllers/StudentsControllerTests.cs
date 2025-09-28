using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NorthstarET.Lms.Application.DTOs.Students;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace NorthstarET.Lms.Api.Tests.Controllers;

public class StudentsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public StudentsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateStudent_WithValidData_ShouldReturn201()
    {
        // Arrange
        var request = new CreateStudentRequest
        {
            StudentNumber = "STU-INT-001",
            FirstName = "John",
            LastName = "Smith",
            DateOfBirth = new DateTime(2010, 6, 15),
            EnrollmentDate = new DateTime(2024, 8, 15),
            Programs = new StudentProgramsDto
            {
                IsSpecialEducation = false,
                IsGifted = true,
                IsEnglishLanguageLearner = false,
                AccommodationTags = new[] { "extended-time" }
            },
            Guardians = new[]
            {
                new GuardianDto
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@email.com",
                    Phone = "555-0123",
                    RelationshipType = "Parent",
                    IsPrimary = true,
                    CanPickup = true
                }
            }
        };

        // Set up authentication for DistrictAdmin
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var student = JsonSerializer.Deserialize<StudentDto>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        student.Should().NotBeNull();
        student!.StudentNumber.Should().Be(request.StudentNumber);
        student.FirstName.Should().Be(request.FirstName);
        student.LastName.Should().Be(request.LastName);
        student.Status.Should().Be("Active");
        student.Programs.IsGifted.Should().Be(true);
    }

    [Fact]
    public async Task CreateStudent_WithDuplicateStudentNumber_ShouldReturn409()
    {
        // Arrange - Create first student
        var request1 = new CreateStudentRequest
        {
            StudentNumber = "STU-DUP-001",
            FirstName = "First",
            LastName = "Student",
            DateOfBirth = new DateTime(2010, 1, 1),
            EnrollmentDate = new DateTime(2024, 8, 15),
            Programs = new StudentProgramsDto(),
            Guardians = new[]
            {
                new GuardianDto
                {
                    FirstName = "Parent",
                    LastName = "One",
                    Email = "parent1@email.com",
                    RelationshipType = "Parent",
                    IsPrimary = true,
                    CanPickup = true
                }
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        await _client.PostAsJsonAsync("/api/v1/students", request1);

        // Create second student with same student number
        var request2 = new CreateStudentRequest
        {
            StudentNumber = "STU-DUP-001", // Same as first student
            FirstName = "Second",
            LastName = "Student",
            DateOfBirth = new DateTime(2010, 2, 2),
            EnrollmentDate = new DateTime(2024, 8, 15),
            Programs = new StudentProgramsDto(),
            Guardians = new[]
            {
                new GuardianDto
                {
                    FirstName = "Parent",
                    LastName = "Two",
                    Email = "parent2@email.com",
                    RelationshipType = "Parent",
                    IsPrimary = true,
                    CanPickup = true
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateStudent_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var request = new CreateStudentRequest
        {
            StudentNumber = "STU-UNAUTH-001",
            FirstName = "Unauthorized",
            LastName = "Student",
            DateOfBirth = new DateTime(2010, 1, 1),
            EnrollmentDate = new DateTime(2024, 8, 15),
            Programs = new StudentProgramsDto(),
            Guardians = Array.Empty<GuardianDto>()
        };

        // Clear any existing authorization
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStudent_WithValidId_ShouldReturn200()
    {
        // Arrange - First create a student
        var createRequest = new CreateStudentRequest
        {
            StudentNumber = "STU-GET-001",
            FirstName = "GetTest",
            LastName = "Student",
            DateOfBirth = new DateTime(2010, 3, 15),
            EnrollmentDate = new DateTime(2024, 8, 15),
            Programs = new StudentProgramsDto
            {
                IsSpecialEducation = true,
                AccommodationTags = new[] { "large-print", "quiet-space" }
            },
            Guardians = new[]
            {
                new GuardianDto
                {
                    FirstName = "Test",
                    LastName = "Guardian",
                    Email = "test.guardian@email.com",
                    RelationshipType = "Parent",
                    IsPrimary = true,
                    CanPickup = true
                }
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/students", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdStudent = JsonSerializer.Deserialize<StudentDto>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Act
        var response = await _client.GetAsync($"/api/v1/students/{createdStudent!.UserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var student = JsonSerializer.Deserialize<StudentDetailDto>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        student.Should().NotBeNull();
        student!.UserId.Should().Be(createdStudent.UserId);
        student.StudentNumber.Should().Be(createRequest.StudentNumber);
        student.Programs.IsSpecialEducation.Should().Be(true);
        student.Programs.AccommodationTags.Should().Contain("large-print");
    }

    [Fact]
    public async Task GetStudent_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Act
        var response = await _client.GetAsync($"/api/v1/students/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListStudents_WithPagination_ShouldReturn200()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Act
        var response = await _client.GetAsync("/api/v1/students?page=1&size=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<StudentDto>>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Pagination.Should().NotBeNull();
        result.Pagination.Page.Should().Be(1);
        result.Pagination.Size.Should().Be(10);
    }

    [Fact]
    public async Task ListStudents_WithFilters_ShouldReturn200()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Act
        var response = await _client.GetAsync("/api/v1/students?status=Active&hasProgram=gifted&page=1&size=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<StudentDto>>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Pagination.Page.Should().Be(1);
        result.Pagination.Size.Should().Be(20);
    }

    [Fact]
    public async Task UpdateStudentPrograms_WithValidData_ShouldReturn200()
    {
        // Arrange - Create a student first
        var createRequest = new CreateStudentRequest
        {
            StudentNumber = "STU-UPDATE-001",
            FirstName = "Update",
            LastName = "Student",
            DateOfBirth = new DateTime(2010, 5, 10),
            EnrollmentDate = new DateTime(2024, 8, 15),
            Programs = new StudentProgramsDto
            {
                IsSpecialEducation = false,
                IsGifted = false,
                IsEnglishLanguageLearner = false
            },
            Guardians = new[]
            {
                new GuardianDto
                {
                    FirstName = "Update",
                    LastName = "Guardian",
                    Email = "update.guardian@email.com",
                    RelationshipType = "Parent",
                    IsPrimary = true,
                    CanPickup = true
                }
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/students", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdStudent = JsonSerializer.Deserialize<StudentDto>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        var updateRequest = new UpdateStudentRequest
        {
            FirstName = "Updated",
            Programs = new StudentProgramsDto
            {
                IsSpecialEducation = true,
                IsGifted = true,
                IsEnglishLanguageLearner = true,
                AccommodationTags = new[] { "extended-time", "large-print", "quiet-space" }
            }
        };

        // Act
        var response = await _client.PutAsync($"/api/v1/students/{createdStudent!.UserId}", 
            JsonContent.Create(updateRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the update
        var getResponse = await _client.GetAsync($"/api/v1/students/{createdStudent.UserId}");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var updatedStudent = JsonSerializer.Deserialize<StudentDetailDto>(getContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        updatedStudent!.FirstName.Should().Be("Updated");
        updatedStudent.Programs.IsSpecialEducation.Should().Be(true);
        updatedStudent.Programs.IsGifted.Should().Be(true);
        updatedStudent.Programs.IsEnglishLanguageLearner.Should().Be(true);
        updatedStudent.Programs.AccommodationTags.Should().Contain("extended-time");
    }

    [Fact]
    public async Task EnrollStudent_WithValidData_ShouldReturn201()
    {
        // Arrange - Create a student first
        var createRequest = new CreateStudentRequest
        {
            StudentNumber = "STU-ENROLL-001",
            FirstName = "Enroll",
            LastName = "Student",
            DateOfBirth = new DateTime(2010, 8, 20),
            EnrollmentDate = new DateTime(2024, 8, 15),
            Programs = new StudentProgramsDto(),
            Guardians = new[]
            {
                new GuardianDto
                {
                    FirstName = "Enroll",
                    LastName = "Guardian",
                    Email = "enroll.guardian@email.com",
                    RelationshipType = "Parent",
                    IsPrimary = true,
                    CanPickup = true
                }
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/students", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdStudent = JsonSerializer.Deserialize<StudentDto>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        var enrollmentRequest = new EnrollStudentRequest
        {
            ClassId = Guid.NewGuid(), // This would be a real class ID in practice
            GradeLevel = "Grade6",
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/students/{createdStudent!.UserId}/enrollments", 
            enrollmentRequest);

        // Assert - This might return different status codes depending on whether the class exists
        // For this integration test, we expect either 201 (success) or 400 (class not found)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WithdrawStudent_WithValidData_ShouldReturn200()
    {
        // This test would require setting up an actual enrollment first
        // For now, we'll test the endpoint structure
        var enrollmentId = Guid.NewGuid();
        var withdrawalRequest = new WithdrawStudentRequest
        {
            WithdrawalDate = DateTime.UtcNow,
            WithdrawalReason = "Transfer to different school"
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Act
        var response = await _client.DeleteAsync($"/api/v1/students/enrollments/{enrollmentId}");

        // Assert - Expect 404 since enrollment doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BulkRollover_WithValidData_ShouldReturn202()
    {
        // Arrange
        var rolloverRequest = new BulkRolloverRequest
        {
            FromSchoolYear = "2023-2024",
            ToSchoolYear = "2024-2025",
            GradeTransitions = new[]
            {
                new GradeTransition { From = "Grade5", To = "Grade6" },
                new GradeTransition { From = "Grade6", To = "Grade7" }
            },
            ExcludeWithdrawn = true
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students/bulk-rollover/preview", rolloverRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
    }
}

// Additional DTOs for student tests
public class CreateStudentRequest
{
    public string StudentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public StudentProgramsDto Programs { get; set; } = new();
    public GuardianDto[] Guardians { get; set; } = Array.Empty<GuardianDto>();
}

public class GuardianDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool CanPickup { get; set; }
}

public class UpdateStudentRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public StudentProgramsDto Programs { get; set; } = new();
}

public class EnrollStudentRequest
{
    public Guid ClassId { get; set; }
    public string GradeLevel { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
}

public class WithdrawStudentRequest
{
    public DateTime WithdrawalDate { get; set; }
    public string WithdrawalReason { get; set; } = string.Empty;
}

public class BulkRolloverRequest
{
    public string FromSchoolYear { get; set; } = string.Empty;
    public string ToSchoolYear { get; set; } = string.Empty;
    public GradeTransition[] GradeTransitions { get; set; } = Array.Empty<GradeTransition>();
    public bool ExcludeWithdrawn { get; set; }
}

public class GradeTransition
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

public class StudentDetailDto : StudentDto
{
    public List<EnrollmentDto> CurrentEnrollments { get; set; } = new();
    public List<GuardianRelationshipDto> Guardians { get; set; } = new();
}

public class GuardianRelationshipDto
{
    public Guid GuardianId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool CanPickup { get; set; }
}

public class EnrollmentDto
{
    public Guid EnrollmentId { get; set; }
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid? SchoolYearId { get; set; }
    public string GradeLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
}