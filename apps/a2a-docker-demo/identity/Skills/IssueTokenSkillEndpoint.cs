using FastEndpoints;
using FastEndpoints.A2A;
using Identity.Infrastructure;

namespace Identity.Skills;

[A2ASkill("issue-token")]
public class IssueTokenSkillEndpoint : EndpointWithoutRequest
{
    private readonly JwtService _jwt;

    public IssueTokenSkillEndpoint(JwtService jwt) => _jwt = jwt;

    public override void Configure()
    {
        Post("/");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var token = _jwt.IssueAgentToken("api-orchestrator", new[] { "triage:execute" });

        var response = new { access_token = token, token_type = "Bearer", expires_in = 3600 };
        await HttpContext.Response.WriteAsJsonAsync(response, ct);
    }
}
