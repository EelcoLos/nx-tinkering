using Microsoft.AspNetCore.Http.Features;

namespace A2ADemo.ApiBackend;

public sealed record SubmitTriageRequest(
    string? Input);

public sealed class SubmitTriageEndpoint(DownstreamGateway gateway, TriageStore store) : Endpoint<SubmitTriageRequest, TriageRecord>
{
    public override void Configure()
    {
        Post("/api/triage");

        this.A2ASkill(
            id: "triage_orchestration",
            tags: ["triage", "orchestration"],
            configure: skill =>
            {
                skill.Name = "Triage Orchestration";
                skill.Description = "Coordinates the multi-step triage workflow.";
                skill.Examples = ["Triage this outage report."];
                skill.InputModes = ["application/json"];
                skill.OutputModes = ["application/json"];
            });
    }

    public override async Task HandleAsync(SubmitTriageRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Input))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "input is required" }, cancellationToken: ct);
            return;
        }

        try
        {
            var correlationId = HttpContext.Features.Get<IHttpActivityFeature>()?.Activity?.TraceId.ToString()
                ?? Activity.Current?.TraceId.ToString();

            var record = await gateway.RunTriageAsync(req.Input.Trim(), correlationId, ct);
            store.Save(record);
            await Send.OkAsync(record, ct);
        }
        catch (Exception ex)
        {
            var failedRecord = new TriageRecord
            {
                Id = $"triage-{Guid.NewGuid():N}"[..19],
                Input = req.Input.Trim(),
                Status = "failed",
                Error = ex.Message,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            store.Save(failedRecord);
            HttpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            await HttpContext.Response.WriteAsJsonAsync(failedRecord, cancellationToken: ct);
        }
    }
}