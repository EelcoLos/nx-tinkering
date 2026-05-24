using A2ADemo.Assessor;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.Services.AddConfiguredToolServiceSettings<ServiceSettings>(ServiceSettings.Configure);

builder.Services.AddToolServiceInfrastructure(settings, DemoTelemetry.ActivitySourceName);
builder.Services.AddToolServiceA2A(settings, "A2A specialist that turns classifications into priorities.");

var app = builder.Build();

app.UseToolServiceAuthentication();
app.UseFastEndpoints();
app.UseA2A(rpcPattern: "/a2a", agentCardPattern: "/.well-known/agent-card.json");
app.Run();