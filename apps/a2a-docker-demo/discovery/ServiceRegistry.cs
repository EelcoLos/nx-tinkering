using A2ADemo.Common;
using System.Collections.Concurrent;

namespace A2ADemo.Discovery;

public sealed class ServiceRegistry(ServiceSettings settings)
{
    private readonly ConcurrentDictionary<string, RegisteredService> services = new(StringComparer.OrdinalIgnoreCase);

    public void SeedSelf()
    {
        services[settings.ServiceName] = new RegisteredService(
            settings.ServiceName,
            "Discovery Service",
            settings.ServiceBaseUrl,
            settings.Port,
            "Service registry for A2A services.",
            ["service-discovery"],
            DateTimeOffset.UtcNow,
            new AgentCard(
                settings.ServiceName,
                "discovery",
                "Registers and returns available A2A services.",
                settings.ServiceBaseUrl,
                "v2",
                [new AgentSkill("service-discovery", "service-discovery", "Lists registered services and their cards.")]));
    }

    public IReadOnlyCollection<RegisteredService> GetAll() => services.Values.OrderBy(service => service.ServiceId).ToArray();

    public AgentCard? GetCard(string serviceId) => services.TryGetValue(serviceId, out var service) ? service.AgentCard : null;

    public RegisteredService Upsert(ServiceRegistrationRequest request)
    {
        var serviceId = request.ServiceId.Trim();
        var registered = new RegisteredService(
            serviceId,
            string.IsNullOrWhiteSpace(request.Name) ? serviceId : request.Name.Trim(),
            request.BaseUrl.Trim(),
            request.Port,
            request.Description.Trim(),
            request.Skills.Where(skill => !string.IsNullOrWhiteSpace(skill)).Select(skill => skill.Trim()).ToArray(),
            DateTimeOffset.UtcNow,
            request.AgentCard);

        services[registered.ServiceId] = registered;
        return registered;
    }
}