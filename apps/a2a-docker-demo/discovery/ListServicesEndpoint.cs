namespace A2ADemo.Discovery;

public sealed class ListServicesEndpoint(ServiceRegistry registry) : EndpointWithoutRequest<IReadOnlyCollection<RegisteredService>>
{
  public override void Configure()
  {
    Get("/services");
  }

  public override Task HandleAsync(CancellationToken ct) => Send.OkAsync(registry.GetAll(), ct);
}
