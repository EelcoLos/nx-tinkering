#:sdk Microsoft.NET.Sdk.Web
#:package A2A@1.*-*
#:package A2A.AspNetCore@1.*-*
#:property ManagePackageVersionsCentrally=false

using A2A;
using A2A.AspNetCore;
using System.Linq;
using System.Text.Json;

const string CoordinatorHostUrl = "http://localhost:5063";
const string CoordinatorEndpointPath = "/a2a/coordinator";
const string SpecialistBaseUrl = "http://localhost:5062/";

if (args.Contains("--client", StringComparer.OrdinalIgnoreCase))
{
    await RunClientModeAsync(args);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(CoordinatorHostUrl);
builder.Services.AddA2AAgent<CoordinatorAgent>(CoordinatorAgent.GetAgentCard($"{CoordinatorHostUrl}{CoordinatorEndpointPath}"));

var app = builder.Build();

app.MapGet("/", () => Results.Json(new
{
    name = "coordinator-agent",
    endpoint = $"{CoordinatorHostUrl}{CoordinatorEndpointPath}",
    card = $"{CoordinatorHostUrl}/.well-known/agent-card.json",
    specialist = SpecialistBaseUrl
}));

app.MapGet("/.well-known/agent-card.json", (AgentCard card)
    => Results.Text(JsonSerializer.Serialize(card, A2AJsonUtilities.DefaultOptions), "application/json"));

app.MapA2A(CoordinatorEndpointPath);

app.Run();

static async Task RunClientModeAsync(string[] args)
{
    var target = CoordinatorHelpers.GetOption(args, "--target")?.ToLowerInvariant() ?? "specialist";
    var message = CoordinatorHelpers.GetOption(args, "--message") ?? "hello from local A2A client mode";

    var targetBaseUrl = target == "coordinator"
        ? CoordinatorHostUrl
        : SpecialistBaseUrl;

    var responseText = await CoordinatorHelpers.CallRemoteAgentAsync(targetBaseUrl, message);

    Console.WriteLine($"Target: {target}");
    Console.WriteLine($"Input: {message}");
    Console.WriteLine($"Response: {responseText}");
}

file sealed class CoordinatorAgent : IAgentHandler
{
    private const string SpecialistUrl = "http://localhost:5062/";

    public static AgentCard GetAgentCard(string agentUrl) =>
        new()
        {
            Name = "CoordinatorAgent",
            Description = "Delegates incoming requests to SpecialistAgent over A2A and returns composed output.",
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
                    Id = "coordinator-delegate",
                    Name = "Coordinator Delegate",
                    Description = "Calls the specialist over A2A and composes the final response.",
                    Tags = ["demo", "delegation", "a2a"]
                }
            ]
        };

    public async Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
    {
        var incoming = context.Message.Parts?.FirstOrDefault()?.Text ?? string.Empty;
        var specialistText = await CoordinatorHelpers.CallRemoteAgentAsync(SpecialistUrl, incoming);
        var responder = new MessageResponder(eventQueue, context.ContextId);

        await responder.ReplyAsync($"COORDINATOR::{incoming} => {specialistText}", cancellationToken);
    }

    public Task CancelAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
    {
        var updater = new TaskUpdater(eventQueue, context.TaskId, context.ContextId);
        return updater.CancelAsync(cancellationToken).AsTask();
    }
}

file static class CoordinatorHelpers
{
    public static string? GetOption(string[] args, string option)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], option, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    public static async Task<string> CallRemoteAgentAsync(string baseUrl, string input)
    {
        try
        {
            var resolver = new A2ACardResolver(new Uri(baseUrl));
            var card = await resolver.GetAgentCardAsync();

            var interfaceUrl = card.SupportedInterfaces?.FirstOrDefault()?.Url;
            if (string.IsNullOrWhiteSpace(interfaceUrl))
            {
                return "ERROR::No supported interface URL advertised by target card.";
            }

            var client = new A2AClient(new Uri(interfaceUrl));
            var response = await client.SendMessageAsync(new SendMessageRequest
            {
                Message = new Message
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    Role = Role.User,
                    Parts = [Part.FromText(input)]
                }
            });

            return response.PayloadCase switch
            {
                SendMessageResponseCase.Message => response.Message?.Parts?.FirstOrDefault()?.Text ?? "",
                SendMessageResponseCase.Task => $"TASK::{response.Task?.Id}",
                _ => "ERROR::Unexpected response payload."
            };
        }
        catch (Exception ex)
        {
            return $"ERROR::{ex.Message}";
        }
    }
}
