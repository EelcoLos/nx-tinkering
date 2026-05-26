namespace A2ADemo.Assessor;

public sealed record AssessRequest(
    string? Classification,
    A2AMetadata? Metadata);

public sealed record AssessResponse(
    string Priority,
    string Result);

public sealed class SkillEndpoint : Endpoint<AssessRequest, AssessResponse>
{
  public override void Configure()
  {
    Post("/skills/assess");

    this.A2ASkill(
        id: "assessor",
        tags: ["triage", "priority"],
        configure: skill =>
        {
          skill.Name = "Assessor";
          skill.Description = "Determines priority from a prior classification.";
          skill.Examples = ["Assess the priority of this incident classification."];
          skill.InputModes = ["application/json"];
          skill.OutputModes = ["application/json"];
        });
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
