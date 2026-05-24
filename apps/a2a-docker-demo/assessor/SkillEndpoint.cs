using A2ADemo.Common;
using FastEndpoints;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace A2ADemo.Assessor;

public sealed record AssessRequest(
    [property: JsonPropertyName("classification")] string? Classification,
    [property: JsonPropertyName("metadata")] A2AMetadata? Metadata);

public sealed record AssessResponse(
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("result")] string Result);

public sealed class SkillEndpoint : Endpoint<AssessRequest, AssessResponse>
{
    public override void Configure()
    {
        Post("/skills/assess");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AssessRequest req, CancellationToken ct)
    {
        using var startedActivity = TelemetryExtensions.StartToolActivity(DemoTelemetry.ActivitySource, "assessor");
        var activity = startedActivity ?? Activity.Current;

        var classification = (req.Classification ?? string.Empty).Trim().ToLowerInvariant();
        var priority = classification switch
        {
            "incident" => "critical",
            "defect" => "high",
            "feature_request" => "medium",
            "inquiry" => "low",
            _ => "normal"
        };

        activity?.SetTag("a2a.result", priority);
        activity?.SetTag("gen_ai.response.finish_reasons", new[] { "stop" });
        activity?.SetStatus(ActivityStatusCode.Ok);

        await Send.OkAsync(new AssessResponse(priority, $"Priority assessed as {priority}."), ct);
    }
}