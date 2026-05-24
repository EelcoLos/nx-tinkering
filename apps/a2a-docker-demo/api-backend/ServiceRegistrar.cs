using A2ADemo.Common;

namespace A2ADemo.ApiBackend;

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
            "Website-facing FastEndpoints API gateway.",
            ["triage-orchestration"],
            new AgentCard(
                settings.ServiceName,
                settings.ServiceName,
                "Website-facing FastEndpoints API gateway.",
                settings.ServiceBaseUrl,
                "v2",
                [new AgentSkill("triage-orchestration", "triage-orchestration", "Coordinates the multi-step triage workflow.")]));

        var client = httpClientFactory.CreateClient();
        await client.TryPostServiceRegistrationAsync(settings.DiscoveryServiceUrl, token, registration);
    }
}