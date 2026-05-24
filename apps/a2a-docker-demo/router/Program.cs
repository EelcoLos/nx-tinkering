using A2ADemo.Common;
using A2ADemo.Router;
using FastEndpoints;
using FastEndpoints.A2A;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.Services.AddConfiguredToolServiceSettings<ServiceSettings>(ServiceSettings.Configure);

builder.Services.AddToolServiceInfrastructure(settings, DemoTelemetry.ActivitySourceName);
builder.Services.AddToolServiceA2A(settings, "A2A specialist that routes work based on priority.");

var app = builder.Build();

app.UseToolServiceAuthentication();
app.UseFastEndpoints();
app.UseA2A(rpcPattern: "/a2a", agentCardPattern: "/.well-known/agent-card.json");
app.Run();