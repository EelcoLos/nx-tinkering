using System.Text;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Jwt:Issuer/Audience live in appsettings.json; Jwt:SigningKey comes from user-secrets (dev)
// or the Jwt__SigningKey environment variable. Both callbacks below run lazily, so the
// OpenAPI export works without a signing key.
var jwt = builder.Configuration.GetSection("Jwt");
string SigningKey() => jwt["SigningKey"] switch
{
  null or "" => throw new InvalidOperationException(
    "Jwt:SigningKey is not configured. Run `dotnet user-secrets set Jwt:SigningKey <32+ byte secret>` in apps/fastendpoints-react-api."),
  var key when Encoding.UTF8.GetByteCount(key) < 32 => throw new InvalidOperationException(
    $"Jwt:SigningKey must be at least 32 bytes (256 bits) in UTF-8 for HMAC-SHA256; the configured key is {Encoding.UTF8.GetByteCount(key)} bytes."),
  var key => key,
};

builder.Services
  .Configure<JwtCreationOptions>(o =>
  {
    o.SigningKey = SigningKey();
    o.Issuer = jwt["Issuer"];
    o.Audience = jwt["Audience"];
  })
  .AddAuthenticationJwtBearer(
    s => s.SigningKey = SigningKey(),
    b =>
    {
      b.TokenValidationParameters.ValidIssuer = jwt["Issuer"];
      b.TokenValidationParameters.ValidAudience = jwt["Audience"];
    })
  .AddAuthorization()
  .AddFastEndpoints()
  .OpenApiDocument(p =>
  {
    p.DocumentName = "v1";
    p.Title = "FastEndpoints React API";
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

// `nx run fastendpoints-react-api:openapi` passes `--export-openapi-docs true`, which writes
// wwwroot/api/v1.json (OpenApiExportPath in the csproj) and exits.
await app.ExportOpenApiArtifactsAndExitAsync("v1");

app.Run();
