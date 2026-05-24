using A2ADemo.Common;

namespace A2ADemo.Handler;

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
            "A2A specialist that processes routed requests.",
            ["request-handling"],
            new AgentCard(
                settings.ServiceName,
                settings.ServiceName,
                "A2A specialist that processes routed requests.",
                settings.ServiceBaseUrl,
                "v2",
                [new AgentSkill("request-handling", "request-handling", "Processes a routed request and returns the outcome.")]));

        var client = httpClientFactory.CreateClient();
        await client.TryPostServiceRegistrationAsync(settings.DiscoveryServiceUrl, token, registration);
    }
}