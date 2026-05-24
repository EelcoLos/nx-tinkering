namespace A2ADemo.Identity;

public sealed record OidcTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("sub")] string? Subject,
    [property: JsonPropertyName("preferred_username")] string? Username);