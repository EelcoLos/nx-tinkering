namespace Assessor.Skills;

using FastEndpoints;
using FastEndpoints.A2A;
using System.Text.Json.Serialization;

/// <summary>
/// A2A Skill: Assesses priority from a classification
/// </summary>
[A2ASkill("assess-priority")]
public class AssessSkillEndpoint : Endpoint<AssessRequest, AssessResponse>
{
    public override void Configure()
    {
        Post("/assess-internal");
        AllowAnonymous();
        Description(b => b
            .WithName("Assess Priority")
            .WithDescription("Assesses priority level (critical, high, medium, low) based on classification"));
    }

    public override async Task HandleAsync(AssessRequest req, CancellationToken ct)
    {
        var classification = (req.Classification ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(classification))
        {
            ThrowError("Classification is required");
        }

        var priority = AssessPriority(classification);
        
        var response = new AssessResponse
        {
            Priority = priority.Level,
            Score = priority.Score,
            Description = $"Assessed priority as '{priority.Level}' (score: {priority.Score}/10)"
        };

        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: ct);
    }

    private static (string Level, int Score) AssessPriority(string classification)
    {
        return classification switch
        {
            "incident" => ("critical", 10),
            "defect" => ("high", 8),
            "feature_request" => ("medium", 5),
            "inquiry" => ("low", 2),
            _ => ("medium", 5)
        };
    }
}

public record AssessRequest
{
    [JsonPropertyName("classification")]
    public string? Classification { get; init; }
}

public record AssessResponse
{
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}
