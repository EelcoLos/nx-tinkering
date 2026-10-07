using FastEndpoints;
using FastEndpoints.OpenApi;
using Scalar.AspNetCore;

var bld = WebApplication.CreateBuilder(args);
bld.Services.AddOutputCache(options =>
{
  options.AddBasePolicy(builder => builder.Cache());
});
bld.Services
   .AddFastEndpoints()
   .OpenApiDocument(o =>
{
  o.DocumentName = "v1";
  o.Title = "My API";
  o.Version = "v1";
  o.ShortSchemaNames = true;
});


var app = bld.Build();
app.UseFastEndpoints();
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
  app.MapScalarApiReference(o => o.AddDocuments("v1"));
}

app.Run();
