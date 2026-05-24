using A2ADemo.Common;
using FastEndpoints;
using FastEndpoints.A2A;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace A2ADemo.Router;

public sealed record RouteRequest(
    string? Priority,
    A2AMetadata? Metadata);

public sealed record RouteResponse(
    [property: JsonPropertyName("next_handler")] string NextHandler,
    string Result);

public sealed class SkillEndpoint : Endpoint<RouteRequest, RouteResponse>
{
    public override void Configure()
    {
        Post("/skills/route");

        this.A2ASkill(
            id: "router",
            tags: ["triage", "routing"],
            configure: skill =>
            {
                skill.Name = "Router";
                skill.Description = "Determines the next handler for a given priority.";
                skill.Examples = ["Route this high priority request."];
                skill.InputModes = ["application/json"];
                skill.OutputModes = ["application/json"];
            });
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