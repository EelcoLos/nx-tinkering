#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@8.*-*
#:package FastEndpoints.A2A@1.0.0-beta.1
#:package A2A@1.*-*
#:property ManagePackageVersionsCentrally=false

using A2A;
using FastEndpoints;
using FastEndpoints.A2A;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

const string CoordinatorHostUrl = "http://localhost:5163";
const string SpecialistBaseUrl = "http://localhost:5162/";

if (args.Contains("--client", StringComparer.OrdinalIgnoreCase))
{
    await RunClientModeAsync(args);
    return;
}

var bld = WebApplication.CreateBuilder(args);
bld.WebHost.UseUrls(CoordinatorHostUrl);
bld.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver()));

bld.Services
   .AddFastEndpoints()
   .AddA2A(o =>
   {
       o.AgentName = "fe-coordinator-agent";
       o.Description = "FastEndpoints coordinator agent for local A2A demos.";
       o.Version = "1.0.0";
       o.SkillVisibilityFilter = (_, _, _) => true;
   });

var app = bld.Build();

app.UseFastEndpoints()
    .UseA2A(rpcPattern: "/a2a", agentCardPattern: "/.well-known/agent-card.json");

app.Run();

static async Task RunClientModeAsync(string[] args)
{
    var target = FastA2AHelpers.GetOption(args, "--target")?.ToLowerInvariant() ?? "specialist";
    var input = FastA2AHelpers.GetOption(args, "--message") ?? "hello from FE A2A client mode";

    var baseUrl = target == "coordinator" ? CoordinatorHostUrl : SpecialistBaseUrl;
    var skillId = target == "coordinator" ? "coordinator_delegate" : "specialist_uppercase";

    var response = await FastA2AHelpers.CallSkillAsync(baseUrl, skillId, input);

    Console.WriteLine($"Target: {target}");
    Console.WriteLine($"Input: {input}");
    Console.WriteLine($"Response: {response}");
}

sealed class CoordinatorEndpoint : Endpoint<CoordinatorRequest, CoordinatorResponse>
{
    public override void Configure()
    {
        Post("/skills/coordinator-delegate");
        AllowAnonymous();

        this.A2ASkill(
            id: "coordinator_delegate",
            tags: ["demo", "delegation", "a2a"],
            configure: skill =>
            {
                skill.Name = "Coordinator Delegate";
                skill.Description = "Delegates Input to specialist_uppercase and composes a deterministic response.";
                skill.Examples = ["Delegate this message to specialist."];
                skill.InputModes = ["application/json"];
                skill.OutputModes = ["application/json"];
            });
    }

    public override async Task HandleAsync(CoordinatorRequest req, CancellationToken ct)
    {
        var input = req.Input ?? string.Empty;
        var specialist = await FastA2AHelpers.CallSkillAsync("http://localhost:5162/", "specialist_uppercase", input);

        await Send.OkAsync(new CoordinatorResponse($"COORDINATOR::{input} => {specialist}"), ct);
    }
}

sealed record CoordinatorRequest(string? Input);
sealed record CoordinatorResponse(string Result);

file static class FastA2AHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

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

    public static async Task<string> CallSkillAsync(string baseUrl, string skillId, string input)
    {
        try
        {
            var resolver = new A2ACardResolver(new Uri(baseUrl));
            var card = await resolver.GetAgentCardAsync();
            var rpcUrl = card.SupportedInterfaces?.FirstOrDefault()?.Url;

            if (string.IsNullOrWhiteSpace(rpcUrl))
            {
                return "ERROR::No supported interface URL in agent card.";
            }

            var payload = JsonSerializer.SerializeToElement(new SkillInput(input), JsonOptions);
            var metadata = new Dictionary<string, JsonElement>
            {
                ["skill"] = JsonSerializer.SerializeToElement(skillId, JsonOptions)
            };

            var client = new A2AClient(new Uri(rpcUrl));
            var response = await client.SendMessageAsync(new SendMessageRequest
            {
                Message = new Message
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    Role = Role.User,
                    Parts = [Part.FromData(payload)]
                },
                Metadata = metadata
            });

            var part = response.Message?.Parts?.FirstOrDefault();
            if (part?.Data is { } data)
            {
                if (TryReadResult(data, out var result))
                {
                    return result;
                }

                return data.GetRawText();
            }

            if (!string.IsNullOrWhiteSpace(part?.Text))
            {
                return part.Text;
            }

            return response.PayloadCase switch
            {
                SendMessageResponseCase.Task => $"TASK::{response.Task?.Id}",
                SendMessageResponseCase.Message => "ERROR::Message response had no readable part.",
                _ => "ERROR::Unexpected response payload."
            };
        }
        catch (Exception ex)
        {
            return $"ERROR::{ex.Message}";
        }
    }

    private static bool TryReadResult(JsonElement data, out string result)
    {
        result = string.Empty;

        if (data.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (data.TryGetProperty("Result", out var upper) && upper.ValueKind == JsonValueKind.String)
        {
            result = upper.GetString() ?? string.Empty;
            return true;
        }

        if (data.TryGetProperty("result", out var lower) && lower.ValueKind == JsonValueKind.String)
        {
            result = lower.GetString() ?? string.Empty;
            return true;
        }

        return false;
    }

    private sealed record SkillInput(string Input);
}
