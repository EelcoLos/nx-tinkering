using FastEndpoints;
using FastEndpoints.A2A;

namespace ApiBackend.Skills;

[A2ASkill("execute-triage")]
public class TriageOrchestrationSkillEndpoint : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TriageOrchestrationSkillEndpoint(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public override void Configure()
    {
        Post("/");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var result = new { status = "ok", message = "Triage orchestration skill" };
        await HttpContext.Response.WriteAsJsonAsync(result, ct);
    }
}
