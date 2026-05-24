using A2ADemo.Common;

namespace A2ADemo.Classifier;

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
            "A2A specialist that classifies incoming text.",
            ["classification"],
            new AgentCard(
                settings.ServiceName,
                settings.ServiceName,
                "A2A specialist that classifies incoming text.",
                settings.ServiceBaseUrl,
                "v2",
                [new AgentSkill("classification", "classification", "Classifies incoming text into a triage category.")]));

        var client = httpClientFactory.CreateClient();
        await client.TryPostServiceRegistrationAsync(settings.DiscoveryServiceUrl, token, registration);
    }
}