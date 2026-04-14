using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FastEndpointsReactApi;

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
        IssuerSigningKey = AuthTokenSettings.SigningKey
      };
    });

builder.Services.AddAuthorization()
                .AddFastEndpoints()
                .SwaggerDocument(p =>
{
  p.ShortSchemaNames = true;
  p.MaxEndpointVersion = 1;

  p.NewtonsoftSettings = s =>
  {
    s.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
    {
      NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
    };
  };
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
  app.UseSwaggerGen();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();


app.Run();
