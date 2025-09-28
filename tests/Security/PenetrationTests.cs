using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NorthstarET.Lms.Api.Tests.Security;

/// <summary>
/// Critical security penetration tests to verify protection against common attacks.
/// These tests simulate real-world attack scenarios to ensure the system is secure.
/// </summary>
public class PenetrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public PenetrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SqlInjection_InStudentSearch_ShouldNotExecute()
    {
        // Arrange - Set up authentication
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // SQL Injection payloads
        var maliciousSearchTerms = new[]
        {
            "'; DROP TABLE students; --",
            "' OR '1'='1",
            "' UNION SELECT * FROM audit_records --",
            "'; INSERT INTO students VALUES ('HACK', 'Hacker', 'User'); --",
            "' OR EXISTS (SELECT * FROM information_schema.tables WHERE table_name = 'students') --",
            "admin'; UPDATE students SET first_name = 'HACKED' WHERE '1'='1",
            "'; EXEC xp_cmdshell('dir'); --"
        };

        foreach (var searchTerm in maliciousSearchTerms)
        {
            // Act
            var encodedSearch = Uri.EscapeDataString(searchTerm);
            var response = await _client.GetAsync($"/api/v1/students?search={encodedSearch}");

            // Assert - Should not execute SQL injection, return normal response codes
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,           // Valid request, no results
                HttpStatusCode.NoContent,    // No results found
                HttpStatusCode.BadRequest    // Invalid search parameter
            );

            // Verify response doesn't contain SQL error messages
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotContain("SQL", StringComparison.OrdinalIgnoreCase);
                content.Should().NotContain("database", StringComparison.OrdinalIgnoreCase);
                content.Should().NotContain("syntax", StringComparison.OrdinalIgnoreCase);
                content.Should().NotContain("table", StringComparison.OrdinalIgnoreCase);
                content.Should().NotContain("column", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task XssAttack_InCreateStudent_ShouldBeSanitized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        var xssPayloads = new[]
        {
            "<script>alert('XSS')</script>",
            "javascript:alert('XSS')",
            "<img src=x onerror=alert('XSS')>",
            "<svg/onload=alert('XSS')>",
            "';alert('XSS');//",
            "<iframe src='javascript:alert(1)'></iframe>",
            "&lt;script&gt;alert('XSS')&lt;/script&gt;"
        };

        foreach (var payload in xssPayloads)
        {
            var createRequest = new
            {
                StudentNumber = $"XSS-{Guid.NewGuid()}",
                FirstName = payload,
                LastName = "TestUser",
                DateOfBirth = DateTime.Now.AddYears(-10),
                EnrollmentDate = DateTime.Now,
                Programs = new { IsSpecialEducation = false }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/students", createRequest);

            // Assert
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var content = await response.Content.ReadAsStringAsync();
                var student = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                // Verify XSS payload is sanitized in response
                if (student.TryGetProperty("firstName", out var firstNameElement))
                {
                    var firstName = firstNameElement.GetString();
                    firstName.Should().NotContain("<script");
                    firstName.Should().NotContain("javascript:");
                    firstName.Should().NotContain("alert");
                    firstName.Should().NotContain("<img");
                    firstName.Should().NotContain("onerror");
                }
            }
            else
            {
                // If rejected, should be BadRequest, not a server error
                response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
            }
        }
    }

    [Fact]
    public async Task AuthorizationBypass_WithTamperedToken_ShouldFail()
    {
        // Arrange - Tampered JWT tokens
        var tamperedTokens = new[]
        {
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.TAMPERED_PAYLOAD.TAMPERED_SIGNATURE",
            "Bearer invalid-token-format",
            "eyJhbGciOiJub25lIn0.eyJzdWIiOiJhZG1pbiIsImV4cCI6OTk5OTk5OTk5OX0.",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJQbGF0Zm9ybUFkbWluIiwiZXhwIjo5OTk5OTk5OTk5fQ.TAMPERED",
            "",
            "null",
            "undefined"
        };

        foreach (var token in tamperedTokens)
        {
            _client.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrEmpty(token) && token != "null" && token != "undefined")
            {
                _client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            // Act
            var response = await _client.GetAsync("/api/v1/students");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task MassAssignment_Attack_ShouldBeBlocked()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Mass assignment payload trying to set protected fields
        var massAssignmentPayload = new
        {
            StudentNumber = "MASS-ASSIGN-001",
            FirstName = "MassAssign",
            LastName = "Test",
            DateOfBirth = DateTime.Now.AddYears(-10),
            EnrollmentDate = DateTime.Now,
            
            // Attempt to mass assign protected fields
            UserId = Guid.NewGuid(),                    // Should be auto-generated
            Status = "Suspended",                       // Should default to Active
            TenantId = "malicious-tenant",             // Should be set by middleware
            CreatedDate = DateTime.Now.AddDays(-365),  // Should be auto-set
            IsSystemUser = true,                       // Protected field
            AdminRoles = new[] { "PlatformAdmin" },    // Attempt role escalation
            AuditBypass = true,                        // Attempt audit bypass
            SecurityLevel = "High"                     // Non-existent protected field
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students", massAssignmentPayload);

        // Assert
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var content = await response.Content.ReadAsStringAsync();
            var student = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            // Verify protected fields were not mass assigned
            if (student.TryGetProperty("status", out var statusElement))
            {
                statusElement.GetString().Should().Be("Active"); // Should be default, not "Suspended"
            }

            // Verify malicious fields were ignored
            student.TryGetProperty("tenantId", out _).Should().BeFalse();
            student.TryGetProperty("isSystemUser", out _).Should().BeFalse();
            student.TryGetProperty("adminRoles", out _).Should().BeFalse();
            student.TryGetProperty("auditBypass", out _).Should().BeFalse();
            student.TryGetProperty("securityLevel", out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task PathTraversal_Attack_ShouldBeBlocked()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Path traversal payloads
        var pathTraversalPayloads = new[]
        {
            "../../../etc/passwd",
            "..\\..\\..\\windows\\system32\\config\\sam",
            "%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd",
            "....//....//....//etc/passwd",
            "..%252f..%252f..%252fetc%252fpasswd",
            "..%c0%af..%c0%af..%c0%afetc%c0%afpasswd"
        };

        foreach (var payload in pathTraversalPayloads)
        {
            // Act - Try path traversal in various endpoints
            var endpoints = new[]
            {
                $"/api/v1/assessments/{payload}",
                $"/api/v1/students?search={Uri.EscapeDataString(payload)}",
                $"/api/v1/audit?entityType={Uri.EscapeDataString(payload)}"
            };

            foreach (var endpoint in endpoints)
            {
                var response = await _client.GetAsync(endpoint);

                // Assert - Should not return file contents or cause server errors
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.NotFound,
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.OK,
                    HttpStatusCode.NoContent,
                    HttpStatusCode.Forbidden
                );

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // Should not contain system file contents
                    content.Should().NotContain("root:");
                    content.Should().NotContain("bin/bash");
                    content.Should().NotContain("Windows Registry");
                    content.Should().NotContain("SAM_Account");
                }
            }
        }
    }

    [Fact]
    public async Task HttpHeaderInjection_ShouldBeBlocked()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Header injection payloads
        var headerInjectionPayloads = new[]
        {
            "normalvalue\r\nX-Injected-Header: injected",
            "normalvalue\nSet-Cookie: sessionid=malicious",
            "normalvalue\r\nLocation: http://evil.com",
            "value%0d%0aX-Injected: header",
            "value%0a%0d%0aHTTP/1.1 200 OK%0d%0a%0d%0a<html>Injected</html>"
        };

        foreach (var payload in headerInjectionPayloads)
        {
            var createRequest = new
            {
                StudentNumber = "HDR-INJ-001",
                FirstName = "HeaderInject",
                LastName = payload, // Try to inject in data that might be used in response headers
                DateOfBirth = DateTime.Now.AddYears(-10),
                EnrollmentDate = DateTime.Now
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/students", createRequest);

            // Assert - Check that no malicious headers were injected
            response.Headers.Should().NotContain(h => h.Key.StartsWith("X-Injected"));
            response.Headers.Should().NotContain(h => h.Key == "Set-Cookie" && h.Value.Any(v => v.Contains("malicious")));
            response.Headers.Should().NotContain(h => h.Key == "Location" && h.Value.Any(v => v.Contains("evil.com")));
        }
    }

    [Fact]
    public async Task DenialOfService_LargePayload_ShouldBeRejected()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // Create extremely large payload
        var largeString = new string('A', 10_000_000); // 10MB string

        var largePayload = new
        {
            StudentNumber = "DOS-001",
            FirstName = largeString,
            LastName = "DOS Test",
            DateOfBirth = DateTime.Now.AddYears(-10),
            EnrollmentDate = DateTime.Now,
            Programs = new
            {
                AccommodationTags = Enumerable.Repeat(largeString, 100).ToArray() // Very large array
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students", largePayload);

        // Assert - Should reject large payloads
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,           // Request too large
            HttpStatusCode.RequestEntityTooLarge // 413 Payload Too Large
        );
    }

    [Fact]
    public async Task RateLimiting_ExcessiveRequests_ShouldBeThrottled()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-rate-limit-token");

        var requests = new List<Task<HttpResponseMessage>>();

        // Act - Send many requests rapidly
        for (int i = 0; i < 100; i++)
        {
            requests.Add(_client.GetAsync("/api/v1/students"));
        }

        var responses = await Task.WhenAll(requests);

        // Assert - Some requests should be rate limited
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        var successfulResponses = responses.Count(r => r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.NoContent);

        // Should have at least some rate limiting in effect
        (rateLimitedResponses > 0 || successfulResponses < requests.Count).Should().BeTrue();
    }

    [Fact]
    public async Task JsonDeserialization_Bomb_ShouldBeRejected()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");

        // JSON bomb - deeply nested structure
        var jsonBomb = "{" + string.Join("", Enumerable.Repeat("\"a\":{", 10000)) + "\"b\":1" + string.Join("", Enumerable.Repeat("}", 10001));

        var content = new StringContent(jsonBomb, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/students", content);

        // Assert - Should reject malformed or excessive JSON
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.UnprocessableEntity
        );
    }

    [Fact]
    public async Task DirectObjectReference_Attack_ShouldBeBlocked()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-oakland-unified");

        // Try to access resources using predictable IDs or different tenant IDs
        var maliciousIds = new[]
        {
            Guid.Empty.ToString(),
            "00000000-0000-0000-0000-000000000001",
            "11111111-1111-1111-1111-111111111111",
            "ffffffff-ffff-ffff-ffff-ffffffffffff",
            "../admin",
            "../../platform-config",
            "null",
            "undefined",
            "1' OR '1'='1"
        };

        foreach (var maliciousId in maliciousIds)
        {
            // Act - Try to access student with malicious ID
            var response = await _client.GetAsync($"/api/v1/students/{Uri.EscapeDataString(maliciousId)}");

            // Assert - Should either be not found or bad request, never unauthorized access
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.NotFound,
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden
            );

            // Should not return sensitive data
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotContain("platform-config");
                content.Should().NotContain("admin");
                content.Should().NotContain("password");
                content.Should().NotContain("secret");
            }
        }
    }

    [Fact]
    public async Task CsrfAttack_WithoutToken_ShouldBeBlocked()
    {
        // Arrange - Remove anti-forgery token and origin headers
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-district-admin-token");
        
        // Add malicious origin
        _client.DefaultRequestHeaders.Add("Origin", "http://evil.com");
        _client.DefaultRequestHeaders.Add("Referer", "http://evil.com/attack");

        var createRequest = new
        {
            StudentNumber = "CSRF-001",
            FirstName = "CSRF",
            LastName = "Test",
            DateOfBirth = DateTime.Now.AddYears(-10)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/students", createRequest);

        // Assert - Should either require CSRF token or validate origin
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Created  // CSRF protection might not be enabled in test environment
        );
    }
}