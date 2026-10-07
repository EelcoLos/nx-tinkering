using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using MsGraphDemo;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1. Validate Entra ID access tokens issued for THIS API (AzureAd section),
//    then exchange them on-behalf-of the caller for Microsoft Graph tokens.
//    The client secret comes from user-secrets / environment, never appsettings.
// ---------------------------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("DownstreamApis:MicrosoftGraph"))
    .AddInMemoryTokenCaches();

// ---------------------------------------------------------------------------
// 2. Policies check the API's own scope / app role via Microsoft.Identity.Web's
//    RequireScope / RequireScopeOrAppPermission. FastEndpoints uses them through
//    Policies(...), identical to [Authorize(Policy = "...")] on a controller.
// ---------------------------------------------------------------------------
builder.Services
    .AddAuthorization(ApiPolicies.Configure)
    .AddFastEndpoints()
    .OpenApiDocument(o =>
    {
      o.DocumentName = "v1";
      o.Title = "MS Graph Demo API";
      o.Version = "v1";
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
  app.MapScalarApiReference(o => o.AddDocuments("v1"));
}

app.Run();
