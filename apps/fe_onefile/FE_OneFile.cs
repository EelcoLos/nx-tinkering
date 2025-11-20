#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@7.*-*

using FastEndpoints;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder();

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
builder.Services.AddFastEndpoints();

var app = builder.Build();
app.UseFastEndpoints();
app.Run();

public record MyRequest(string FirstName, string LastName, int Age);
public record MyResponse(string FullName, bool IsOver18);

public class MyEndpoint : Endpoint<MyRequest, MyResponse>
{
    public override void Configure()
    {
        Post("/api/user/create");
        AllowAnonymous();
    }

    public override Task HandleAsync(MyRequest req, CancellationToken ct) =>
        Send.OkAsync(new($"{req.FirstName} {req.LastName}", req.Age >= 18), cancellation: ct);
}

[JsonSerializable(typeof(MyRequest)), JsonSerializable(typeof(MyResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
