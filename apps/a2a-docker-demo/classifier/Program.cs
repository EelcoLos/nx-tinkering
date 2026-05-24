using A2ADemo.Classifier;
using A2ADemo.Common;
using FastEndpoints;
using FastEndpoints.A2A;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create();

builder.Services.AddToolServiceInfrastructure(settings, DemoTelemetry.ActivitySourceName);
builder.Services.AddToolServiceA2A(settings, "A2A specialist that classifies incoming text.");

var app = builder.Build();

app.UseToolServiceAuthentication();
app.UseFastEndpoints();
app.UseA2A(rpcPattern: "/a2a", agentCardPattern: "/.well-known/agent-card.json");
app.Run();
