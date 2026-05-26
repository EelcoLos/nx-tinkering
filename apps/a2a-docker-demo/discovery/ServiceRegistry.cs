using System.Collections.Concurrent;

namespace A2ADemo.Discovery;

public sealed class ServiceRegistry(IOptions<ServiceSettings> settingsOptions)
{
  private readonly ServiceSettings settings = settingsOptions.Value;
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
}
