using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.OpenApi;
using FastEndpoints.OpenApi.Kiota;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FastEndpointsReactApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AuthTokenHandler>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidIssuer = AuthTokenSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = AuthTokenSettings.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = AuthTokenSettings.CreateSigningKey(builder.Configuration)
      };
    });

builder.Services.AddAuthorization()
                .AddFastEndpoints()
                .OpenApiDocument(p =>
{
  p.DocumentSettings = s =>
  {
    s.DocumentName = "v1";
    s.Title = "FastEndpoints React API";
    s.Version = "v1";
  };
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

await app.ExportOpenApiJsonAndExitAsync(
    "v1",
    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "api"),
    "specification.json");


app.Run();
