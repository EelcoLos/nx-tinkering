using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using Azure.Identity;

namespace DotnetFeMsGraph.Services;

public class AppAuthGraphService : IAppAuthGraphService
{
    private readonly GraphServiceClient _graphServiceClient;

    public AppAuthGraphService(IConfiguration configuration)
    {
        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new ArgumentException("Azure AD configuration is missing required values.");
        }

        // Create a client credentials provider for application permissions
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        
        // Create a Graph client with app-only permissions
        _graphServiceClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
    }

    public async Task<List<User>> GetAllUsersAsync(int top = 100, CancellationToken cancellationToken = default)
    {
        var users = await _graphServiceClient.Users
            .GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Top = top;
                requestConfiguration.QueryParameters.Select = new string[] { "displayName", "mail", "userPrincipalName", "id" };
            }, cancellationToken);

        return users?.Value?.ToList() ?? new List<User>();
    }

    public async Task<User> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var user = await _graphServiceClient.Users[userId]
            .GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = new string[] { "displayName", "mail", "userPrincipalName", "id" };
            }, cancellationToken);

        return user ?? new User();
    }

    public async Task<User> GetUserByPrincipalNameAsync(string userPrincipalName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userPrincipalName))
        {
            throw new ArgumentNullException(nameof(userPrincipalName));
        }

        var users = await _graphServiceClient.Users
            .GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Filter = $"userPrincipalName eq '{userPrincipalName}'";
                requestConfiguration.QueryParameters.Select = new string[] { "displayName", "mail", "userPrincipalName", "id" };
            }, cancellationToken);

        return users?.Value?.FirstOrDefault() ?? new User();
    }
}