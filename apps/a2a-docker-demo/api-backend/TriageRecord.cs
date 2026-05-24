using System.Text.Json.Serialization;

namespace A2ADemo.ApiBackend;

public sealed record TriageRecord
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("input")] public string Input { get; set; } = string.Empty;
    [JsonPropertyName("correlation_id")] public string? CorrelationId { get; set; }
    [JsonPropertyName("classification")] public string? Classification { get; set; }
    [JsonPropertyName("priority")] public string? Priority { get; set; }
    [JsonPropertyName("next_handler")] public string? NextHandler { get; set; }
    [JsonPropertyName("ticket_id")] public string? TicketId { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "pending";
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("trace")] public List<TraceEntry> Trace { get; set; } = [];
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }
}

public sealed record TraceEntry(
    [property: JsonPropertyName("service")] string Service,
    [property: JsonPropertyName("correlation_id")] string? CorrelationId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("timestamp_ms")] long TimestampMs,
    [property: JsonPropertyName("result")] string Result);