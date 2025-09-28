using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NorthstarET.Lms.Api.Documentation;

/// <summary>
/// OpenAPI/Swagger configuration and documentation attributes for the NorthstarET LMS API.
/// This file demonstrates the comprehensive API documentation standards required for the system.
/// </summary>
public static class OpenApiConfiguration
{
    /// <summary>
    /// Configure OpenAPI documentation for the LMS API
    /// </summary>
    public static OpenApiInfo GetApiInfo()
    {
        return new OpenApiInfo
        {
            Version = "v1",
            Title = "NorthstarET Learning Management System API",
            Description = @"
                ## NorthstarET LMS API

                A comprehensive Learning Management System API designed for K-12 educational institutions with enterprise-grade multi-tenant architecture, RBAC security, and FERPA compliance.

                ### Key Features
                - **Multi-Tenant Architecture**: Complete data isolation per school district
                - **Role-Based Access Control**: Hierarchical permissions (Platform/District/School/Class)
                - **FERPA Compliance**: Comprehensive audit trails and data retention policies
                - **Performance Optimized**: Sub-200ms response times for CRUD operations
                - **Secure by Design**: Defense-in-depth security with tenant isolation

                ### Authentication
                This API uses bearer token authentication. Include your JWT token in the Authorization header:
                ```
                Authorization: Bearer <your-jwt-token>
                ```

                ### Rate Limiting
                API requests are rate-limited per tenant:
                - Standard: 1000 requests/hour
                - Premium: 5000 requests/hour
                - Bulk operations: 50 requests/hour

                ### Error Handling
                The API uses standard HTTP status codes and returns detailed error information:
                - **400**: Bad Request - Invalid input data
                - **401**: Unauthorized - Authentication required
                - **403**: Forbidden - Insufficient permissions
                - **404**: Not Found - Resource does not exist
                - **422**: Unprocessable Entity - Business rule validation failed
                - **500**: Internal Server Error - System error occurred

                ### Tenant Context
                All API calls are automatically scoped to the authenticated user's tenant. 
                Cross-tenant data access is strictly forbidden and monitored.
                ",
            Contact = new OpenApiContact
            {
                Name = "NorthstarET Support",
                Email = "support@northstaret.com",
                Url = new Uri("https://support.northstaret.com")
            },
            License = new OpenApiLicense
            {
                Name = "Proprietary License",
                Url = new Uri("https://northstaret.com/license")
            }
        };
    }

    /// <summary>
    /// Get OpenAPI security schemes
    /// </summary>
    public static Dictionary<string, OpenApiSecurityScheme> GetSecuritySchemes()
    {
        return new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme.
                              Enter 'Bearer' [space] and then your token in the text input below.
                              Example: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            }
        };
    }
}

#region Student API Documentation Models

/// <summary>
/// Student information for educational record management.
/// Represents a K-12 student within a school district tenant.
/// </summary>
/// <example>
/// {
///   "userId": "123e4567-e89b-12d3-a456-426614174000",
///   "studentNumber": "STU-2024-001",
///   "firstName": "John",
///   "lastName": "Doe",
///   "dateOfBirth": "2010-05-15",
///   "gradeLevel": 5,
///   "status": "Active",
///   "enrollmentDate": "2024-08-15T08:00:00Z"
/// }
/// </example>
public class StudentDto
{
    /// <summary>
    /// Unique identifier for the student
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// District-specific student identification number
    /// </summary>
    /// <example>STU-2024-001</example>
    [Required]
    [StringLength(50, MinimumLength = 5)]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Student's first name
    /// </summary>
    /// <example>John</example>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Student's last name
    /// </summary>
    /// <example>Doe</example>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Student's date of birth (FERPA protected)
    /// </summary>
    /// <example>2010-05-15</example>
    [Required]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Current grade level (-1 for Pre-K, 0 for Kindergarten, 1-12 for grades)
    /// </summary>
    /// <example>5</example>
    [Range(-1, 12)]
    public int GradeLevel { get; set; }

    /// <summary>
    /// Student's current enrollment status
    /// </summary>
    /// <example>Active</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StudentStatus Status { get; set; }

    /// <summary>
    /// Date when student was enrolled in the district
    /// </summary>
    /// <example>2024-08-15T08:00:00Z</example>
    [Required]
    public DateTime EnrollmentDate { get; set; }
}

/// <summary>
/// Request model for creating a new student record
/// </summary>
public class CreateStudentRequest
{
    /// <summary>
    /// District-specific student identification number (must be unique within tenant)
    /// </summary>
    /// <example>STU-2024-001</example>
    [Required]
    [StringLength(50, MinimumLength = 5)]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Student's first name
    /// </summary>
    /// <example>John</example>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Student's last name
    /// </summary>
    /// <example>Doe</example>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Student's date of birth (FERPA protected)
    /// </summary>
    /// <example>2010-05-15</example>
    [Required]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Initial grade level for enrollment
    /// </summary>
    /// <example>5</example>
    [Range(-1, 12)]
    public int GradeLevel { get; set; }
}

/// <summary>
/// Student enrollment status enumeration
/// </summary>
public enum StudentStatus
{
    /// <summary>Student is actively enrolled</summary>
    Active = 0,
    /// <summary>Student has withdrawn from the district</summary>
    Withdrawn = 1,
    /// <summary>Student has graduated</summary>
    Graduated = 2,
    /// <summary>Student has transferred to another district</summary>
    Transferred = 3
}

#endregion

#region API Response Models

/// <summary>
/// Standard paged result container for list endpoints
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Items in the current page
    /// </summary>
    [Required]
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    /// <example>20</example>
    [Range(1, 100)]
    public int Size { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    /// <example>150</example>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    /// <example>8</example>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Size);

    /// <summary>
    /// Whether there are more pages available
    /// </summary>
    /// <example>true</example>
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// Standard error response model
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Human-readable error message
    /// </summary>
    /// <example>Student number already exists in this district</example>
    [Required]
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Machine-readable error code
    /// </summary>
    /// <example>DUPLICATE_STUDENT_NUMBER</example>
    public string? Code { get; set; }

    /// <summary>
    /// Correlation ID for support and debugging
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    /// <example>2024-08-15T10:30:00Z</example>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

#endregion