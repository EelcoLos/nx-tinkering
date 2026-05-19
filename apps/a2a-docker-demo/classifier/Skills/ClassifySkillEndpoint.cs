namespace Classifier.Skills;

using FastEndpoints;
using System.Text.Json.Serialization;

/// <summary>
/// A2A Skill: Classifies text input into predefined categories
/// Exposed only via /a2a JSON-RPC endpoint, not direct HTTP
/// </summary>
public class ClassifySkillEndpoint : Endpoint<ClassifyRequest, ClassifyResponse>
{
    public override void Configure()
    {
        Post("/classify");
        AllowAnonymous();
        
        // Register as A2A skill
        // Accessible ONLY via /a2a JSON-RPC, not direct HTTP POST
        this.A2ASkill(skill => skill
            .WithId("classify-text")
            .WithName("Classify Text")
            .WithDescription("Classifies text into categories: incident, defect, feature_request, inquiry, or general")
            .WithTag("text-classification")
            .WithTag("triage")
            .WithExample("The system is down - production outage")
        );
    }

    public override async Task HandleAsync(ClassifyRequest req, CancellationToken ct)
    {
        var text = (req.Text ?? "").Trim();
        if (string.IsNullOrEmpty(text))
        {
            ThrowError("Text input is required");
        }

        var classification = ClassifyText(text);
        
        var response = new ClassifyResponse
        {
            Classification = classification.Category,
            Confidence = classification.Confidence,
            Description = $"Text classified as '{classification.Category}' with {classification.Confidence:P0} confidence"
        };

        await SendOkAsync(ct);
        await HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: ct);
    }

    private static (string Category, decimal Confidence) ClassifyText(string text)
    {
        var normalized = text.ToLowerInvariant();

        return normalized switch
        {
            _ when normalized.Contains("critical") || 
                   normalized.Contains("urgent") || 
                   normalized.Contains("down") || 
                   normalized.Contains("outage")
                => ("incident", 0.95m),

            _ when normalized.Contains("bug") || 
                   normalized.Contains("error") || 
                   normalized.Contains("fail") || 
                   normalized.Contains("issue")
                => ("defect", 0.90m),

            _ when normalized.Contains("feature") || 
                   normalized.Contains("enhancement") || 
                   normalized.Contains("request")
                => ("feature_request", 0.85m),

            _ when normalized.Contains("help") || 
                   normalized.Contains("question") || 
                   normalized.Contains("how")
                => ("inquiry", 0.80m),

            _ => ("general", 0.70m)
        };
    }
}

public record ClassifyRequest
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

public record ClassifyResponse
{
    [JsonPropertyName("classification")]
    public string Classification { get; set; } = "general";

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}
