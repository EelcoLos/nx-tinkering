using A2ADemo.Common;
using FastEndpoints;
using System.Text.Json.Serialization;

namespace A2ADemo.Discovery;

public sealed record RegisterServiceResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("service_id")] string ServiceId,
    [property: JsonPropertyName("registered_at")] DateTimeOffset RegisteredAt);

public sealed class RegisterServiceEndpoint(ServiceRegistry registry) : Endpoint<ServiceRegistrationRequest, RegisterServiceResponse>
{
    public override void Configure()
    {
        Post("/register");
    }

    public override async Task HandleAsync(ServiceRegistrationRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ServiceId) || string.IsNullOrWhiteSpace(req.BaseUrl))
        {
            AddError("service_id and base_url are required");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        var registered = registry.Upsert(req);
        await Send.OkAsync(new RegisterServiceResponse("registered", registered.ServiceId, registered.RegisteredAt), ct);
    }
}