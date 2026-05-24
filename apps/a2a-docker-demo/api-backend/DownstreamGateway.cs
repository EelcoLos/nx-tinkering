using A2ADemo.Common;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace A2ADemo.ApiBackend;

public sealed class DownstreamGateway(
    IHttpClientFactory httpClientFactory,
    IdentityClient identityClient,
    ServiceSettings settings)
{
    public async Task<IReadOnlyCollection<ServiceSummary>> GetServicesAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        var token = await identityClient.GetAgentTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.DiscoveryServiceUrl}/services");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ServiceSummary>>(cancellationToken: ct) ?? [];
    }

    public async Task<AgentCard?> GetServiceCardAsync(string serviceId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        var token = await identityClient.GetAgentTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.DiscoveryServiceUrl}/services/{Uri.EscapeDataString(serviceId)}/card");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentCard>(cancellationToken: ct);
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
        var classification = await PostAsync<ClassificationResponse>(client, $"{settings.ClassifierServiceUrl}/skills/classify", new
        {
            input,
            metadata = new { agent_jwt = agentToken }
        }, ct);
        record.Classification = classification.ClassificationType;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry("Classifier", correlationId, "completed", (long)(DateTimeOffset.UtcNow - classifyStart).TotalMilliseconds, classification.ClassificationType));

        var assessStart = DateTimeOffset.UtcNow;
        var assessment = await PostAsync<AssessmentResponse>(client, $"{settings.AssessorServiceUrl}/skills/assess", new
        {
            classification = classification.ClassificationType,
            metadata = new { agent_jwt = agentToken }
        }, ct);
        record.Priority = assessment.Priority;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry("Assessor", correlationId, "completed", (long)(DateTimeOffset.UtcNow - assessStart).TotalMilliseconds, assessment.Priority));

        var routeStart = DateTimeOffset.UtcNow;
        var routing = await PostAsync<RoutingResponse>(client, $"{settings.RouterServiceUrl}/skills/route", new
        {
            priority = assessment.Priority,
            metadata = new { agent_jwt = agentToken }
        }, ct);
        record.NextHandler = routing.NextHandler;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry("Router", correlationId, "completed", (long)(DateTimeOffset.UtcNow - routeStart).TotalMilliseconds, routing.NextHandler));

        var handleStart = DateTimeOffset.UtcNow;
        var handling = await PostAsync<HandlingResponse>(client, $"{settings.HandlerServiceUrl}/skills/handle", new
        {
            input,
            classification = classification.ClassificationType,
            priority = assessment.Priority,
            metadata = new { agent_jwt = agentToken }
        }, ct);

        record.Status = handling.Status;
        record.TicketId = handling.TicketId;
        record.Summary = handling.Summary;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry("Handler", correlationId, "completed", (long)(DateTimeOffset.UtcNow - handleStart).TotalMilliseconds, handling.TicketId));

        workflowActivity?.SetStatus(ActivityStatusCode.Ok);
        return record;
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string url, object payload, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(url, payload, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException($"{url} returned an empty response.");
    }
}