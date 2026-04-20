using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.OpenApi;
using FastEndpoints.OpenApi.Kiota;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotnetFeAuth;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FETokenHandler>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidIssuer = "https://localhost:5001",
        ValidateAudience = true,
        ValidAudience = "https://localhost:5001",
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-256-bit-secret-your-256-bit-secret"))
      };
    });

builder.Services.AddAuthorization()
                .AddFastEndpoints()
                .OpenApiDocument(p =>
{
  p.DocumentSettings = s =>
  {
    s.DocumentName = "v1";
    s.Title = "Dotnet FE Auth";
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
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
  app.MapScalarApiReference(o => o.AddDocuments("v1"));
}

await app.ExportOpenApiJsonAndExitAsync(
    "v1",
    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "api"),
    "specification.json");


app.Run();
