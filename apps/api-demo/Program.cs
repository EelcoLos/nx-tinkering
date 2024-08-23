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


public class MyRequest
{
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public int Age { get; set; }
}

public class MyResponse
{
  public string FullName { get; set; }
  public bool IsOver18 { get; set; }
}

public class MyEndpoint : Endpoint<MyRequest, MyResponse>
{
  public override void Configure()
  {
    Post("/api/user/create");
    AllowAnonymous();
  }

  public override async Task HandleAsync(MyRequest req, CancellationToken ct)
  {
    await SendAsync(new()
    {
      FullName = req.FirstName + " " + req.LastName,
      IsOver18 = req.Age > 18
    });
  }
}
