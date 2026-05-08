#:sdk Microsoft.NET.Sdk.Web
#:package A2A@1.*-*
#:package A2A.AspNetCore@1.*-*
#:property ManagePackageVersionsCentrally=false

using A2A;
using A2A.AspNetCore;
using System.Linq;
using System.Text.Json;

const string HostUrl = "http://localhost:5062";
const string EndpointPath = "/a2a/specialist";

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(HostUrl);
builder.Services.AddA2AAgent<SpecialistAgent>(SpecialistAgent.GetAgentCard($"{HostUrl}{EndpointPath}"));

var app = builder.Build();

app.MapGet("/", () => Results.Json(new
{
    name = "specialist-agent",
    endpoint = $"{HostUrl}{EndpointPath}",
    card = $"{HostUrl}/.well-known/agent-card.json"
}));

app.MapGet("/.well-known/agent-card.json", (AgentCard card)
    => Results.Text(JsonSerializer.Serialize(card, A2AJsonUtilities.DefaultOptions), "application/json"));

app.MapA2A(EndpointPath);

app.Run();

file sealed class SpecialistAgent : IAgentHandler
{
    public static AgentCard GetAgentCard(string agentUrl) =>
        new()
        {
            Name = "SpecialistAgent",
            Description = "Returns deterministic uppercase responses for local A2A demos.",
            Version = "1.0.0",
            SupportedInterfaces =
            [
                new AgentInterface
                {
                    Url = agentUrl,
                    ProtocolBinding = "JSONRPC",
                    ProtocolVersion = "1.0"
                }
            ],
            DefaultInputModes = ["text/plain"],
            DefaultOutputModes = ["text/plain"],
            Capabilities = new AgentCapabilities { Streaming = false },
            Skills =
            [
                new AgentSkill
                {
                    Id = "specialist-uppercase",
                    Name = "Specialist Uppercase",
                    Description = "Uppercases incoming text and adds a marker.",
                    Tags = ["demo", "deterministic", "uppercase"]
                }
            ]
        };

    public Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
    {
        var incoming = context.Message.Parts?.FirstOrDefault()?.Text ?? string.Empty;
        var transformed = incoming.ToUpperInvariant();
        var responder = new MessageResponder(eventQueue, context.ContextId);

        return responder.ReplyAsync($"SPECIALIST::{transformed}", cancellationToken).AsTask();
    }

    public Task CancelAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
    {
        var updater = new TaskUpdater(eventQueue, context.TaskId, context.ContextId);
        return updater.CancelAsync(cancellationToken).AsTask();
    }
}
