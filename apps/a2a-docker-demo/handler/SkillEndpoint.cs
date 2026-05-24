using A2ADemo.Common;
using FastEndpoints;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace A2ADemo.Handler;

public sealed record HandleRequest(
    [property: JsonPropertyName("input")] string? Input,
    [property: JsonPropertyName("classification")] string? Classification,
    [property: JsonPropertyName("priority")] string? Priority,
    [property: JsonPropertyName("metadata")] A2AMetadata? Metadata);

public sealed record HandleResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("ticket_id")] string TicketId,
    [property: JsonPropertyName("summary")] string Summary);

public sealed class SkillEndpoint : Endpoint<HandleRequest, HandleResponse>
{
    public override void Configure()
    {
        Post("/skills/handle");
        AllowAnonymous();
    }

    public override async Task HandleAsync(HandleRequest req, CancellationToken ct)
    {
        using var startedActivity = TelemetryExtensions.StartToolActivity(DemoTelemetry.ActivitySource, "handler");
        var activity = startedActivity ?? Activity.Current;

        var ticketId = $"TKT-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var priority = string.IsNullOrWhiteSpace(req.Priority) ? "normal" : req.Priority.Trim().ToLowerInvariant();
        var classification = string.IsNullOrWhiteSpace(req.Classification) ? "general" : req.Classification.Trim().ToLowerInvariant();

        activity?.SetTag("a2a.result", ticketId);
        activity?.SetTag("gen_ai.response.finish_reasons", new[] { "stop" });
        activity?.SetStatus(ActivityStatusCode.Ok);

        await Send.OkAsync(new HandleResponse("processed", ticketId, $"Handled {classification} request with {priority} priority."), ct);
    }
}