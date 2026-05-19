namespace Router.Skills;

using FastEndpoints;
using FastEndpoints.A2A;
using System.Text.Json.Serialization;

/// <summary>
/// A2A Skill: Routes incidents to appropriate handler
/// </summary>
[A2ASkill("route-incident")]
public class RouteSkillEndpoint : Endpoint<RouteRequest, RouteResponse>
{
    public override void Configure()
    {
        Post("/route-internal");
        AllowAnonymous();
        Description(b => b
            .WithName("Route Incident")
            .WithDescription("Routes incidents to appropriate handler teams based on priority"));
    }

    public override async Task HandleAsync(RouteRequest req, CancellationToken ct)
    {
        var priority = (req.Priority ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(priority))
        {
            ThrowError("Priority is required");
        }

        var route = RouteIncident(priority);
        
        var response = new RouteResponse
        {
            Team = route.Team,
            Escalated = route.Escalated,
            Description = $"Routed to {route.Team} (escalated: {route.Escalated})"
        };

        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: ct);
    }

    private static (string Team, bool Escalated) RouteIncident(string priority)
    {
        return priority switch
        {
            "critical" => ("ops-critical", true),
            "high" => ("ops-high", true),
            "medium" => ("ops-standard", false),
            "low" => ("support-tier-1", false),
            _ => ("ops-standard", false)
        };
    }
}

public record RouteRequest
{
    [JsonPropertyName("priority")]
    public string? Priority { get; init; }
}

public record RouteResponse
{
    [JsonPropertyName("team")]
    public string Team { get; set; } = "";

    [JsonPropertyName("escalated")]
    public bool Escalated { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}
