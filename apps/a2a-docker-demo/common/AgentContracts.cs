namespace A2ADemo.Common;

public sealed record AgentCard(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("skills")] IReadOnlyList<AgentSkill> Skills);

public sealed record AgentSkill(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description);

public sealed record AgentTokenResponse(
    [property: JsonPropertyName("token")] string? Token);

public sealed record ValidateTokenRequest(
    [property: JsonPropertyName("token")] string? Token);

public sealed record ValidatedToken(
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("agent_id")] string? AgentId,
    [property: JsonPropertyName("username")] string? Username);

public sealed record A2AMetadata(
    [property: JsonPropertyName("agent_jwt")] string? AgentJwt);
