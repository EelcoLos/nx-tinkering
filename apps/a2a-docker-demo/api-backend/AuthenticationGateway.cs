using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace A2ADemo.ApiBackend;

public sealed class AuthenticationGateway(IHttpClientFactory httpClientFactory, IOptions<ServiceSettings> settingsOptions)
{
    private readonly ServiceSettings settings = settingsOptions.Value;

    public async Task<(int StatusCode, string Body)> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsJsonAsync($"{settings.IdentityServiceUrl}/auth/login", request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return ((int)response.StatusCode, body);
    }
}