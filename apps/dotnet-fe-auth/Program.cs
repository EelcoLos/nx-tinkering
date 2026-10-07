using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["SigningKey"];
if (string.IsNullOrEmpty(signingKey) && builder.IsNotExportMode())
{
  throw new InvalidOperationException(
      "Jwt:SigningKey is not configured. See apps/dotnet-fe-auth/README.md (dotnet user-secrets).");
}

builder.Services
    .AddAuthenticationJwtBearer(
        s => s.SigningKey = signingKey,
        o =>
        {
          o.TokenValidationParameters.ValidateIssuer = true;
          o.TokenValidationParameters.ValidIssuer = jwt["Issuer"];
          o.TokenValidationParameters.ValidateAudience = true;
          o.TokenValidationParameters.ValidAudience = jwt["Audience"];
        })
    .Configure<JwtCreationOptions>(o =>
    {
      o.SigningKey = signingKey!;
      o.Issuer = jwt["Issuer"];
      o.Audience = jwt["Audience"];
    });

builder.Services.AddAuthorization()
                .AddFastEndpoints()
                .OpenApiDocument(p =>
{
  p.DocumentName = "v1";
  p.Title = "Dotnet FE Auth";
  p.Version = "v1";
  p.ShortSchemaNames = true;
  p.MaxEndpointVersion = 1;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.MapScalarApiReference(o => o.AddDocuments("v1"));
}

await app.ExportOpenApiDocsAndExitAsync("v1");

app.Run();
