namespace NorthstarET.Lms.Application.DTOs;

/// <summary>
/// Data transfer object for district information
/// </summary>
public record DistrictDto
{
    public required Guid Id { get; init; }
    public required string Slug { get; init; }
    public required string DisplayName { get; init; }
    public required string Status { get; init; }
    public required QuotaDto Quotas { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime? SuspendedAt { get; init; }
    public string? SuspendedReason { get; init; }
}

/// <summary>
/// Data transfer object for quota information
/// </summary>
public record QuotaDto
{
    public required int Students { get; init; }
    public required int Staff { get; init; }
    public required int Admins { get; init; }
}

/// <summary>
/// Data transfer object for school year information
/// </summary>
public record SchoolYearDto
{
    public required Guid Id { get; init; }
    public required string Label { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required string Status { get; init; }
    public DateTime? ArchivedAt { get; init; }
}

/// <summary>
/// Data transfer object for student information
/// </summary>
public record StudentDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string StudentNumber { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required int GradeLevel { get; init; }
    public required string Status { get; init; }
    public required List<string> ProgramFlags { get; init; } = new();
    public required List<string> AccommodationTags { get; init; } = new();
}

/// <summary>
/// Data transfer object for role assignment information
/// </summary>
public record RoleAssignmentDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required Guid RoleDefinitionId { get; init; }
    public required string RoleName { get; init; }
    public required string ScopeType { get; init; }
    public required string Status { get; init; }
    public Guid? DelegatedBy { get; init; }
    public DateTime? DelegationExpiresAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public string? RevocationReason { get; init; }
}

/// <summary>
/// Data transfer object for pagination metadata
/// </summary>
public record PaginationDto
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasNextPage { get; init; }
    public required bool HasPreviousPage { get; init; }
}

/// <summary>
/// Data transfer object for paginated results
/// </summary>
public record PagedResultDto<T>
{
    public required List<T> Items { get; init; } = new();
    public required PaginationDto Pagination { get; init; }
}

/// <summary>
/// Data transfer object for school information
/// </summary>
public record SchoolDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required Guid DistrictId { get; init; }
    public required string Status { get; init; }
    public string? Address { get; init; }
    public string? PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Data transfer object for class information
/// </summary>
public record ClassDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required Guid SchoolId { get; init; }
    public required Guid SchoolYearId { get; init; }
    public required int MaxCapacity { get; init; }
    public required int CurrentEnrollment { get; init; }
    public required string Status { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Data transfer object for enrollment information
/// </summary>
public record EnrollmentDto
{
    public required Guid Id { get; init; }
    public required Guid StudentId { get; init; }
    public required Guid ClassId { get; init; }
    public required string Status { get; init; }
    public required DateTime EnrollmentDate { get; init; }
    public DateTime? WithdrawalDate { get; init; }
    public string? WithdrawalReason { get; init; }
}

/// <summary>
/// Data transfer object for staff information
/// </summary>
public record StaffDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string EmployeeNumber { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required string EmploymentStatus { get; init; }
    public string? ExternalIdentifier { get; init; }
    public DateTime? SuspendedUntil { get; init; }
    public string? SuspensionReason { get; init; }
}