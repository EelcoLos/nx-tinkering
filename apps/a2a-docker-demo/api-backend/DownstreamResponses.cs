using System.Text.Json.Serialization;

namespace A2ADemo.ApiBackend;

public sealed record ClassificationResponse(
    [property: JsonPropertyName("classification_type")] string ClassificationType);

public sealed record AssessmentResponse(
    [property: JsonPropertyName("priority")] string Priority);

public sealed record RoutingResponse(
    [property: JsonPropertyName("next_handler")] string NextHandler);

public sealed record HandlingResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("ticket_id")] string TicketId,
    [property: JsonPropertyName("summary")] string Summary);