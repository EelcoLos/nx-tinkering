namespace A2ADemo.ApiBackend;

public sealed record ServiceSummary(
    [property: JsonPropertyName("service_id")] string ServiceId,
    string Name,
    [property: JsonPropertyName("base_url")] string BaseUrl,
    int Port,
    string Description,
    IReadOnlyList<string> Skills,
    [property: JsonPropertyName("registered_at")] DateTimeOffset RegisteredAt);