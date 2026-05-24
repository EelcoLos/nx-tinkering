namespace A2ADemo.Identity;

public sealed record AgentTokenResult(
    string Token,
    [property: JsonPropertyName("agent_id")] string AgentId);

public sealed class AgentTokenEndpoint(
    IOptions<AuthSettings> settingsOptions,
    OidcAuthClient oidcAuthClient,
    JwtService jwtService) : EndpointWithoutRequest<AgentTokenResult>
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

    public override async Task HandleAsync(CancellationToken ct)
    {
        var requestedAgentId = Query<string>("agentId");
        var agentId = string.IsNullOrWhiteSpace(requestedAgentId) ? "identity-agent" : requestedAgentId.Trim();

        if (settings.OidcEnabled)
        {
            if (!settings.AgentClients.ContainsKey(agentId))
            {
                AddError($"No OIDC client mapping found for agent '{agentId}'");
                await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
                return;
            }

            var oidcToken = await oidcAuthClient.GetAgentTokenAsync(agentId, ct);
            if (string.IsNullOrWhiteSpace(oidcToken))
            {
                AddError($"OIDC token request failed for agent '{agentId}'");
                await Send.ErrorsAsync(StatusCodes.Status502BadGateway, ct);
                return;
            }

            await Send.OkAsync(new AgentTokenResult(oidcToken, agentId), ct);
            return;
        }

        await Send.OkAsync(new AgentTokenResult(jwtService.GenerateAgentToken(agentId), agentId), ct);
    }
}