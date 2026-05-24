using FluentValidation;

namespace A2ADemo.Identity;

public sealed record AgentTokenRequest(
    string? AgentId)
{
    public string ResolvedAgentId => string.IsNullOrWhiteSpace(AgentId) ? "identity-agent" : AgentId.Trim();
}

public sealed record AgentTokenResult(
    string Token,
    [property: JsonPropertyName("agent_id")] string AgentId);

public sealed class AgentTokenRequestValidator : Validator<AgentTokenRequest>
{
    public AgentTokenRequestValidator(IOptions<AuthSettings> settingsOptions)
    {
        var settings = settingsOptions.Value;

        RuleFor(request => request.ResolvedAgentId)
            .Must(agentId => !settings.OidcEnabled || settings.AgentClients.ContainsKey(agentId))
            .WithMessage(request => $"No OIDC client mapping found for agent '{request.ResolvedAgentId}'");
    }
}

public sealed class AgentTokenEndpoint(
    IOptions<AuthSettings> settingsOptions,
    OidcAuthClient oidcAuthClient,
    JwtService jwtService) : Endpoint<AgentTokenRequest, AgentTokenResult>
{
    private readonly AuthSettings settings = settingsOptions.Value;

    public override void Configure()
    {
        Get("/auth/agent/token");
        AllowAnonymous();
        Description(description => description
            .WithName("Get Agent Token")
            .WithDescription("DEMO ONLY: In production, this endpoint must require authentication (client credentials, mTLS, or API key). Currently allows any agent ID to be requested."));
    }

    public override async Task HandleAsync(AgentTokenRequest req, CancellationToken ct)
    {
        var agentId = req.ResolvedAgentId;

        var token = await GetTokenAsync(agentId, ct);
        if (token is null)
        {
            AddError($"OIDC token request failed for agent '{agentId}'");
            await Send.ErrorsAsync(StatusCodes.Status502BadGateway, ct);
            return;
        }

        await Send.OkAsync(new AgentTokenResult(token, agentId), ct);
    }

    private async Task<string?> GetTokenAsync(string agentId, CancellationToken ct)
    {
        if (!settings.OidcEnabled)
        {
            return jwtService.GenerateAgentToken(agentId);
        }

        var oidcToken = await oidcAuthClient.GetAgentTokenAsync(agentId, ct);
        return string.IsNullOrWhiteSpace(oidcToken) ? null : oidcToken;
    }
}