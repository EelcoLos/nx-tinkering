using A2ADemo.Common;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace A2ADemo.Identity;

public sealed class OidcAuthClient(IHttpClientFactory httpClientFactory, IOptions<AuthSettings> settingsOptions)
{
    private readonly AuthSettings settings = settingsOptions.Value;

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = request.Username ?? string.Empty,
                ["password"] = request.Password ?? string.Empty,
                ["client_id"] = settings.OidcUserClientId,
                ["client_secret"] = settings.OidcUserClientSecret
            },
            ct);

        if (token is null)
        {
            return null;
        }

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.AccessToken);
        var subject = jwt.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;

        return new LoginResponse(token.AccessToken, subject ?? token.Subject ?? token.Username);
    }

    public async Task<string?> GetAgentTokenAsync(string agentId, CancellationToken ct)
    {
        if (!settings.AgentClients.TryGetValue(agentId, out var client))
        {
            return null;
        }

        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = client.ClientId,
                ["client_secret"] = client.ClientSecret
            },
            ct);

        return token?.AccessToken;
    }

    public async Task<ValidatedToken?> ValidateTokenAsync(string token, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, settings.OidcIntrospectionEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = token
            })
        };

        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.OidcIdentityClientId}:{settings.OidcIdentityClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<OidcIntrospectionResponse>(cancellationToken: ct);
        if (payload is null || !payload.Active)
        {
            return null;
        }

        var isServiceAccount = !string.IsNullOrWhiteSpace(payload.ClientId)
            && payload.Username?.StartsWith("service-account-", StringComparison.OrdinalIgnoreCase) == true;
        var isUser = !string.IsNullOrWhiteSpace(payload.Username) && !isServiceAccount;

        return new ValidatedToken(
            payload.Subject ?? string.Empty,
            isUser ? "user" : "agent",
            isUser ? null : payload.ClientId,
            payload.Username);
    }

    private async Task<OidcTokenResponse?> RequestTokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsync(settings.OidcTokenEndpoint, new FormUrlEncodedContent(form), ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<OidcTokenResponse>(cancellationToken: ct);
        return string.IsNullOrWhiteSpace(token?.AccessToken) ? null : token;
    }
}