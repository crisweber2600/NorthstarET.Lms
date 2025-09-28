using Microsoft.Extensions.Logging;
using Microsoft.Graph;
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
}