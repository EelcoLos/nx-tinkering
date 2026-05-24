namespace A2ADemo.Classifier;

public sealed record ClassifyRequest(
    string? Input,
    A2AMetadata? Metadata);

public sealed record ClassifyResponse(
    [property: JsonPropertyName("classification_type")] string ClassificationType,
    string Result);

public sealed class SkillEndpoint : Endpoint<ClassifyRequest, ClassifyResponse>
{
    public override void Configure()
    {
        Post("/skills/classify");

        this.A2ASkill(
            id: "classifier",
            tags: ["triage", "classification"],
            configure: skill =>
            {
                skill.Name = "Classifier";
                skill.Description = "Classifies incoming text into a triage category.";
                skill.Examples = ["Classify this incident report."];
                skill.InputModes = ["application/json"];
                skill.OutputModes = ["application/json"];
            });
    }

    public override async Task HandleAsync(ClassifyRequest req, CancellationToken ct)
    {
        using var startedActivity = TelemetryExtensions.StartToolActivity(DemoTelemetry.ActivitySource, "classifier");
        var activity = startedActivity ?? Activity.Current;

        var input = req.Input?.Trim() ?? string.Empty;
        activity?.SetTag("a2a.input.length", input.Length);

        var normalized = input.ToLowerInvariant();
        var classification = normalized switch
        {
            _ when normalized.Contains("critical") || normalized.Contains("urgent") || normalized.Contains("down") || normalized.Contains("outage") => "incident",
            _ when normalized.Contains("bug") || normalized.Contains("error") || normalized.Contains("fail") || normalized.Contains("issue") => "defect",
            _ when normalized.Contains("feature") || normalized.Contains("enhancement") || normalized.Contains("request") => "feature_request",
            _ when normalized.Contains("help") || normalized.Contains("question") || normalized.Contains("how") => "inquiry",
            _ => "general"
        };

        activity?.SetTag("a2a.result", classification);
        activity?.SetTag("gen_ai.response.finish_reasons", new[] { "stop" });
        activity?.SetStatus(ActivityStatusCode.Ok);

        await Send.OkAsync(new ClassifyResponse(classification, $"Input classified as {classification}."), ct);
    }
}