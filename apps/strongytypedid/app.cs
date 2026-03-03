#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@8.0.1
#:package FastEndpoints.Swagger@8.0.1
#:package StronglyTypedId@1.0.0-beta08
#:property ManagePackageVersionsCentrally=false
#:property PublishAot=false

using FastEndpoints;
using FastEndpoints.Swagger;
using StronglyTypedIds;

var builder = WebApplication.CreateBuilder();
builder.Services.AddFastEndpoints().SwaggerDocument();

var app = builder.Build();
app.UseFastEndpoints().UseSwaggerGen();
app.Run();

[StronglyTypedId]
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
        Send.OkAsync(new MyResponse(UserId.New()), cancellation: ct);
}