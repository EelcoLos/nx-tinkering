namespace A2ADemo.Discovery;

public sealed class GetServiceCardEndpoint(ServiceRegistry registry) : EndpointWithoutRequest<AgentCard>
{
    public override void Configure()
    {
        Get("/services/{id}/card");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var serviceId = Route<string>("id") ?? string.Empty;
        var card = registry.GetCard(serviceId);
        if (card is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(card, ct);
    }
}