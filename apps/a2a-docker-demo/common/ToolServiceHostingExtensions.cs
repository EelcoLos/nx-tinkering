using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace A2ADemo.Common;

public static class ToolServiceHostingExtensions
{
    public static IServiceCollection AddToolServiceInfrastructure<TSettings>(
        this IServiceCollection services,
        TSettings settings,
        string activitySourceName)
        where TSettings : class, IToolServiceSettings
    {
        services.AddSingleton(settings);
        services.AddSingleton<IIdentityServiceSettings>(settings);
        services.AddHttpClient();
        services.AddSingleton(new JwtService(settings.JwtSecretKey));
        services.AddSingleton<IdentityClient>();
        services.AddSingleton<RequestAuthorizer>();
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

                context.Items["validated_token"] = validatedToken;
            }

            await next();
        });

        return app;
    }

    public static void RegisterToolServiceOnStarted<TRegistrar>(this WebApplication app)
        where TRegistrar : class, IServiceRegistrar
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                using var scope = app.Services.CreateScope();
                try
                {
                    await scope.ServiceProvider.GetRequiredService<TRegistrar>().RegisterAsync();
                }
                catch
                {
                }
            });
        });
    }
}