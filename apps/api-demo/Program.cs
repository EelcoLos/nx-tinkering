using FastEndpoints;
using FastEndpoints.Swagger; //add this
using FastEndpoints.ClientGen.Kiota;
using Kiota.Builder;

var bld = WebApplication.CreateBuilder(args);
bld.Services.AddOutputCache(options =>
{
  options.AddBasePolicy(builder => builder.Cache());
});
bld.Services
   .AddFastEndpoints()
   .SwaggerDocument(o =>
{
  o.DocumentSettings = s =>
  {
    s.Title = "My API";
    s.Version = "v1";
  };
});


var app = bld.Build();
app.UseFastEndpoints()
   .UseSwaggerGen(); //add this

var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "libs", "swagger", "src", "lib");

// Ensure the directory exists
if (!Directory.Exists(outputPath))
{
  Directory.CreateDirectory(outputPath);
}
await app.GenerateApiClientsAndExitAsync(
    c =>
    {
      c.SwaggerDocumentName = "v1";
      c.Language = GenerationLanguage.TypeScript;
      c.OutputPath = outputPath;
      c.ClientNamespaceName = "MyCompanyName";
      c.ClientClassName = "MyTsClient";

    });
app.Run();
