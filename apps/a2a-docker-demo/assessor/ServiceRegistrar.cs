using A2ADemo.Common;

namespace A2ADemo.Assessor;

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
            "A2A specialist that turns classifications into priorities.",
            ["priority-assessment"],
            new AgentCard(
                settings.ServiceName,
                settings.ServiceName,
                "A2A specialist that turns classifications into priorities.",
                settings.ServiceBaseUrl,
                "v2",
                [new AgentSkill("priority-assessment", "priority-assessment", "Determines priority from a prior classification.")]));

        var client = httpClientFactory.CreateClient();
        await client.TryPostServiceRegistrationAsync(settings.DiscoveryServiceUrl, token, registration);
    }
}