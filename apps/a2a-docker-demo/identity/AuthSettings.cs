namespace A2ADemo.Identity;

public sealed class AuthSettings
{
    public string ServiceName { get; set; } = "identity";
    public bool OidcEnabled { get; set; }
    public string JwtSecretKey { get; set; } = "your-256-bit-secret-key-must-be-min-32-chars";
    public string OtelExporterEndpoint { get; set; } = "http://tempo:4317";
    public string OtelServiceNamespace { get; set; } = "a2a-docker-demo";
    public bool OtelEnabled { get; set; }
    public string OidcTokenEndpoint { get; set; } = string.Empty;
    public string OidcIntrospectionEndpoint { get; set; } = string.Empty;
    public string OidcUserClientId { get; set; } = "website-client";
    public string OidcUserClientSecret { get; set; } = string.Empty;
    public string OidcIdentityClientId { get; set; } = "identity-facade";
    public string OidcIdentityClientSecret { get; set; } = string.Empty;
    public string DemoUserUsername { get; set; } = "admin";
    public string DemoUserPassword { get; set; } = "demo123";
    public string DemoUser2Username { get; set; } = "user";
    public string DemoUser2Password { get; set; } = "user456";
    public IReadOnlyDictionary<string, AgentClientCredentials> AgentClients { get; set; } = new Dictionary<string, AgentClientCredentials>(StringComparer.OrdinalIgnoreCase);

    public static void Configure(AuthSettings options)
    {
        var discoveryAgentId = Environment.GetEnvironmentVariable("DISCOVERY_AGENT_ID") ?? "discovery-agent";
        var classifierAgentId = Environment.GetEnvironmentVariable("CLASSIFIER_AGENT_ID") ?? "classifier-agent";
        var assessorAgentId = Environment.GetEnvironmentVariable("ASSESSOR_AGENT_ID") ?? "assessor-agent";
        var routerAgentId = Environment.GetEnvironmentVariable("ROUTER_AGENT_ID") ?? "router-agent";
        var handlerAgentId = Environment.GetEnvironmentVariable("HANDLER_AGENT_ID") ?? "handler-agent";
        var apiBackendAgentId = Environment.GetEnvironmentVariable("API_BACKEND_AGENT_ID") ?? "api-backend-agent";

        options.OidcEnabled = bool.TryParse(Environment.GetEnvironmentVariable("OIDC_ENABLED"), out var enabled) && enabled;
        options.JwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? options.JwtSecretKey;
        options.OtelExporterEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? options.OtelExporterEndpoint;
        options.OtelServiceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? options.OtelServiceNamespace;
        options.OtelEnabled = bool.TryParse(Environment.GetEnvironmentVariable("OTEL_ENABLED"), out var otelEnabled) && otelEnabled;
        options.OidcTokenEndpoint = Environment.GetEnvironmentVariable("OIDC_TOKEN_ENDPOINT") ?? options.OidcTokenEndpoint;
        options.OidcIntrospectionEndpoint = Environment.GetEnvironmentVariable("OIDC_INTROSPECTION_ENDPOINT") ?? options.OidcIntrospectionEndpoint;
        options.OidcUserClientId = Environment.GetEnvironmentVariable("OIDC_USER_CLIENT_ID") ?? options.OidcUserClientId;
        options.OidcUserClientSecret = Environment.GetEnvironmentVariable("OIDC_USER_CLIENT_SECRET") ?? options.OidcUserClientSecret;
        options.OidcIdentityClientId = Environment.GetEnvironmentVariable("OIDC_IDENTITY_CLIENT_ID") ?? options.OidcIdentityClientId;
        options.OidcIdentityClientSecret = Environment.GetEnvironmentVariable("OIDC_IDENTITY_CLIENT_SECRET") ?? options.OidcIdentityClientSecret;
        options.DemoUserUsername = Environment.GetEnvironmentVariable("DEMO_USER_USERNAME") ?? options.DemoUserUsername;
        options.DemoUserPassword = Environment.GetEnvironmentVariable("DEMO_USER_PASSWORD") ?? options.DemoUserPassword;
        options.DemoUser2Username = Environment.GetEnvironmentVariable("DEMO_USER2_USERNAME") ?? options.DemoUser2Username;
        options.DemoUser2Password = Environment.GetEnvironmentVariable("DEMO_USER2_PASSWORD") ?? options.DemoUser2Password;
        options.AgentClients = new Dictionary<string, AgentClientCredentials>(StringComparer.OrdinalIgnoreCase)
        {
            [discoveryAgentId] = new(
                Environment.GetEnvironmentVariable("OIDC_DISCOVERY_CLIENT_ID") ?? discoveryAgentId,
                Environment.GetEnvironmentVariable("OIDC_DISCOVERY_CLIENT_SECRET") ?? string.Empty),
            [classifierAgentId] = new(
                Environment.GetEnvironmentVariable("OIDC_CLASSIFIER_CLIENT_ID") ?? classifierAgentId,
                Environment.GetEnvironmentVariable("OIDC_CLASSIFIER_CLIENT_SECRET") ?? string.Empty),
            [assessorAgentId] = new(
                Environment.GetEnvironmentVariable("OIDC_ASSESSOR_CLIENT_ID") ?? assessorAgentId,
                Environment.GetEnvironmentVariable("OIDC_ASSESSOR_CLIENT_SECRET") ?? string.Empty),
            [routerAgentId] = new(
                Environment.GetEnvironmentVariable("OIDC_ROUTER_CLIENT_ID") ?? routerAgentId,
                Environment.GetEnvironmentVariable("OIDC_ROUTER_CLIENT_SECRET") ?? string.Empty),
            [handlerAgentId] = new(
                Environment.GetEnvironmentVariable("OIDC_HANDLER_CLIENT_ID") ?? handlerAgentId,
                Environment.GetEnvironmentVariable("OIDC_HANDLER_CLIENT_SECRET") ?? string.Empty),
            [apiBackendAgentId] = new(
                Environment.GetEnvironmentVariable("OIDC_API_BACKEND_CLIENT_ID") ?? apiBackendAgentId,
                Environment.GetEnvironmentVariable("OIDC_API_BACKEND_CLIENT_SECRET") ?? string.Empty)
        };
    }
}

public sealed record AgentClientCredentials(string ClientId, string ClientSecret);