using A2ADemo.Common;
using A2ADemo.Identity;
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);
var settings = AuthSettings.Create();

if (!settings.OidcEnabled && (string.IsNullOrWhiteSpace(settings.JwtSecretKey) || settings.JwtSecretKey.Length < 32))
{
    throw new InvalidOperationException("JWT_SECRET_KEY environment variable must be set and at least 32 characters long");
}

builder.Services.AddSingleton(settings);
builder.Services.AddHttpClient();
builder.Services.AddSingleton(new A2ADemo.Identity.JwtService(settings.JwtSecretKey));
builder.Services.AddSingleton<OidcAuthClient>();
builder.Services.AddSingleton<UserDatabase>();
builder.Services.AddFastEndpoints();
builder.Services.AddServiceTelemetry(
    settings.OtelEnabled,
    settings.ServiceName,
    settings.OtelServiceNamespace,
    settings.OtelExporterEndpoint,
    settings.ServiceName);

var app = builder.Build();

app.Services.GetRequiredService<UserDatabase>().SeedDemoUsers();
app.UseFastEndpoints();
app.Run();