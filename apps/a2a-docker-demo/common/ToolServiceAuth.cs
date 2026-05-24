using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace A2ADemo.Common;

public interface IIdentityServiceSettings
{
    string IdentityServiceUrl { get; }
    string AgentId { get; }
}

public interface IToolServiceSettings : IIdentityServiceSettings
{
    string ServiceName { get; }
    int Port { get; }
    string ServiceBaseUrl { get; }
    string DiscoveryServiceUrl { get; }
    string JwtSecretKey { get; }
    bool OtelEnabled { get; }
    string OtelExporterEndpoint { get; }
    string OtelServiceNamespace { get; }
}

public interface IServiceRegistrar
{
    Task RegisterAsync();
}

public sealed class JwtService(string jwtSecretKey)
{
    private readonly SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtSecretKey));

    public string GenerateAgentToken(string agentId)
    {
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", agentId),
                new Claim("agent_id", agentId),
                new Claim("type", "agent")
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public ValidatedToken? ValidateLocal(string token, string? expectedType = null)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);

            var validatedToken = new ValidatedToken(
                principal.FindFirst("sub")?.Value ?? string.Empty,
                principal.FindFirst("type")?.Value ?? string.Empty,
                principal.FindFirst("agent_id")?.Value,
                principal.FindFirst("username")?.Value);

            return !string.IsNullOrWhiteSpace(expectedType) &&
                   !string.Equals(validatedToken.Type, expectedType, StringComparison.OrdinalIgnoreCase)
                ? null
                : validatedToken;
        }
        catch
        {
            return null;
        }
    }
}

public sealed class IdentityClient(
    IHttpClientFactory httpClientFactory,
    IIdentityServiceSettings settings,
    JwtService jwtService)
{
    public async Task<ValidatedToken?> ValidateTokenAsync(string token, string expectedType, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            using var response = await client.PostAsJsonAsync(
                $"{settings.IdentityServiceUrl}/auth/validate",
                new ValidateTokenRequest(token),
                ct);

            if (response.IsSuccessStatusCode)
            {
                var validated = await response.Content.ReadFromJsonAsync<ValidatedToken>(cancellationToken: ct);
                if (validated is not null && string.Equals(validated.Type, expectedType, StringComparison.OrdinalIgnoreCase))
                {
                    return validated;
                }

                return null;
            }
        }
        catch
        {
        }

        return jwtService.ValidateLocal(token, expectedType);
    }

    public async Task<string> GetAgentTokenAsync(CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var url = $"{settings.IdentityServiceUrl}/auth/agent/token?agentId={Uri.EscapeDataString(settings.AgentId)}";
            var tokenResponse = await client.GetFromJsonAsync<AgentTokenResponse>(url, ct);
            if (!string.IsNullOrWhiteSpace(tokenResponse?.Token))
            {
                return tokenResponse.Token;
            }
        }
        catch
        {
        }

        return jwtService.GenerateAgentToken(settings.AgentId);
    }
}

public sealed class RequestAuthorizer(IdentityClient identityClient)
{
    public static ClaimsPrincipal CreatePrincipal(ValidatedToken validatedToken)
    {
        var claims = new List<Claim>
        {
            new("sub", validatedToken.Subject),
            new("type", validatedToken.Type)
        };

        if (!string.IsNullOrWhiteSpace(validatedToken.AgentId))
        {
            claims.Add(new Claim("agent_id", validatedToken.AgentId));
        }

        if (!string.IsNullOrWhiteSpace(validatedToken.Username))
        {
            claims.Add(new Claim("username", validatedToken.Username));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "A2ADemoToken");
        return new ClaimsPrincipal(identity);
    }

    public async Task<ValidatedToken?> ValidateBearerAsync(HttpContext context, string expectedType, CancellationToken ct)
    {
        var token = GetBearerToken(context.Request.Headers.Authorization);

        return string.IsNullOrWhiteSpace(token)
            ? null
            : await identityClient.ValidateTokenAsync(token, expectedType, ct);
    }

    public async Task<ValidatedToken?> ValidateBodyOrBearerAsync(HttpContext context, string expectedType, CancellationToken ct)
    {
        var token = GetBearerToken(context.Request.Headers.Authorization);
        token ??= await TryReadTokenFromBodyAsync(context);

        return string.IsNullOrWhiteSpace(token)
            ? null
            : await identityClient.ValidateTokenAsync(token, expectedType, ct);
    }

    private static string? GetBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        const string prefix = "Bearer ";
        return authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[prefix.Length..].Trim()
            : null;
    }

    private static async Task<string?> TryReadTokenFromBodyAsync(HttpContext context)
    {
        if (!context.Request.HasJsonContentType())
        {
            return null;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("metadata", out var metadata))
            {
                if (metadata.TryGetProperty("agent_jwt", out var agentJwt) && agentJwt.ValueKind == JsonValueKind.String)
                {
                    return agentJwt.GetString();
                }

                if (metadata.TryGetProperty("agentJwt", out var camelCaseAgentJwt) && camelCaseAgentJwt.ValueKind == JsonValueKind.String)
                {
                    return camelCaseAgentJwt.GetString();
                }
            }

            if (document.RootElement.TryGetProperty("agent_jwt", out var rootAgentJwt) && rootAgentJwt.ValueKind == JsonValueKind.String)
            {
                return rootAgentJwt.GetString();
            }
        }
        catch
        {
        }

        return null;
    }
}