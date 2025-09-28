using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using NorthstarET.Lms.Application.Interfaces;

namespace NorthstarET.Lms.Infrastructure.ExternalServices;

/// <summary>
/// Implementation of Entra External ID integration service for identity mapping
/// </summary>
public class EntraIdentityService : IIdentityProvider
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<EntraIdentityService> _logger;

    public EntraIdentityService(
        GraphServiceClient graphServiceClient,
        ILogger<EntraIdentityService> logger)
    {
        _graphServiceClient = graphServiceClient;
        _logger = logger;
    }

    public async Task<string?> CreateExternalUserAsync(string email, string firstName, string lastName, string tenantDisplayName, CancellationToken cancellationToken = default)
    {
        try
        {
            var invitation = new Invitation
            {
                InvitedUserEmailAddress = email,
                InvitedUserDisplayName = $"{firstName} {lastName}",
                InviteRedirectUrl = "https://myapplications.microsoft.com",
                SendInvitationMessage = true,
                InvitedUserMessageInfo = new InvitedUserMessageInfo
                {
                    MessageLanguage = "en-US",
                    CustomizedMessageBody = $"You have been invited to access the {tenantDisplayName} Learning Management System. Please accept this invitation to get started."
                }
            };

            var result = await _graphServiceClient.Invitations
                .PostAsync(invitation, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully created external user invitation for {Email}. External ID: {ExternalId}", 
                email, result?.InvitedUser?.Id);

            return result?.InvitedUser?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create external user for {Email}", email);
            return null;
        }
    }

    public async Task<bool> ValidateExternalUserAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _graphServiceClient.Users[externalId]
                .GetAsync(cancellationToken: cancellationToken);

            return user != null && user.AccountEnabled == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate external user {ExternalId}", externalId);
            return false;
        }
    }

    public async Task<bool> DisableExternalUserAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = new User
            {
                AccountEnabled = false
            };

            await _graphServiceClient.Users[externalId]
                .PatchAsync(user, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully disabled external user {ExternalId}", externalId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable external user {ExternalId}", externalId);
            return false;
        }
    }

    public async Task<bool> DeleteExternalUserAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _graphServiceClient.Users[externalId]
                .DeleteAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully deleted external user {ExternalId}", externalId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete external user {ExternalId}", externalId);
            return false;
        }
    }

    public async Task<(string? Email, string? DisplayName)> GetExternalUserInfoAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _graphServiceClient.Users[externalId]
                .GetAsync(requestConfiguration => 
                {
                    requestConfiguration.QueryParameters.Select = new[] { "mail", "displayName", "userPrincipalName" };
                }, cancellationToken: cancellationToken);

            var email = user?.Mail ?? user?.UserPrincipalName;
            var displayName = user?.DisplayName;

            return (email, displayName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get external user info for {ExternalId}", externalId);
            return (null, null);
        }
    }

    public async Task<bool> SendPasswordResetAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            // For B2B external users, password reset is typically handled through their home tenant
            // This would require custom implementation or directing users to their home tenant's reset process
            
            _logger.LogInformation("Password reset requested for external user {ExternalId}. External users should use their home tenant's password reset process.", externalId);
            
            await Task.CompletedTask; // Fix async warning
            
            // In a real implementation, you might send a custom email or notification
            // directing the user to their home tenant's password reset process
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate password reset for external user {ExternalId}", externalId);
            return false;
        }
    }

    public async Task<IList<string>> GetExternalUserGroupsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var memberOf = await _graphServiceClient.Users[externalId].MemberOf
                .GetAsync(cancellationToken: cancellationToken);

            var groupIds = new List<string>();
            
            if (memberOf?.Value != null)
            {
                foreach (var directoryObject in memberOf.Value)
                {
                    if (directoryObject is Group group && group.Id != null)
                    {
                        groupIds.Add(group.Id);
                    }
                }
            }

            return groupIds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get groups for external user {ExternalId}", externalId);
            return Array.Empty<string>();
        }
    }

    // IIdentityProvider interface implementations
    public async Task<string?> GetUserIdAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _graphServiceClient.Users
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"mail eq '{email}' or userPrincipalName eq '{email}'";
                    requestConfiguration.QueryParameters.Select = new[] { "id" };
                }, cancellationToken: cancellationToken);

            return users?.Value?.FirstOrDefault()?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user ID for {Email}", email);
            return null;
        }
    }

    public async Task<UserInfo?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _graphServiceClient.Users[userId]
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] { "id", "mail", "givenName", "surname", "accountEnabled", "userPrincipalName" };
                }, cancellationToken: cancellationToken);

            if (user == null) return null;

            return new UserInfo
            {
                UserId = user.Id ?? userId,
                Email = user.Mail ?? user.UserPrincipalName ?? "",
                FirstName = user.GivenName ?? "",
                LastName = user.Surname ?? "",
                IsActive = user.AccountEnabled ?? false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user info for {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var invitation = new Invitation
            {
                InvitedUserEmailAddress = request.Email,
                InvitedUserDisplayName = $"{request.FirstName} {request.LastName}",
                InviteRedirectUrl = "https://myapplications.microsoft.com",
                SendInvitationMessage = true
            };

            var result = await _graphServiceClient.Invitations
                .PostAsync(invitation, cancellationToken: cancellationToken);

            return result?.InvitedUser?.Id != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user for {Email}", request.Email);
            return false;
        }
    }

    public async Task<bool> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = new User();
            if (!string.IsNullOrEmpty(request.FirstName))
                user.GivenName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName))
                user.Surname = request.LastName;

            await _graphServiceClient.Users[userId]
                .PatchAsync(user, cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DisableUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await DisableExternalUserAsync(userId, cancellationToken);
    }
}