using System.Text.Json.Serialization;

namespace A2ADemo.ApiBackend;

public sealed record ServiceSummary(
    [property: JsonPropertyName("service_id")] string ServiceId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("base_url")] string BaseUrl,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("skills")] IReadOnlyList<string> Skills,
    [property: JsonPropertyName("registered_at")] DateTimeOffset RegisteredAt);