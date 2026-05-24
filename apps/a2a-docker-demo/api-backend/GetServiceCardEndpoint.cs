using A2ADemo.Common;
using FastEndpoints;

namespace A2ADemo.ApiBackend;

public sealed class GetServiceCardEndpoint(DownstreamGateway gateway) : EndpointWithoutRequest<AgentCard>
{
    public override void Configure()
    {
        Get("/api/services/{id}/card");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var serviceId = Route<string>("id") ?? string.Empty;
        var card = await gateway.GetServiceCardAsync(serviceId, ct);
        if (card is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Service not found" }, cancellationToken: ct);
            return;
        }

        await Send.OkAsync(card, ct);
    }
}