using Azure.Identity;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using Microsoft.AspNetCore.Authorization;
using DotnetFeMsGraph.Services;
using DotnetFeMsGraph.Auth;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var configuration = builder.Configuration;

// Configure Microsoft Identity Platform
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        configuration.Bind("AzureAd", options);
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                // Optionally extract custom claims or perform additional validation
            }
        };
    }, options => { configuration.Bind("AzureAd", options); })
    .EnableTokenAcquisitionToCallDownstreamApi(options => 
    {
        configuration.Bind("AzureAd", options);
    })
    .AddMicrosoftGraph(configuration.GetSection("MicrosoftGraph"))
    .AddInMemoryTokenCaches();

// Register Graph services
builder.Services.AddScoped<IUserAuthGraphService, UserAuthGraphService>();
builder.Services.AddScoped<IAppAuthGraphService, AppAuthGraphService>();

// Configure authorization with dynamic permission policies
builder.Services.AddAuthorization(options =>
{
    // Configure known Graph permission policies
    GraphPermissions.AllPermissions.ToList().ForEach(permission =>
        options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission))));
})
.AddFastEndpoints()
.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Microsoft Graph FastEndpoints API";
        s.Version = "v1";
        s.AddAuth("oauth2", new()
        {
            Type = NSwag.OpenApiSecuritySchemeType.OAuth2,
            Flows = new()
            {
                Implicit = new()
                {
                    AuthorizationUrl = configuration["AzureAd:AuthorizationUrl"] ?? "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
                    TokenUrl = configuration["AzureAd:TokenUrl"] ?? "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                    Scopes = GraphPermissions.AllPermissions.ToDictionary(
                        scope => scope,
                        scope => $"Grants {scope} permission"
                    )
                }
            }
        });
    };
});

// Add handler for permission requirements
builder.Services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwaggerGen();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(config => 
{
    config.Endpoints.RoutePrefix = "api";
    config.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

app.Run();