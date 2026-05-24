using FastEndpoints;
using FastEndpoints.A2A;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace A2ADemo.Common;

public static class ToolServiceHostingExtensions
{
    public static IServiceCollection AddToolServiceInfrastructure<TSettings>(
        this IServiceCollection services,
        TSettings settings,
        string activitySourceName)
        where TSettings : class, IToolServiceSettings
    {
        services.AddHttpClient();
        services.AddSingleton(sp => new JwtService(sp.GetRequiredService<IOptions<TSettings>>().Value.JwtSecretKey));
        services.AddSingleton<IdentityClient>();
        services.AddSingleton<RequestAuthorizer>();
        services.AddAuthorization();
        services.AddFastEndpoints();
        services.AddServiceTelemetry(
            settings.OtelEnabled,
            settings.ServiceName,
            settings.OtelServiceNamespace,
            settings.OtelExporterEndpoint,
            activitySourceName);

        return services;
    }

    public static WebApplication UseToolServiceAuthentication(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await next();
                return;
            }

            if (context.Request.Path.StartsWithSegments("/skills"))
            {
                var authorizer = context.RequestServices.GetRequiredService<RequestAuthorizer>();
                var validatedToken = await authorizer.ValidateBodyOrBearerAsync(context, "agent", context.RequestAborted);
                if (validatedToken is null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" }, cancellationToken: context.RequestAborted);
                    return;
                }

                context.User = RequestAuthorizer.CreatePrincipal(validatedToken);
                context.Items["validated_token"] = validatedToken;
            }

            if (context.Request.Path.StartsWithSegments("/a2a")
                || context.Request.Path.StartsWithSegments("/.well-known"))
            {
                var authorizer = context.RequestServices.GetRequiredService<RequestAuthorizer>();
                var validatedToken = await authorizer.ValidateBearerAsync(context, "agent", context.RequestAborted);
                if (validatedToken is null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" }, cancellationToken: context.RequestAborted);
                    return;
                }

                context.User = RequestAuthorizer.CreatePrincipal(validatedToken);
                context.Items["validated_token"] = validatedToken;
            }

            await next();
        });

        app.UseAuthorization();

        return app;
    }

    public static IServiceCollection AddToolServiceA2A<TSettings>(
        this IServiceCollection services,
        TSettings settings,
        string description)
        where TSettings : class, IToolServiceSettings
    {
        services.AddA2A(options =>
        {
            options.AgentName = settings.ServiceName;
            options.Description = description;
            options.Version = "1.0.0";
            options.Url = $"{settings.ServiceBaseUrl.TrimEnd('/')}/a2a";
            options.SkillVisibilityFilter = (_, user, _) =>
                user.Identity?.IsAuthenticated == true &&
                user.HasClaim("type", "agent");
        });

        return services;
    }
}