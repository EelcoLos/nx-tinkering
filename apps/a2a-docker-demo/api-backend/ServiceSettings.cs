using A2ADemo.Common;

namespace A2ADemo.ApiBackend;

public sealed record ServiceSettings(
    string ServiceName,
    int Port,
    string ServiceBaseUrl,
    string IdentityServiceUrl,
    string DiscoveryServiceUrl,
    string ClassifierServiceUrl,
    string AssessorServiceUrl,
    string RouterServiceUrl,
    string HandlerServiceUrl,
    string JwtSecretKey,
    string AgentId,
    bool OtelEnabled,
    string OtelExporterEndpoint,
    string OtelServiceNamespace) : IToolServiceSettings
{
    public static ServiceSettings Create() => new(
        "api-backend",
        5056,
        Environment.GetEnvironmentVariable("API_BACKEND_SERVICE_URL") ?? "http://api-backend:5056",
        Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? "http://identity:5050",
        Environment.GetEnvironmentVariable("DISCOVERY_SERVICE_URL") ?? "http://discovery:5051",
        Environment.GetEnvironmentVariable("CLASSIFIER_SERVICE_URL") ?? "http://classifier:5052",
        Environment.GetEnvironmentVariable("ASSESSOR_SERVICE_URL") ?? "http://assessor:5053",
        Environment.GetEnvironmentVariable("ROUTER_SERVICE_URL") ?? "http://router:5054",
        Environment.GetEnvironmentVariable("HANDLER_SERVICE_URL") ?? "http://handler:5055",
        Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-256-bit-secret-key-must-be-min-32-chars",
        Environment.GetEnvironmentVariable("API_BACKEND_AGENT_ID") ?? "api-backend-agent",
        bool.TryParse(Environment.GetEnvironmentVariable("OTEL_ENABLED"), out var enabled) && enabled,
        Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://tempo:4317",
        Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? "a2a-docker-demo");
}