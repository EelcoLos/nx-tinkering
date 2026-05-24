namespace A2ADemo.Identity;

public sealed record AuthSettings(
    string ServiceName,
    bool OidcEnabled,
    string JwtSecretKey,
    string OtelExporterEndpoint,
    string OtelServiceNamespace,
    bool OtelEnabled,
    string OidcTokenEndpoint,
    string OidcIntrospectionEndpoint,
    string OidcUserClientId,
    string OidcUserClientSecret,
    string OidcIdentityClientId,
    string OidcIdentityClientSecret,
    IReadOnlyDictionary<string, AgentClientCredentials> AgentClients)
{
    public static AuthSettings Create()
    {
        var discoveryAgentId = Environment.GetEnvironmentVariable("DISCOVERY_AGENT_ID") ?? "discovery-agent";
        var classifierAgentId = Environment.GetEnvironmentVariable("CLASSIFIER_AGENT_ID") ?? "classifier-agent";
        var assessorAgentId = Environment.GetEnvironmentVariable("ASSESSOR_AGENT_ID") ?? "assessor-agent";
        var routerAgentId = Environment.GetEnvironmentVariable("ROUTER_AGENT_ID") ?? "router-agent";
        var handlerAgentId = Environment.GetEnvironmentVariable("HANDLER_AGENT_ID") ?? "handler-agent";
        var apiBackendAgentId = Environment.GetEnvironmentVariable("API_BACKEND_AGENT_ID") ?? "api-backend-agent";

        return new AuthSettings(
            "identity",
            bool.TryParse(Environment.GetEnvironmentVariable("OIDC_ENABLED"), out var enabled) && enabled,
            Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-256-bit-secret-key-must-be-min-32-chars",
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://tempo:4317",
            Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? "a2a-docker-demo",
            bool.TryParse(Environment.GetEnvironmentVariable("OTEL_ENABLED"), out var otelEnabled) && otelEnabled,
            Environment.GetEnvironmentVariable("OIDC_TOKEN_ENDPOINT") ?? string.Empty,
            Environment.GetEnvironmentVariable("OIDC_INTROSPECTION_ENDPOINT") ?? string.Empty,
            Environment.GetEnvironmentVariable("OIDC_USER_CLIENT_ID") ?? "website-client",
            Environment.GetEnvironmentVariable("OIDC_USER_CLIENT_SECRET") ?? string.Empty,
            Environment.GetEnvironmentVariable("OIDC_IDENTITY_CLIENT_ID") ?? "identity-facade",
            Environment.GetEnvironmentVariable("OIDC_IDENTITY_CLIENT_SECRET") ?? string.Empty,
            new Dictionary<string, AgentClientCredentials>(StringComparer.OrdinalIgnoreCase)
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
            });
    }
}

public sealed record AgentClientCredentials(string ClientId, string ClientSecret);