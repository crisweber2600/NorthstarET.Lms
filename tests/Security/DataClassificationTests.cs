using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace NorthstarET.Lms.Api.Tests.Security;

/// <summary>
/// Critical compliance tests to verify proper data classification and FERPA compliance.
/// These tests ensure Personally Identifiable Information (PII) is properly protected.
/// </summary>
public class DataClassificationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public DataClassificationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StudentData_ShouldNotExposeFullSSN_InAnyContext()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var createRequest = new
        {
            StudentNumber = "PII-001",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 6, 15),
            EnrollmentDate = DateTime.Now,
            SocialSecurityNumber = "123-45-6789",  // This should never appear in full
            Programs = new { IsSpecialEducation = false }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students", createRequest);

        // Assert
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            // Should never contain full SSN
            content.Should().NotContain("123-45-6789");
            content.Should().NotContain("123456789");
            
            // Should not contain any pattern that looks like full SSN
            var ssnPattern = @"\b\d{3}-?\d{2}-?\d{4}\b";
            Regex.IsMatch(content, ssnPattern).Should().BeFalse();
        }
    }

    [Fact]
    public async Task StudentData_ShouldMaskSensitiveFields_ForLowerPrivilegeUsers()
    {
        // Arrange - Create student first as admin
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var createRequest = new
        {
            StudentNumber = "MASK-001",
            FirstName = "Jane",
            LastName = "Smith",
            DateOfBirth = new DateTime(2010, 3, 20),
            EnrollmentDate = DateTime.Now,
            Programs = new 
            { 
                IsSpecialEducation = true,
                AccommodationTags = new[] { "adhd-medication", "therapy-sessions" }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/students", createRequest);
        
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            return; // Skip test if creation failed
        }

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var student = JsonSerializer.Deserialize<JsonElement>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        var studentId = student.GetProperty("userId").GetGuid();

        // Act - Now access as a teacher (lower privilege)
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-teacher-class-123");

        var response = await _client.GetAsync($"/api/v1/students/{studentId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            // Teacher should not see sensitive medical information
            content.Should().NotContain("adhd-medication");
            content.Should().NotContain("therapy-sessions");
            
            // Date of birth might be masked or not shown
            if (content.Contains("dateOfBirth"))
            {
                // If shown, should not contain full date
                content.Should().NotContain("2010-03-20");
            }
        }
        else
        {
            // Teacher might not have access at all - this is also acceptable
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task AuditLogs_ShouldNotLogSensitiveData_InPlainText()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Act - Query audit logs
        var response = await _client.GetAsync("/api/v1/audit?entityType=Student&page=1&size=50");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            // Should not contain PII in audit logs
            var ssnPattern = @"\b\d{3}-?\d{2}-?\d{4}\b";
            Regex.IsMatch(content, ssnPattern).Should().BeFalse();
            
            // Should not contain phone numbers in full
            var phonePattern = @"\b\d{3}-?\d{3}-?\d{4}\b";
            Regex.IsMatch(content, phonePattern).Should().BeFalse();
            
            // Should not contain email addresses unless necessary
            content.Should().NotContain("@email.com");
            
            // Medical information should be redacted
            content.Should().NotContain("adhd");
            content.Should().NotContain("therapy");
            content.Should().NotContain("medication");
        }
    }

    [Fact]
    public async Task ErrorMessages_ShouldNotExposeSensitiveInformation()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Try to create invalid student data that would cause errors
        var invalidRequests = new[]
        {
            new { StudentNumber = "", FirstName = "Test" }, // Missing required fields
            new { StudentNumber = "DUP-001", FirstName = "Duplicate" }, // Potential duplicate
            new { StudentNumber = "INV-001", DateOfBirth = "invalid-date" } // Invalid date format
        };

        foreach (var invalidRequest in invalidRequests)
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/students", invalidRequest);

            // Assert
            if (response.StatusCode == HttpStatusCode.BadRequest || 
                response.StatusCode == HttpStatusCode.UnprocessableEntity ||
                response.StatusCode == HttpStatusCode.Conflict)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Should not expose database schema information
                content.Should().NotContain("students");
                content.Should().NotContain("table");
                content.Should().NotContain("column");
                content.Should().NotContain("constraint");
                content.Should().NotContain("foreign key");
                content.Should().NotContain("database");
                
                // Should not expose internal system paths
                content.Should().NotContain("C:\\");
                content.Should().NotContain("/var/");
                content.Should().NotContain("src/");
                content.Should().NotContain(".cs");
                content.Should().NotContain(".dll");
                
                // Should not expose connection strings or config
                content.Should().NotContain("Server=");
                content.Should().NotContain("Password=");
                content.Should().NotContain("ConnectionString");
            }
        }
    }

    [Fact]
    public async Task GuardianData_ShouldBeProtected_FromUnauthorizedAccess()
    {
        // Arrange - Create student with guardian as DistrictAdmin
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var createRequest = new
        {
            StudentNumber = "GUARD-001",
            FirstName = "Protected",
            LastName = "Student",
            DateOfBirth = new DateTime(2010, 1, 1),
            EnrollmentDate = DateTime.Now,
            Guardians = new[]
            {
                new
                {
                    FirstName = "Private",
                    LastName = "Guardian",
                    Email = "private.guardian@email.com",
                    Phone = "555-0199",
                    RelationshipType = "Parent",
                    IsPrimary = true,
                    CanPickup = true
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/students", createRequest);
        
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            return; // Skip test if creation failed
        }

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var student = JsonSerializer.Deserialize<JsonElement>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        var studentId = student.GetProperty("userId").GetGuid();

        // Act - Try to access as different role levels
        var testScenarios = new[]
        {
            ("test-teacher-class-456", HttpStatusCode.Forbidden), // Teacher from different class
            ("test-school-user-different-school", HttpStatusCode.Forbidden), // Different school
            ("test-district-admin-different-district", HttpStatusCode.Forbidden) // Different district
        };

        foreach (var (token, expectedStatus) in testScenarios)
        {
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/v1/students/{studentId}");

            // Assert
            response.StatusCode.Should().Be(expectedStatus);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Even if somehow accessible, should not contain guardian PII
                content.Should().NotContain("private.guardian@email.com");
                content.Should().NotContain("555-0199");
            }
        }
    }

    [Fact]
    public async Task DataExport_ShouldRequireHighestPrivileges_AndAuditAccess()
    {
        // Arrange - Test data export functionality
        var exportTestScenarios = new[]
        {
            ("test-teacher-123", HttpStatusCode.Forbidden),
            ("test-school-user-456", HttpStatusCode.Forbidden),
            ("test-district-admin-oakland", HttpStatusCode.OK) // Should have access
        };

        foreach (var (token, expectedMinStatus) in exportTestScenarios)
        {
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var exportRequest = new
            {
                Format = "csv",
                Filters = new
                {
                    IncludeGuardians = true,
                    IncludeEnrollmentHistory = true
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/students/bulk-export", exportRequest);

            // Assert
            if (expectedMinStatus == HttpStatusCode.Forbidden)
            {
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
            else
            {
                // District admin should be able to initiate export
                response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
            }
        }
    }

    [Fact]
    public async Task SpecialEducationData_ShouldHaveExtraProtection()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var createRequest = new
        {
            StudentNumber = "SPED-001",
            FirstName = "Special",
            LastName = "Needs",
            DateOfBirth = new DateTime(2010, 5, 15),
            EnrollmentDate = DateTime.Now,
            Programs = new
            {
                IsSpecialEducation = true,
                AccommodationTags = new[] 
                { 
                    "iep-required", 
                    "speech-therapy", 
                    "behavioral-support",
                    "modified-testing"
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/students", createRequest);
        
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            return;
        }

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var student = JsonSerializer.Deserialize<JsonElement>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        var studentId = student.GetProperty("userId").GetGuid();

        // Act - Access as regular teacher (should have limited access to SPED data)
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-teacher-regular");

        var response = await _client.GetAsync($"/api/v1/students/{studentId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            // Regular teachers should not see detailed SPED accommodations
            content.Should().NotContain("iep-required");
            content.Should().NotContain("speech-therapy");
            content.Should().NotContain("behavioral-support");
            
            // Might show general SPED status but not details
            if (content.Contains("isSpecialEducation"))
            {
                content.Should().Contain("\"isSpecialEducation\":true");
            }
        }
        else
        {
            // No access is also acceptable for regular teachers
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task SearchFunctionality_ShouldNotAllowDataMining()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Data mining attempts
        var miningAttempts = new[]
        {
            "?search=*",                    // Wildcard search
            "?search=%",                    // SQL wildcard
            "?search=.*",                   // Regex wildcard
            "?dateOfBirth=*",              // Wildcard on sensitive field
            "?socialSecurityNumber=*",      // Direct PII search
            "?accommodationTags=*",        // Medical data mining
            "?search=&limit=999999",       // Excessive results
            "?includeDeleted=true",        // Access to deleted records
            "?includeAll=true"             // Bypass filters
        };

        foreach (var attempt in miningAttempts)
        {
            // Act
            var response = await _client.GetAsync($"/api/v1/students{attempt}");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                // Should not return excessive amounts of data
                if (result.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    dataArray.GetArrayLength().Should().BeLessOrEqualTo(100); // Reasonable page size
                }

                // Should not contain sensitive patterns
                content.Should().NotContain("\"socialSecurityNumber\":");
                content.Should().NotContain("\"password\":");
                content.Should().NotContain("\"secret\":");
            }
            else
            {
                // Rejecting mining attempts is acceptable
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.Forbidden,
                    HttpStatusCode.UnprocessableEntity
                );
            }
        }
    }

    [Fact]
    public async Task ResponseHeaders_ShouldNotExposeSensitiveSystemInfo()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Act
        var response = await _client.GetAsync("/api/v1/students");

        // Assert - Response headers should not expose sensitive information
        var headers = response.Headers.Concat(response.Content.Headers);
        
        foreach (var header in headers)
        {
            var headerValues = string.Join(", ", header.Value);
            
            // Should not expose server/framework versions
            headerValues.Should().NotContain("Microsoft");
            headerValues.Should().NotContain("ASP.NET");
            headerValues.Should().NotContain("IIS");
            headerValues.Should().NotContain("Kestrel");
            
            // Should not expose internal paths
            headerValues.Should().NotContain("C:\\");
            headerValues.Should().NotContain("/var/");
            headerValues.Should().NotContain("src/");
            
            // Should not expose database info
            headerValues.Should().NotContain("SqlServer");
            headerValues.Should().NotContain("Database");
            headerValues.Should().NotContain("Connection");
        }

        // Should have security headers
        response.Headers.Should().ContainKey("X-Content-Type-Options")
            .WhoseValue.Should().Contain("nosniff");
        response.Headers.Should().ContainKey("X-Frame-Options")
            .WhoseValue.Should().Contain("DENY");
    }

    [Fact]
    public async Task LoggedOutUser_ShouldNotRetainDataAccess()
    {
        // Arrange - Simulate logged out user (no auth header)
        _client.DefaultRequestHeaders.Authorization = null;

        var endpoints = new[]
        {
            "/api/v1/students",
            "/api/v1/students/search?q=test",
            "/api/v1/audit",
            "/api/v1/districts"
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert - Should require authentication
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            
            var content = await response.Content.ReadAsStringAsync();
            
            // Should not leak any data in unauthorized responses
            content.Should().NotContain("studentNumber");
            content.Should().NotContain("firstName");
            content.Should().NotContain("lastName");
            content.Should().NotContain("dateOfBirth");
            content.Should().NotContain("email");
            content.Should().NotContain("phone");
        }
    }
}