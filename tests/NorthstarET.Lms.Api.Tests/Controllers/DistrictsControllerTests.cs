using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NorthstarET.Lms.Application.DTOs.Districts;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace NorthstarET.Lms.Api.Tests.Controllers;

public class DistrictsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public DistrictsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateDistrict_WithValidData_ShouldReturn201()
    {
        // Arrange
        var request = new CreateDistrictRequest
        {
            Slug = "integration-test-district",
            DisplayName = "Integration Test District",
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = 10000,
                MaxStaff = 1000,
                MaxAdmins = 50
            }
        };

        // Set up authentication for PlatformAdmin
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/districts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var district = JsonSerializer.Deserialize<DistrictDto>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        district.Should().NotBeNull();
        district!.Slug.Should().Be(request.Slug);
        district.DisplayName.Should().Be(request.DisplayName);
        district.Status.Should().Be("Active");
        district.Quotas.MaxStudents.Should().Be(request.Quotas.MaxStudents);
    }

    [Fact]
    public async Task CreateDistrict_WithDuplicateSlug_ShouldReturn409()
    {
        // Arrange - Create first district
        var request1 = new CreateDistrictRequest
        {
            Slug = "duplicate-test-district",
            DisplayName = "First District",
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = 5000,
                MaxStaff = 500,
                MaxAdmins = 25
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        await _client.PostAsJsonAsync("/api/v1/districts", request1);

        // Create second district with same slug
        var request2 = new CreateDistrictRequest
        {
            Slug = "duplicate-test-district",
            DisplayName = "Second District",
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = 3000,
                MaxStaff = 300,
                MaxAdmins = 15
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/districts", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateDistrict_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var request = new CreateDistrictRequest
        {
            Slug = "unauthorized-district",
            DisplayName = "Unauthorized District",
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = 1000,
                MaxStaff = 100,
                MaxAdmins = 10
            }
        };

        // Clear any existing authorization
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/districts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDistrict_WithInvalidQuotas_ShouldReturn400()
    {
        // Arrange
        var request = new CreateDistrictRequest
        {
            Slug = "invalid-quotas-district",
            DisplayName = "Invalid Quotas District",
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = 150000, // Exceeds maximum allowed
                MaxStaff = 1000,
                MaxAdmins = 50
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/districts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDistrict_WithValidId_ShouldReturn200()
    {
        // Arrange - First create a district
        var createRequest = new CreateDistrictRequest
        {
            Slug = "get-test-district",
            DisplayName = "Get Test District",
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = 8000,
                MaxStaff = 800,
                MaxAdmins = 40
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/districts", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdDistrict = JsonSerializer.Deserialize<DistrictDto>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Act
        var response = await _client.GetAsync($"/api/v1/districts/{createdDistrict!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var district = JsonSerializer.Deserialize<DistrictDto>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        district.Should().NotBeNull();
        district!.Id.Should().Be(createdDistrict.Id);
        district.Slug.Should().Be(createRequest.Slug);
    }

    [Fact]
    public async Task GetDistrict_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        // Act
        var response = await _client.GetAsync($"/api/v1/districts/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListDistricts_WithPagination_ShouldReturn200()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        // Act
        var response = await _client.GetAsync("/api/v1/districts?page=1&size=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<DistrictDto>>(content, new JsonSerializerOptions 
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
    public async Task SuspendDistrict_WithValidData_ShouldReturn200()
    {
        // Arrange - Create a district first
        var createRequest = new CreateDistrictRequest
        {
            Slug = "suspend-test-district",
            DisplayName = "Suspend Test District",
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = 5000,
                MaxStaff = 500,
                MaxAdmins = 25
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/districts", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdDistrict = JsonSerializer.Deserialize<DistrictDto>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        var suspendRequest = new SuspendDistrictRequest
        {
            Reason = "Policy violation test",
            EffectiveDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/districts/{createdDistrict!.Id}/suspend", suspendRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateDistrictQuotas_WithValidData_ShouldReturn200()
    {
        // Arrange - Create a district first
        var createRequest = new CreateDistrictRequest
        {
            Slug = "quota-update-test-district",
            DisplayName = "Quota Update Test District",
            Quotas = new DistrictQuotasDto
            {
                MaxStudents = 5000,
                MaxStaff = 500,
                MaxAdmins = 25
            }
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-platform-admin-token");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/districts", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdDistrict = JsonSerializer.Deserialize<DistrictDto>(createContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        var updateRequest = new UpdateDistrictQuotasRequest
        {
            MaxStudents = 10000,
            MaxStaff = 1000,
            MaxAdmins = 50
        };

        // Act
        var response = await _client.PatchAsync($"/api/v1/districts/{createdDistrict!.Id}/quotas", 
            JsonContent.Create(updateRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the update
        var getResponse = await _client.GetAsync($"/api/v1/districts/{createdDistrict.Id}");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var updatedDistrict = JsonSerializer.Deserialize<DistrictDto>(getContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        updatedDistrict!.Quotas.MaxStudents.Should().Be(updateRequest.MaxStudents);
        updatedDistrict.Quotas.MaxStaff.Should().Be(updateRequest.MaxStaff);
        updatedDistrict.Quotas.MaxAdmins.Should().Be(updateRequest.MaxAdmins);
    }
}

// DTOs for the test requests/responses
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

public class SuspendDistrictRequest
{
    public string Reason { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
}

public class UpdateDistrictQuotasRequest
{
    public int MaxStudents { get; set; }
    public int MaxStaff { get; set; }
    public int MaxAdmins { get; set; }
}