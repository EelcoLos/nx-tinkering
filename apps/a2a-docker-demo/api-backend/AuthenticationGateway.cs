using System.Net.Http.Json;

namespace A2ADemo.ApiBackend;

public sealed class AuthenticationGateway(IHttpClientFactory httpClientFactory, ServiceSettings settings)
{
    public async Task<(int StatusCode, string Body)> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsJsonAsync($"{settings.IdentityServiceUrl}/auth/login", request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return ((int)response.StatusCode, body);
    }
}