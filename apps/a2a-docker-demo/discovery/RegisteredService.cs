using System.Text.Json.Serialization;

namespace A2ADemo.Discovery;

public sealed record RegisteredService(
    [property: JsonPropertyName("service_id")] string ServiceId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("base_url")] string BaseUrl,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("skills")] IReadOnlyList<string> Skills,
    [property: JsonPropertyName("registered_at")] DateTimeOffset RegisteredAt,
    [property: JsonPropertyName("agent_card")] AgentCard AgentCard);