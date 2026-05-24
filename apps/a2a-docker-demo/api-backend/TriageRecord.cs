namespace A2ADemo.ApiBackend;

public sealed record TriageRecord
{
    public string Id { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    [JsonPropertyName("correlation_id")] public string? CorrelationId { get; set; }
    public string? Classification { get; set; }
    public string? Priority { get; set; }
    [JsonPropertyName("next_handler")] public string? NextHandler { get; set; }
    [JsonPropertyName("ticket_id")] public string? TicketId { get; set; }
    public string? Summary { get; set; }
    public string Status { get; set; } = "pending";
    public string? Error { get; set; }
    public List<TraceEntry> Trace { get; set; } = [];
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }
}

public sealed record TraceEntry(
    string Service,
    [property: JsonPropertyName("correlation_id")] string? CorrelationId,
    string Status,
    [property: JsonPropertyName("timestamp_ms")] long TimestampMs,
    string Result);