using A2ADemo.Common;

namespace A2ADemo.Discovery;

public sealed class ServiceSettings : IIdentityServiceSettings
{
    public string ServiceName { get; set; } = "discovery";
    public int Port { get; set; } = 5051;
    public string ServiceBaseUrl { get; set; } = "http://discovery:5051";
    public string IdentityServiceUrl { get; set; } = "http://identity:5050";
    public string JwtSecretKey { get; set; } = "your-256-bit-secret-key-must-be-min-32-chars";
    public string AgentId { get; set; } = "discovery-agent";
    public bool OtelEnabled { get; set; }
    public string OtelExporterEndpoint { get; set; } = "http://tempo:4317";
    public string OtelServiceNamespace { get; set; } = "a2a-docker-demo";

    public static void Configure(ServiceSettings options)
    {
        options.ServiceBaseUrl = Environment.GetEnvironmentVariable("DISCOVERY_SERVICE_URL") ?? options.ServiceBaseUrl;
        options.IdentityServiceUrl = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? options.IdentityServiceUrl;
        options.JwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? options.JwtSecretKey;
        options.AgentId = Environment.GetEnvironmentVariable("DISCOVERY_AGENT_ID") ?? options.AgentId;
        options.OtelEnabled = bool.TryParse(Environment.GetEnvironmentVariable("OTEL_ENABLED"), out var enabled) && enabled;
        options.OtelExporterEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? options.OtelExporterEndpoint;
        options.OtelServiceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? options.OtelServiceNamespace;
    }
}