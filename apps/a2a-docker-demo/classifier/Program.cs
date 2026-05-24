using A2ADemo.Classifier;
using A2ADemo.Common;
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create();

builder.Services.AddToolServiceInfrastructure(settings, DemoTelemetry.ActivitySourceName);
builder.Services.AddSingleton<ServiceRegistrar>();

var app = builder.Build();

app.UseToolServiceAuthentication();
app.UseFastEndpoints();
app.RegisterToolServiceOnStarted<ServiceRegistrar>();
app.Run();
