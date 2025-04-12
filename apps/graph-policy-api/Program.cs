using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to reduce excessive debug output
builder.Logging.AddFilter("Microsoft.Identity", Microsoft.Extensions.Logging.LogLevel.Warning);

// Simplify Microsoft Identity integration to avoid recursive validation
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

// Add Microsoft Graph separately without recursive configuration
builder.Services.AddMicrosoftGraphClient(builder.Configuration.GetSection("AzureAd"));

// Add in-memory token caching
builder.Services.AddInMemoryTokenCaches();

// Add FastEndpoints
builder.Services
    .AddFastEndpoints()
    .AddAuthorization(options =>
    {
      // Define policies for Graph API scopes
      options.AddPolicy("GraphUser.Read", policy =>
          policy.RequireScope("User.Read"));

      options.AddPolicy("GraphUser.ReadWrite", policy =>
          policy.RequireScope("User.ReadWrite"));

      options.AddPolicy("GraphDirectory.Read", policy =>
          policy.RequireScope("Directory.Read.All"));
    })
    .SwaggerDocument();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwaggerGen();
}

// Enable authentication and authorization
app.UseAuthentication()
   .UseAuthorization();

// Enable FastEndpoints
app.UseFastEndpoints(c =>
{
  c.Endpoints.RoutePrefix = "api";
  c.Serializer.Options.PropertyNamingPolicy = null;
});

app.Run();

// Extension method to add Microsoft Graph client without circular dependencies
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMicrosoftGraphClient(
      this IServiceCollection services,
      IConfigurationSection graphSection)
  {
    services.Configure<ConfidentialClientApplicationOptions>(graphSection);

    services.AddSingleton<IConfidentialClientApplication>(sp =>
    {
      var config = sp.GetRequiredService<IOptionsMonitor<ConfidentialClientApplicationOptions>>().CurrentValue;

      return ConfidentialClientApplicationBuilder
              .Create(config.ClientId)
              .WithClientSecret(config.ClientSecret)
              .WithAuthority(new Uri($"{config.Instance}{config.TenantId}"))
              .Build();
    });

    services.AddSingleton<GraphServiceClient>(sp =>
    {
      var app = sp.GetRequiredService<IConfidentialClientApplication>();
      var scopes = new[] { "https://graph.microsoft.com/.default" };

      return new GraphServiceClient(
              new ClientCredentialProvider(app, scopes));
    });

    return services;
  }
}

// Provider to avoid token acquisition loops
public class ClientCredentialProvider(
    IConfidentialClientApplication clientApplication,
    string[] scopes) : IAuthenticationProvider
{

  public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
  {
    var result = await clientApplication
            .AcquireTokenForClient(scopes)
            .ExecuteAsync();

    request.Headers.Add("Authorization", $"Bearer {result.AccessToken}");
  }
}
