namespace A2ADemo.Handler;

public sealed record HandleRequest(
    string? Input,
    string? Classification,
    string? Priority,
    A2AMetadata? Metadata);

public sealed record HandleResponse(
    string Status,
    [property: JsonPropertyName("ticket_id")] string TicketId,
    string Summary);

public sealed class SkillEndpoint : Endpoint<HandleRequest, HandleResponse>
{
  public override void Configure()
  {
    Post("/skills/handle");

    this.A2ASkill(
        id: "handler",
        tags: ["triage", "handling"],
        configure: skill =>
        {
          skill.Name = "Handler";
          skill.Description = "Processes a routed request and returns the outcome.";
          skill.Examples = ["Handle this critical defect request."];
          skill.InputModes = ["application/json"];
          skill.OutputModes = ["application/json"];
        });
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
