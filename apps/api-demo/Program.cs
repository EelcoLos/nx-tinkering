using FastEndpoints;
using FastEndpoints.OpenApi;
using FastEndpoints.OpenApi.Kiota;
using Kiota.Builder;
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
  o.ShortSchemaNames = true;
  o.DocumentSettings = s =>
  {
    s.DocumentName = "v1";
    s.Title = "My API";
    s.Version = "v1";
  };
});


var app = bld.Build();
app.UseFastEndpoints();
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
  app.MapScalarApiReference(o => o.AddDocuments("v1"));
}

var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "libs", "swagger", "src", "lib");

// Ensure the directory exists
if (!Directory.Exists(outputPath))
{
  Directory.CreateDirectory(outputPath);
}
await app.GenerateApiClientsAndExitAsync(
    c =>
    {
      c.OpenApiDocumentName = "v1";
      c.Language = GenerationLanguage.TypeScript;
      c.OutputPath = outputPath;
      c.ClientNamespaceName = "MyCompanyName";
      c.ClientClassName = "MyTsClient";

    });
app.Run();
