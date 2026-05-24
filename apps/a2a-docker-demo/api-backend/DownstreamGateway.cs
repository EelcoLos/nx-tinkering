using A2ADemo.Common;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2ADemo.ApiBackend;

public sealed class DownstreamGateway(
    IHttpClientFactory httpClientFactory,
    IdentityClient identityClient,
    IOptions<ServiceSettings> settingsOptions)
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ServiceSettings settings = settingsOptions.Value;

    public async Task<IReadOnlyCollection<ServiceSummary>> GetServicesAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        var token = await identityClient.GetAgentTokenAsync(ct);
        var summaries = await Task.WhenAll(GetKnownServices().Select(async service =>
        {
            var card = await GetAgentCardAsync(client, service, token, ct);
            return ToServiceSummary(service, card);
        }));

        return summaries;
    }

    public async Task<A2AAgentCard?> GetServiceCardAsync(string serviceId, CancellationToken ct)
    {
        var service = GetKnownServices().FirstOrDefault(candidate =>
            string.Equals(candidate.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase));

        if (service is null)
        {
            return null;
        }

        var client = httpClientFactory.CreateClient();
        var token = await identityClient.GetAgentTokenAsync(ct);
        return await GetAgentCardAsync(client, service, token, ct);
    }

    public async Task<TriageRecord> RunTriageAsync(string input, string? correlationId, CancellationToken ct)
    {
        using var startedWorkflowActivity = TelemetryExtensions.StartWorkflowActivity(DemoTelemetry.ActivitySource, "a2a-triage");
        var workflowActivity = startedWorkflowActivity ?? Activity.Current;
        workflowActivity?.SetTag("a2a.input.length", input.Length);
        correlationId ??= TelemetryExtensions.GetCorrelationId(workflowActivity);
        workflowActivity?.SetTag("correlationId", correlationId);
        workflowActivity?.SetTag("a2a.correlation_id", correlationId);

        var startTime = DateTimeOffset.UtcNow;
        var agentToken = await identityClient.GetAgentTokenAsync(ct);
        var client = httpClientFactory.CreateClient();
        var record = new TriageRecord
        {
            Id = $"triage-{Guid.NewGuid():N}"[..19],
            Input = input,
            CorrelationId = correlationId,
            Status = "processing",
            CreatedAt = startTime,
            UpdatedAt = startTime
        };

        var classifyStart = DateTimeOffset.UtcNow;
        var classification = await SendSkillAsync<ClassificationResponse>(client, $"{settings.ClassifierServiceUrl.TrimEnd('/')}/a2a", "classifier", new ClassifySkillInput(input), agentToken, ct);
        record.Classification = classification.ClassificationType;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry("Classifier", correlationId, "completed", (long)(DateTimeOffset.UtcNow - classifyStart).TotalMilliseconds, classification.ClassificationType));

        var assessStart = DateTimeOffset.UtcNow;
        var assessment = await SendSkillAsync<AssessmentResponse>(client, $"{settings.AssessorServiceUrl.TrimEnd('/')}/a2a", "assessor", new AssessSkillInput(classification.ClassificationType), agentToken, ct);
        record.Priority = assessment.Priority;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry("Assessor", correlationId, "completed", (long)(DateTimeOffset.UtcNow - assessStart).TotalMilliseconds, assessment.Priority));

        var routeStart = DateTimeOffset.UtcNow;
        var routing = await SendSkillAsync<RoutingResponse>(client, $"{settings.RouterServiceUrl.TrimEnd('/')}/a2a", "router", new RouteSkillInput(assessment.Priority), agentToken, ct);
        record.NextHandler = routing.NextHandler;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry("Router", correlationId, "completed", (long)(DateTimeOffset.UtcNow - routeStart).TotalMilliseconds, routing.NextHandler));

        var handleStart = DateTimeOffset.UtcNow;
        var handling = await SendSkillAsync<HandlingResponse>(client, $"{settings.HandlerServiceUrl.TrimEnd('/')}/a2a", "handler", new HandleSkillInput(input, classification.ClassificationType, assessment.Priority), agentToken, ct);

        record.Status = handling.Status;
        record.TicketId = handling.TicketId;
        record.Summary = handling.Summary;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry("Handler", correlationId, "completed", (long)(DateTimeOffset.UtcNow - handleStart).TotalMilliseconds, handling.TicketId));

        workflowActivity?.SetStatus(ActivityStatusCode.Ok);
        return record;
    }

    private static async Task<T> SendSkillAsync<T>(HttpClient client, string url, string skillId, object payload, string bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new A2ARequest(
                "2.0",
                Guid.NewGuid().ToString("N"),
                "SendMessage",
                new A2ARequestParams(
                    new A2AMessage(
                        Guid.NewGuid().ToString("N"),
                        "user",
                        [new A2APart(JsonSerializer.SerializeToElement(payload))]),
                    new A2AMetadata(skillId))))
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Headers.Add("A2A-Version", "1.0");

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<A2AResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException($"{url} returned an empty response.");

        var data = body.Result?.Message?.Parts?.FirstOrDefault()?.Data
            ?? throw new InvalidOperationException($"{url} returned no A2A data part.");

        return data.Deserialize<T>(WebJsonOptions)
            ?? throw new InvalidOperationException($"{url} returned an unreadable A2A response payload.");
    }

    private async Task<A2AAgentCard> GetAgentCardAsync(HttpClient client, KnownA2AService service, string bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, service.AgentCardUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<A2AAgentCard>(cancellationToken: ct)
            ?? throw new InvalidOperationException($"{service.AgentCardUrl} returned an empty agent card.");
    }

    private IReadOnlyList<KnownA2AService> GetKnownServices() =>
    [
        new("api-backend", settings.ServiceBaseUrl),
        new("classifier", settings.ClassifierServiceUrl),
        new("assessor", settings.AssessorServiceUrl),
        new("router", settings.RouterServiceUrl),
        new("handler", settings.HandlerServiceUrl)
    ];

    private static ServiceSummary ToServiceSummary(KnownA2AService service, A2AAgentCard card)
    {
        var rpcUrl = card.SupportedInterfaces.FirstOrDefault()?.Url ?? $"{service.BaseUrl.TrimEnd('/')}/a2a";
        var baseUrl = GetBaseUrl(rpcUrl, service.BaseUrl);
        var port = Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ? baseUri.Port : 0;

        return new ServiceSummary(
            service.ServiceId,
            card.Name,
            baseUrl,
            port,
            card.Description,
            card.Skills.Select(skill => skill.Id).ToArray(),
            DateTimeOffset.UtcNow);
    }

    private static string GetBaseUrl(string rpcUrl, string fallbackBaseUrl)
    {
        if (!Uri.TryCreate(rpcUrl, UriKind.Absolute, out var uri))
        {
            return fallbackBaseUrl;
        }

        var path = uri.AbsolutePath;
        if (path.EndsWith("/a2a", StringComparison.OrdinalIgnoreCase))
        {
            path = path[..^4];
        }

        if (!path.EndsWith('/'))
        {
            path += "/";
        }

        var builder = new UriBuilder(uri)
        {
            Path = path,
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.ToString().TrimEnd('/');
    }
}

internal sealed record KnownA2AService(string ServiceId, string BaseUrl)
{
    public string AgentCardUrl => $"{BaseUrl.TrimEnd('/')}/.well-known/agent-card.json";
}

file sealed record ClassifySkillInput(string Input);
file sealed record AssessSkillInput(string Classification);
file sealed record RouteSkillInput(string Priority);
file sealed record HandleSkillInput(string Input, string Classification, string Priority);

file sealed record A2ARequest(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    string Id,
    string Method,
    A2ARequestParams Params);

file sealed record A2ARequestParams(
    A2AMessage Message,
    A2AMetadata Metadata);

file sealed record A2AMessage(
    string MessageId,
    string Role,
    IReadOnlyList<A2APart> Parts);

file sealed record A2APart(
    JsonElement Data);

file sealed record A2AMetadata(
    string Skill);

file sealed record A2AResponse(
    A2AResult? Result);

file sealed record A2AResult(
    A2AResultMessage? Message);

file sealed record A2AResultMessage(
    IReadOnlyList<A2AResultPart>? Parts);

file sealed record A2AResultPart(
    JsonElement? Data);