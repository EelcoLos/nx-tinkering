using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;

namespace DotnetFeMsGraph.Services;

public class UserAuthGraphService : IUserAuthGraphService
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ITokenAcquisition _tokenAcquisition;

    public UserAuthGraphService(ITokenAcquisition tokenAcquisition)
    {
        _tokenAcquisition = tokenAcquisition;
        
        // Create a Graph client with user delegated permissions
        _graphServiceClient = GraphServiceClientFactory.Create(async options =>
        {
            var scopes = new[] { "User.Read", "Mail.Read", "Calendars.Read" };
            options.AuthenticationProvider = new BaseBearerTokenAuthenticationProvider(
                new TokenProvider(async (request) => 
                {
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
                    return token;
                }));
        });
    }

    public async Task<User> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var user = await _graphServiceClient.Me
            .GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = new string[] { "displayName", "mail", "userPrincipalName", "id" };
            }, cancellationToken);
            
        return user ?? new User();
    }

    public async Task<List<Message>> GetMyMessagesAsync(int top = 10, CancellationToken cancellationToken = default)
    {
        var messages = await _graphServiceClient.Me.Messages
            .GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Top = top;
                requestConfiguration.QueryParameters.Select = new string[] { "subject", "receivedDateTime", "from" };
                requestConfiguration.Headers.Add("OrderBy", "receivedDateTime DESC");
            }, cancellationToken);

        return messages?.Value?.ToList() ?? new List<Message>();
    }

    public async Task<List<Event>> GetMyCalendarEventsAsync(DateTime? startDateTime = null, DateTime? endDateTime = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var start = startDateTime ?? now;
        var end = endDateTime ?? now.AddDays(7);

        var events = await _graphServiceClient.Me.Calendar.Events
            .GetAsync(requestConfiguration => {
                requestConfiguration.QueryParameters.Select = new string[] { "subject", "start", "end", "location" };
                requestConfiguration.QueryParameters.Filter = $"start/dateTime ge '{start:o}' and end/dateTime le '{end:o}'";
                requestConfiguration.Headers.Add("OrderBy", "start/dateTime");
            }, cancellationToken);

        return events?.Value?.ToList() ?? new List<Event>();
    }

    // Simple token provider for authentication
    private class TokenProvider : IAccessTokenProvider
    {
        private readonly Func<AuthenticationContext, Task<string>> _tokenDelegate;

        public TokenProvider(Func<AuthenticationContext, Task<string>> tokenDelegate)
        {
            _tokenDelegate = tokenDelegate;
        }

        public AllowedHostsValidator AllowedHostsValidator => new AllowedHostsValidator();

        public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            var authContext = new AuthenticationContext(uri);
            return await _tokenDelegate(authContext);
        }

        public class AuthenticationContext
        {
            public Uri ResourceUri { get; }

            public AuthenticationContext(Uri resourceUri)
            {
                ResourceUri = resourceUri;
            }
        }
    }
}