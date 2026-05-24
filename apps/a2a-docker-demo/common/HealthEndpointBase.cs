namespace A2ADemo.Common;

public sealed class HealthStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";
}

public abstract class HealthEndpointBase : EndpointWithoutRequest<HealthStatusResponse>
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        return Send.OkAsync(new HealthStatusResponse(), ct);
    }
}
