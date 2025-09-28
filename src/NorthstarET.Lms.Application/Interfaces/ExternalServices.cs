using NorthstarET.Lms.Domain.Entities;

namespace NorthstarET.Lms.Application.Interfaces;

public interface IAssessmentFileService
{
    Task<string> StoreAssessmentFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> RetrieveAssessmentFileAsync(string fileId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAssessmentFileAsync(string fileId, CancellationToken cancellationToken = default);
    Task<string> GenerateAccessUrlAsync(string fileId, TimeSpan expiration, CancellationToken cancellationToken = default);
}

public interface IIdentityProvider
{
    Task<string?> GetUserIdAsync(string email, CancellationToken cancellationToken = default);
    Task<UserInfo?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DisableUserAsync(string userId, CancellationToken cancellationToken = default);
}

public class UserInfo
{
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
}

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
}