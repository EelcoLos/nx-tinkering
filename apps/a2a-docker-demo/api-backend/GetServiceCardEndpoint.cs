using AgentCardModel = A2A.AgentCard;

namespace A2ADemo.ApiBackend;

public sealed class GetServiceCardEndpoint(DownstreamGateway gateway) : EndpointWithoutRequest<AgentCardModel>
{
  public override void Configure()
  {
    Get("/api/services/{id}/card");
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
