using A2ADemo.Common;

namespace A2ADemo.Discovery;

public sealed record ServiceSettings(
    string ServiceName,
    int Port,
    string ServiceBaseUrl,
    string IdentityServiceUrl,
    string JwtSecretKey,
    string AgentId,
    bool OtelEnabled,
    string OtelExporterEndpoint,
    string OtelServiceNamespace) : IIdentityServiceSettings
{
    public static ServiceSettings Create()
    {
        const string serviceName = "discovery";
        const int port = 5051;
        const string defaultBaseUrl = "http://discovery:5051";
        const string envPrefix = "DISCOVERY";

        return new ServiceSettings(
            serviceName,
            port,
            Environment.GetEnvironmentVariable($"{envPrefix}_SERVICE_URL") ?? defaultBaseUrl,
            Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? "http://identity:5050",
            Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-256-bit-secret-key-must-be-min-32-chars",
            Environment.GetEnvironmentVariable($"{envPrefix}_AGENT_ID") ?? $"{serviceName}-agent",
            bool.TryParse(Environment.GetEnvironmentVariable("OTEL_ENABLED"), out var enabled) && enabled,
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://tempo:4317",
            Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? "a2a-docker-demo");
    }
}