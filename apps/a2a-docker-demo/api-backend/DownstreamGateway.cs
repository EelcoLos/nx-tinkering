using System.Text.Json;
using AgentCardModel = A2A.AgentCard;

namespace A2ADemo.ApiBackend;

public sealed class DownstreamGateway(
    IHttpClientFactory httpClientFactory,
    IdentityClient identityClient,
    ILogger<DownstreamGateway> logger,
    IOptions<ServiceSettings> settingsOptions)
{
  private readonly ServiceSettings settings = settingsOptions.Value;

  public async Task<IReadOnlyCollection<ServiceSummary>> GetServicesAsync(CancellationToken ct)
  {
    var token = await identityClient.GetAgentTokenAsync(ct);
    var summaries = await Task.WhenAll(GetKnownServices().Select(async service =>
    {
      var card = await GetAgentCardAsync(service, token, ct);
      return ToServiceSummary(service, card);
    }));

    return summaries;
  }

  public async Task<AgentCardModel?> GetServiceCardAsync(string serviceId, CancellationToken ct)
  {
    var service = GetKnownServices().FirstOrDefault(candidate =>
        string.Equals(candidate.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase));

    if (service is null)
    {
      return null;
    }

    var token = await identityClient.GetAgentTokenAsync(ct);
    return await GetAgentCardAsync(service, token, ct);
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
    var classification = await SendSkillAsync<ClassificationResponse>(settings.ClassifierServiceUrl, "classifier", new ClassifySkillInput(input), agentToken, ct);
    record.Classification = classification.ClassificationType;
    record.UpdatedAt = DateTimeOffset.UtcNow;
    record.Trace.Add(new TraceEntry("Classifier", correlationId, "completed", (long)(DateTimeOffset.UtcNow - classifyStart).TotalMilliseconds, classification.ClassificationType));

    var assessStart = DateTimeOffset.UtcNow;
    var assessment = await SendSkillAsync<AssessmentResponse>(settings.AssessorServiceUrl, "assessor", new AssessSkillInput(classification.ClassificationType), agentToken, ct);
    record.Priority = assessment.Priority;
    record.UpdatedAt = DateTimeOffset.UtcNow;
    record.Trace.Add(new TraceEntry("Assessor", correlationId, "completed", (long)(DateTimeOffset.UtcNow - assessStart).TotalMilliseconds, assessment.Priority));

    var routeStart = DateTimeOffset.UtcNow;
    var routing = await SendSkillAsync<RoutingResponse>(settings.RouterServiceUrl, "router", new RouteSkillInput(assessment.Priority), agentToken, ct);
    record.NextHandler = routing.NextHandler;
    record.UpdatedAt = DateTimeOffset.UtcNow;
    record.Trace.Add(new TraceEntry("Router", correlationId, "completed", (long)(DateTimeOffset.UtcNow - routeStart).TotalMilliseconds, routing.NextHandler));

    var handleStart = DateTimeOffset.UtcNow;
    var handling = await SendSkillAsync<HandlingResponse>(settings.HandlerServiceUrl, "handler", new HandleSkillInput(input, classification.ClassificationType, assessment.Priority), agentToken, ct);

    record.Status = handling.Status;
    record.TicketId = handling.TicketId;
    record.Summary = handling.Summary;
    record.UpdatedAt = DateTimeOffset.UtcNow;
    record.Trace.Add(new TraceEntry("Handler", correlationId, "completed", (long)(DateTimeOffset.UtcNow - handleStart).TotalMilliseconds, handling.TicketId));

    workflowActivity?.SetStatus(ActivityStatusCode.Ok);
    return record;
  }

  private async Task<T> SendSkillAsync<T>(string serviceBaseUrl, string skillId, object payload, string bearerToken, CancellationToken ct)
  {
    using var client = CreateA2AClient($"{serviceBaseUrl.TrimEnd('/')}/a2a", bearerToken);
    var response = await client.SendMessageAsync(new SendMessageRequest
    {
      Message = new Message
      {
        MessageId = Guid.NewGuid().ToString("N"),
        Role = Role.User,
        Parts = [Part.FromData(JsonSerializer.SerializeToElement(payload))]
      },
      Metadata = new Dictionary<string, JsonElement>
      {
        ["skill"] = JsonSerializer.SerializeToElement(skillId)
      }
    }, ct);

    var data = response.Message?.Parts?.FirstOrDefault(part => part.Data.HasValue)?.Data;
    if (data is null)
    {
      throw new InvalidOperationException($"{serviceBaseUrl} returned no A2A data part.");
    }

    return data.Value.Deserialize<T>(A2AJsonUtilities.DefaultOptions)
        ?? throw new InvalidOperationException($"{serviceBaseUrl} returned an unreadable A2A response payload.");
  }

  private async Task<AgentCardModel> GetAgentCardAsync(KnownA2AService service, string bearerToken, CancellationToken ct)
  {
    using var client = CreateAuthorizedHttpClient(bearerToken);
    var resolver = new A2ACardResolver(new Uri(EnsureTrailingSlash(service.BaseUrl)), client, "/.well-known/agent-card.json", logger);
    return await resolver.GetAgentCardAsync(ct)
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

  private static ServiceSummary ToServiceSummary(KnownA2AService service, AgentCardModel card)
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

  private A2AClient CreateA2AClient(string endpointUrl, string bearerToken) =>
      new(new Uri(endpointUrl), CreateAuthorizedHttpClient(bearerToken));

  private HttpClient CreateAuthorizedHttpClient(string bearerToken)
  {
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", bearerToken);
    return client;
  }

  private static string EnsureTrailingSlash(string url) => url.EndsWith('/') ? url : $"{url}/";

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
