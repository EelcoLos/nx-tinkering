using A2ADemo.Common;

namespace A2ADemo.Router;

public sealed class ServiceRegistrar(
    IHttpClientFactory httpClientFactory,
    IdentityClient identityClient,
    ServiceSettings settings) : IServiceRegistrar
{
    public async Task RegisterAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));

        var token = await identityClient.GetAgentTokenAsync(CancellationToken.None);
        var registration = new ServiceRegistrationRequest(
            settings.ServiceName,
            settings.ServiceName,
            settings.ServiceBaseUrl,
            settings.Port,
            "A2A specialist that routes work based on priority.",
            ["routing"],
            new AgentCard(
                settings.ServiceName,
                settings.ServiceName,
                "A2A specialist that routes work based on priority.",
                settings.ServiceBaseUrl,
                "v2",
                [new AgentSkill("routing", "routing", "Determines the next handler for a given priority.")]));

        var client = httpClientFactory.CreateClient();
        await client.TryPostServiceRegistrationAsync(settings.DiscoveryServiceUrl, token, registration);
    }
}