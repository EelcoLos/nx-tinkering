namespace ApiBackend.Services;

public class ServiceSettings
{
    public string ServiceName { get; init; } = "";
    public int Port { get; init; }
    public string ServiceBaseUrl { get; init; } = "";
    public string DiscoveryServiceUrl { get; init; } = "";
    public string IdentityServiceUrl { get; init; } = "";
    public string JwtSecretKey { get; init; } = "";
    public string AgentId { get; init; } = "";

    public static ServiceSettings Create(string serviceName, int port, string defaultBaseUrl)
    {
        var envPrefix = serviceName.ToUpperInvariant().Replace('-', '_');
        
        // Validate JWT secret
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                       "your-256-bit-secret-key-must-be-min-32-chars-1234567890abcdef";
        
        if (jwtSecret.Length < 32)
            throw new InvalidOperationException($"JWT_SECRET_KEY must be at least 32 characters, got {jwtSecret.Length}");

        return new ServiceSettings
        {
            ServiceName = serviceName,
            Port = port,
            ServiceBaseUrl = Environment.GetEnvironmentVariable($"{envPrefix}_SERVICE_URL") ?? defaultBaseUrl,
            DiscoveryServiceUrl = Environment.GetEnvironmentVariable("DISCOVERY_SERVICE_URL") ?? "http://discovery:5051",
            IdentityServiceUrl = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? "http://identity:5050",
            JwtSecretKey = jwtSecret,
            AgentId = Environment.GetEnvironmentVariable($"{envPrefix}_AGENT_ID") ?? $"{serviceName}-agent"
        };
    }
}
