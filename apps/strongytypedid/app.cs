#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints
#:package FastEndpoints.OpenApi
#:package Scalar.AspNetCore
#:package Vogen
#:property PublishAot=false

using FastEndpoints;
using FastEndpoints.OpenApi;
using Vogen;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder();
builder.Services.AddFastEndpoints().OpenApiDocument(o =>
{
    o.DocumentName = "v1";
    o.Title = "Strongly Typed Id Demo";
    o.Version = "v1";
});

var app = builder.Build();
app.UseFastEndpoints();
app.MapOpenApi();
app.MapScalarApiReference(o => o.AddDocuments("v1"));
app.Run();

// Vogen generates System.Text.Json + TypeConverter conversions and TryParse,
// so UserId works in JSON bodies and route params.
[ValueObject<Guid>(customizations: Customizations.AddFactoryMethodForGuids)]
public readonly partial struct UserId;

public record MyRequest(string FirstName, string LastName, int Age);
public record MyResponse(UserId Id);

public class MyEndpoint : Endpoint<MyRequest, MyResponse>
{
    public override void Configure()
    {
        Post("/api/user/create");
        AllowAnonymous();
    }

    public override Task HandleAsync(MyRequest req, CancellationToken ct) =>
        Send.OkAsync(new MyResponse(UserId.FromNewGuid()), cancellation: ct);
}

public record GetUserRequest(UserId Id);

public class GetUserEndpoint : Endpoint<GetUserRequest, MyResponse>
{
    public override void Configure()
    {
        Get("/api/user/{id}");
        AllowAnonymous();
    }

    public override Task HandleAsync(GetUserRequest req, CancellationToken ct) =>
        Send.OkAsync(new MyResponse(req.Id), cancellation: ct);
}
