using A2ADemo.Common;
using FastEndpoints;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace A2ADemo.Router;

public sealed record RouteRequest(
    [property: JsonPropertyName("priority")] string? Priority,
    [property: JsonPropertyName("metadata")] A2AMetadata? Metadata);

public sealed record RouteResponse(
    [property: JsonPropertyName("next_handler")] string NextHandler,
    [property: JsonPropertyName("result")] string Result);

public sealed class SkillEndpoint : Endpoint<RouteRequest, RouteResponse>
{
    public override void Configure()
    {
        Post("/skills/route");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RouteRequest req, CancellationToken ct)
    {
        using var startedActivity = TelemetryExtensions.StartToolActivity(DemoTelemetry.ActivitySource, "router");
        var activity = startedActivity ?? Activity.Current;

        var priority = (req.Priority ?? string.Empty).Trim().ToLowerInvariant();
        var nextHandler = priority switch
        {
            "critical" => "urgent-handler",
            "high" => "priority-handler",
            "medium" => "standard-handler",
            "low" => "self-service-handler",
            _ => "general-handler"
        };

        activity?.SetTag("a2a.result", nextHandler);
        activity?.SetTag("gen_ai.response.finish_reasons", new[] { "stop" });
        activity?.SetStatus(ActivityStatusCode.Ok);

        await Send.OkAsync(new RouteResponse(nextHandler, $"Priority {priority} routed to {nextHandler}."), ct);
    }
}