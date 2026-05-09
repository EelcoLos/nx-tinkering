#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@8.*-*
#:package FastEndpoints.A2A@1.0.0-beta.1
#:property ManagePackageVersionsCentrally=false

using FastEndpoints;
using FastEndpoints.A2A;
using System.Text.Json.Serialization.Metadata;

const string HostUrl = "http://localhost:5162";

var bld = WebApplication.CreateBuilder(args);
bld.WebHost.UseUrls(HostUrl);
bld.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver()));

bld.Services
   .AddFastEndpoints()
   .AddA2A(o =>
   {
       o.AgentName = "fe-specialist-agent";
       o.Description = "FastEndpoints specialist agent for local A2A demos.";
       o.Version = "1.0.0";
       o.SkillVisibilityFilter = (_, _, _) => true;
   });

var app = bld.Build();

app.UseFastEndpoints()
    .UseA2A(rpcPattern: "/a2a", agentCardPattern: "/.well-known/agent-card.json");

app.Run();

sealed class SpecialistEndpoint : Endpoint<SpecialistRequest, SpecialistResponse>
{
    public override void Configure()
    {
        Post("/skills/specialist-uppercase");
        AllowAnonymous();

        this.A2ASkill(
            id: "specialist_uppercase",
            tags: ["demo", "transform", "uppercase"],
            configure: skill =>
            {
                skill.Name = "Specialist Uppercase";
                skill.Description = "Uppercases the Input text and returns a deterministic marker.";
                skill.Examples = ["Transform hello to uppercase."];
                skill.InputModes = ["application/json"];
                skill.OutputModes = ["application/json"];
            });
    }

    public override Task HandleAsync(SpecialistRequest req, CancellationToken ct)
        => Send.OkAsync(new SpecialistResponse($"SPECIALIST::{(req.Input ?? string.Empty).ToUpperInvariant()}"), ct);
}

sealed record SpecialistRequest(string? Input);
sealed record SpecialistResponse(string Result);
