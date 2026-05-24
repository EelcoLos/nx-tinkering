using A2ADemo.Common;

namespace A2ADemo.Assessor;

public sealed record ServiceSettings(
    string ServiceName,
    int Port,
    string ServiceBaseUrl,
    string DiscoveryServiceUrl,
    string IdentityServiceUrl,
    string JwtSecretKey,
    string AgentId,
    bool OtelEnabled,
    string OtelExporterEndpoint,
    string OtelServiceNamespace) : IToolServiceSettings
{
    public static ServiceSettings Create()
    {
        const string serviceName = "assessor";
        const int port = 5053;
        const string defaultBaseUrl = "http://assessor:5053";
        const string envPrefix = "ASSESSOR";

        return new ServiceSettings(
            serviceName,
            port,
            Environment.GetEnvironmentVariable($"{envPrefix}_SERVICE_URL") ?? defaultBaseUrl,
            Environment.GetEnvironmentVariable("DISCOVERY_SERVICE_URL") ?? "http://discovery:5051",
            Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? "http://identity:5050",
            Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-256-bit-secret-key-must-be-min-32-chars",
            Environment.GetEnvironmentVariable($"{envPrefix}_AGENT_ID") ?? $"{serviceName}-agent",
            bool.TryParse(Environment.GetEnvironmentVariable("OTEL_ENABLED"), out var enabled) && enabled,
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://tempo:4317",
            Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? "a2a-docker-demo");
    }
}