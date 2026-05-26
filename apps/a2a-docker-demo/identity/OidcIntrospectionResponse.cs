namespace A2ADemo.Identity;

public sealed record OidcIntrospectionResponse(
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("sub")] string? Subject,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("client_id")] string? ClientId);
