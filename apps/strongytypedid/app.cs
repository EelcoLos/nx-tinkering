#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@8.2.0-beta.13
#:package FastEndpoints.OpenApi@8.2.0-beta.13
#:package Scalar.AspNetCore@2.14.1
#:package StronglyTypedId@1.0.0-beta08
#:property ManagePackageVersionsCentrally=false
#:property PublishAot=false

using FastEndpoints;
using FastEndpoints.OpenApi;
using StronglyTypedIds;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder();
builder.Services.AddFastEndpoints().OpenApiDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.DocumentName = "v1";
        s.Title = "Strongly Typed Id Demo";
        s.Version = "v1";
    };
});

var app = builder.Build();
app.UseFastEndpoints();
app.MapOpenApi();
app.MapScalarApiReference(o => o.AddDocuments("v1"));
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