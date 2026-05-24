using A2ADemo.Identity;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.Services.AddConfiguredSettings<AuthSettings>(AuthSettings.Configure);

if (!settings.OidcEnabled && (string.IsNullOrWhiteSpace(settings.JwtSecretKey) || settings.JwtSecretKey.Length < 32))
{
    throw new InvalidOperationException("JWT_SECRET_KEY environment variable must be set and at least 32 characters long");
}

builder.Services.AddHttpClient();
builder.Services.AddSingleton(sp => new A2ADemo.Identity.JwtService(sp.GetRequiredService<IOptions<AuthSettings>>().Value.JwtSecretKey));
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