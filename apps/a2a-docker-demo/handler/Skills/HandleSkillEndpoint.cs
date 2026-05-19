namespace Handler.Skills;

using FastEndpoints;
using FastEndpoints.A2A;
using System.Text.Json.Serialization;

/// <summary>
/// A2A Skill: Creates ticket from incident data
/// </summary>
[A2ASkill("create-ticket")]
public class HandleSkillEndpoint : Endpoint<HandleRequest, HandleResponse>
{
    public override void Configure()
    {
        Post("/handle-internal");
        AllowAnonymous();
        Description(b => b
            .WithName("Create Ticket")
            .WithDescription("Creates a ticket from incident data for tracking and resolution"));
    }

    public override async Task HandleAsync(HandleRequest req, CancellationToken ct)
    {
        var subject = (req.Subject ?? "").Trim();
        if (string.IsNullOrEmpty(subject))
        {
            ThrowError("Subject is required");
        }

        var ticketId = GenerateTicketId();
        
        var response = new HandleResponse
        {
            TicketId = ticketId,
            Status = "created",
            Description = $"Ticket {ticketId} created for: {subject}"
        };

        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: ct);
    }

    private static string GenerateTicketId()
    {
        return $"TKT-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}

public record HandleRequest
{
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    [JsonPropertyName("team")]
    public string? Team { get; init; }
}

public record HandleResponse
{
    [JsonPropertyName("ticketId")]
    public string TicketId { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "created";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}
