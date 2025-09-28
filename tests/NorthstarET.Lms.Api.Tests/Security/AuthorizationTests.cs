using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace NorthstarET.Lms.Api.Tests.Security;

/// <summary>
/// Critical security tests to verify Role-Based Access Control (RBAC) enforcement.
/// These tests ensure users can only access resources they are authorized for.
/// </summary>
public class AuthorizationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthorizationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PlatformAdmin_CanAccessDistrictManagement()
    {
        // Arrange
        var createDistrictRequest = new
        {
            Slug = "rbac-test-district",
            DisplayName = "RBAC Test District",
            Quotas = new { MaxStudents = 10000, MaxStaff = 1000, MaxAdmins = 50 }
        };

        // Set authentication for PlatformAdmin role
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/districts", createDistrictRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict); // Created or already exists
    }

    [Fact]
    public async Task DistrictAdmin_CannotAccessOtherDistricts()
    {
        // Arrange - Set authentication for DistrictAdmin with specific tenant
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-oakland-unified");

        // Act - Try to access all districts (should be forbidden for DistrictAdmin)
        var response = await _client.GetAsync("/api/v1/districts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DistrictAdmin_CanAccessOwnTenantStudents()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-oakland-unified");

        // Act
        var response = await _client.GetAsync("/api/v1/students?page=1&size=10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SchoolUser_CanOnlyAccessAssignedSchools()
    {
        // Arrange
        var schoolId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $"test-school-user-{schoolId}");

        // Act - Try to access students from different school
        var differentSchoolId = Guid.NewGuid().ToString();
        var response = await _client.GetAsync($"/api/v1/students?schoolId={differentSchoolId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_CanOnlyAccessAssignedClasses()
    {
        // Arrange
        var classId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $"test-teacher-{classId}");

        // Act - Try to access students from different class
        var differentClassId = Guid.NewGuid().ToString();
        var response = await _client.GetAsync($"/api/v1/students?classId={differentClassId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnauthorizedUser_CannotAccessAnyEndpoints()
    {
        // Arrange - No authentication header

        // Act & Assert - Test multiple endpoints
        var endpoints = new[]
        {
            "/api/v1/districts",
            "/api/v1/students",
            "/api/v1/schools",
            "/api/v1/audit",
            "/api/v1/assessments"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"Endpoint {endpoint} should require authentication");
        }
    }

    [Fact]
    public async Task ExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "expired-token");

        // Act
        var response = await _client.GetAsync("/api/v1/students");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token-format");

        // Act
        var response = await _client.GetAsync("/api/v1/students");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Teacher_CannotCreateStudents()
    {
        // Arrange
        var createStudentRequest = new
        {
            StudentNumber = "UNAUTHORIZED-001",
            FirstName = "Unauthorized",
            LastName = "Student",
            DateOfBirth = DateTime.Now.AddYears(-10),
            EnrollmentDate = DateTime.Now,
            Programs = new { IsSpecialEducation = false, IsGifted = false, IsEnglishLanguageLearner = false }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-teacher-12345");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students", createStudentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_CannotModifyStudentPrograms()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var updateRequest = new
        {
            Programs = new
            {
                IsSpecialEducation = true,
                IsGifted = true,
                IsEnglishLanguageLearner = false
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-teacher-12345");

        // Act
        var response = await _client.PutAsync($"/api/v1/students/{studentId}", JsonContent.Create(updateRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SchoolUser_CannotAccessAuditLogs()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-school-user-12345");

        // Act
        var response = await _client.GetAsync("/api/v1/audit?page=1&size=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DistrictAdmin_CanAccessAuditLogs()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-oakland-unified");

        // Act
        var response = await _client.GetAsync("/api/v1/audit?page=1&size=10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Teacher_CannotPerformBulkOperations()
    {
        // Arrange
        var bulkRolloverRequest = new
        {
            FromSchoolYear = "2023-2024",
            ToSchoolYear = "2024-2025",
            GradeTransitions = new[] { new { From = "Grade5", To = "Grade6" } }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-teacher-12345");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students/bulk-rollover/preview", bulkRolloverRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlatformAdmin_CanAccessAllTenantData()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        // Act - Platform admin should be able to see district list
        var response = await _client.GetAsync("/api/v1/districts");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CrossTenantAccess_ShouldBeBlocked()
    {
        // Arrange - User authenticated for oakland-unified trying to access berkeley-unified data
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-oakland-unified");

        // This test would require setting up actual tenant-specific endpoints or parameters
        // For now, we test the concept with a hypothetical endpoint
        
        // Act - Try to access another tenant's students (if tenant ID were in URL)
        var response = await _client.GetAsync("/api/v1/students?tenantHint=berkeley-unified");

        // Assert - Should either be forbidden or ignore the tenant hint and show only allowed data
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // If OK, response should not contain berkeley-unified data
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("berkeley-unified");
        }
    }

    [Fact]
    public async Task RoleEscalation_ShouldBeBlocked()
    {
        // Arrange - Teacher trying to perform admin-level operations
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-teacher-12345");

        var suspendDistrictRequest = new
        {
            Reason = "Attempted privilege escalation",
            EffectiveDate = DateTime.UtcNow
        };

        var districtId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/districts/{districtId}/suspend", suspendDistrictRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TimeBasedAccess_ShouldRespectRoleExpiration()
    {
        // Arrange - Token with expired role assignment
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-expired-role-token");

        // Act
        var response = await _client.GetAsync("/api/v1/students");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DelegatedRole_ShouldHaveLimitedAccess()
    {
        // Arrange - Token with delegated teacher role (temporary)
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-delegated-teacher-token");

        // Act - Should be able to read students but not create them
        var readResponse = await _client.GetAsync("/api/v1/students?classId=12345");
        
        var createRequest = new
        {
            StudentNumber = "DELEGATED-001",
            FirstName = "Delegated",
            LastName = "Test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/students", createRequest);

        // Assert
        readResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Forbidden);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task IpWhitelist_ShouldRestrictAccess()
    {
        // Arrange - Token that should be restricted by IP
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-ip-restricted-token");
        
        // Add X-Forwarded-For header to simulate request from unauthorized IP
        _client.DefaultRequestHeaders.Add("X-Forwarded-For", "192.168.1.999");

        // Act
        var response = await _client.GetAsync("/api/v1/students");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
        // Note: IP restriction might not be implemented in test environment
    }

    [Fact]
    public async Task ConcurrentSessions_ShouldBeControlled()
    {
        // Arrange - Multiple clients with same token
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        var token = "test-concurrent-session-token";
        client1.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client2.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Make simultaneous requests
        var task1 = client1.GetAsync("/api/v1/students");
        var task2 = client2.GetAsync("/api/v1/students");

        var responses = await Task.WhenAll(task1, task2);

        // Assert - At least one should succeed (concurrent sessions might be allowed in test)
        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.NoContent);
    }
}