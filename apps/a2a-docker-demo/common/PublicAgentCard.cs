using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;

namespace A2ADemo.Common;

/// <summary>
/// Serves <c>/.well-known/agent-card.json</c> without requiring a token and declares the
/// agent bearer scheme on the card (A2A v1.0 <c>securitySchemes</c> / <c>securityRequirements</c>).
/// A valid agent token is still honoured, so authenticated agents also see the skills.
/// </summary>
public static class PublicAgentCard
{
  private const string SchemeName = "agentBearer";

  public static bool IsAgentCardRequest(HttpContext context) =>
      context.Request.Path.StartsWithSegments("/.well-known");

  public static async Task HandleAsync(HttpContext context, Func<Task> next)
  {
    var authorizer = context.RequestServices.GetRequiredService<RequestAuthorizer>();
    var validatedToken = await authorizer.ValidateBearerAsync(context, "agent", context.RequestAborted);
    if (validatedToken is not null)
    {
      context.User = RequestAuthorizer.CreatePrincipal(validatedToken);
      context.Items["validated_token"] = validatedToken;
    }

    var originalBody = context.Response.Body;
    using var buffer = new MemoryStream();
    context.Response.Body = buffer;
    try
    {
      await next();
    }
    finally
    {
      context.Response.Body = originalBody;
    }

    buffer.Position = 0;
    if (context.Response.StatusCode != StatusCodes.Status200OK || buffer.Length == 0 || JsonNode.Parse(buffer) is not JsonObject card)
    {
      buffer.Position = 0;
      await buffer.CopyToAsync(originalBody, context.RequestAborted);
      return;
    }

    card["securitySchemes"] = new JsonObject
    {
      [SchemeName] = new JsonObject
      {
        ["httpAuthSecurityScheme"] = new JsonObject
        {
          ["scheme"] = "Bearer",
          ["bearerFormat"] = "JWT",
          ["description"] = "Agent JWT issued by the identity service (GET /auth/agent/token)."
        }
      }
    };
    card["securityRequirements"] = new JsonArray(
        new JsonObject { ["schemes"] = new JsonObject { [SchemeName] = new JsonObject { ["list"] = new JsonArray() } } });

    context.Response.ContentLength = null;
    await context.Response.WriteAsync(card.ToJsonString(), context.RequestAborted);
  }
}
