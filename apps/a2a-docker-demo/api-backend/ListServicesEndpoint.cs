namespace A2ADemo.ApiBackend;

public sealed class ListServicesEndpoint(DownstreamGateway gateway) : EndpointWithoutRequest<IReadOnlyCollection<ServiceSummary>>
{
    public override void Configure()
    {
        Get("/api/services");
    }

    public override async Task HandleAsync(CancellationToken ct) => await Send.OkAsync(await gateway.GetServicesAsync(ct), ct);
}