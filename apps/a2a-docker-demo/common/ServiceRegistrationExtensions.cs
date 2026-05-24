using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace A2ADemo.Common;

public static class ServiceRegistrationExtensions
{
    public static async Task TryPostServiceRegistrationAsync(
        this HttpClient client,
        string discoveryServiceUrl,
        string bearerToken,
        object registration,
        CancellationToken ct = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"{discoveryServiceUrl}/register")
            {
                Content = JsonContent.Create(registration)
            };

            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            await client.SendAsync(message, ct);
        }
        catch
        {
            // Best effort registration to avoid startup failure loops in local demos.
        }
    }
}
