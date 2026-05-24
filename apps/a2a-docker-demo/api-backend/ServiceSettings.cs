using A2ADemo.Common;

namespace A2ADemo.ApiBackend;

public sealed class ServiceSettings : IToolServiceSettings
{
    public string ServiceName { get; set; } = "api-backend";
    public int Port { get; set; } = 5056;
    public string ServiceBaseUrl { get; set; } = "http://api-backend:5056";
    public string IdentityServiceUrl { get; set; } = "http://identity:5050";
    public string ClassifierServiceUrl { get; set; } = "http://classifier:5052";
    public string AssessorServiceUrl { get; set; } = "http://assessor:5053";
    public string RouterServiceUrl { get; set; } = "http://router:5054";
    public string HandlerServiceUrl { get; set; } = "http://handler:5055";
    public string JwtSecretKey { get; set; } = "your-256-bit-secret-key-must-be-min-32-chars";
    public string AgentId { get; set; } = "api-backend-agent";
    public bool OtelEnabled { get; set; }
    public string OtelExporterEndpoint { get; set; } = "http://tempo:4317";
    public string OtelServiceNamespace { get; set; } = "a2a-docker-demo";

    public static void Configure(ServiceSettings options)
    {
        options.ServiceBaseUrl = Environment.GetEnvironmentVariable("API_BACKEND_SERVICE_URL") ?? options.ServiceBaseUrl;
        options.IdentityServiceUrl = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? options.IdentityServiceUrl;
        options.ClassifierServiceUrl = Environment.GetEnvironmentVariable("CLASSIFIER_SERVICE_URL") ?? options.ClassifierServiceUrl;
        options.AssessorServiceUrl = Environment.GetEnvironmentVariable("ASSESSOR_SERVICE_URL") ?? options.AssessorServiceUrl;
        options.RouterServiceUrl = Environment.GetEnvironmentVariable("ROUTER_SERVICE_URL") ?? options.RouterServiceUrl;
        options.HandlerServiceUrl = Environment.GetEnvironmentVariable("HANDLER_SERVICE_URL") ?? options.HandlerServiceUrl;
        options.JwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? options.JwtSecretKey;
        options.AgentId = Environment.GetEnvironmentVariable("API_BACKEND_AGENT_ID") ?? options.AgentId;
        options.OtelEnabled = bool.TryParse(Environment.GetEnvironmentVariable("OTEL_ENABLED"), out var enabled) && enabled;
        options.OtelExporterEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? options.OtelExporterEndpoint;
        options.OtelServiceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? options.OtelServiceNamespace;
    }
}