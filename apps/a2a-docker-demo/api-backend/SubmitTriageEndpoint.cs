using FluentValidation;
using Microsoft.AspNetCore.Http.Features;

namespace A2ADemo.ApiBackend;

public sealed record SubmitTriageRequest(
    string? Input);

public sealed class SubmitTriageRequestValidator : Validator<SubmitTriageRequest>
{
    public SubmitTriageRequestValidator()
    {
        RuleFor(request => request.Input)
            .NotEmpty()
            .WithMessage("input is required");
    }
}

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
        var input = req.Input!.Trim();
        TriageExceptionHandler.StoreSubmittedInput(HttpContext, input);

        var correlationId = HttpContext.Features.Get<IHttpActivityFeature>()?.Activity?.TraceId.ToString()
            ?? Activity.Current?.TraceId.ToString();

        var record = await gateway.RunTriageAsync(input, correlationId, ct);
        store.Save(record);
        await Send.OkAsync(record, ct);
    }
}