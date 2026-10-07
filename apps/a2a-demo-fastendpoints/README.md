# A2A Demo FastEndpoints (Single-File .NET PoC)

This folder mirrors the design of apps/a2a-demo, but uses FastEndpoints.A2A skill opt-in and dispatcher wiring.
It targets the A2A v1.0 spec; FastEndpoints.A2A (1.0.0-beta.5) and the A2A .NET client SDK (1.0.0-preview2) are prerelease.
Versions come from the root Directory.Packages.props.

Scope:

- Standalone single-file apps under apps/a2a-demo-fastendpoints
- Not wired into nx-tinker.slnx or Nx project graph
- Local deterministic behavior (no cloud keys)

## Apps

- specialist/specialist.cs
  - Hosts FE specialist agent on http://localhost:5162
  - A2A routes: /a2a and /.well-known/agent-card.json
  - Skill id: specialist_uppercase
- coordinator/coordinator.cs
  - Server mode: hosts FE coordinator agent on http://localhost:5163
  - Client mode: calls specialist or coordinator via A2A discovery
  - Skill id: coordinator_delegate

## Run

Open two terminals for server mode and optional third for client mode.

1. Start specialist server

```powershell
dotnet run apps/a2a-demo-fastendpoints/specialist/specialist.cs
```

2. Start coordinator server

```powershell
dotnet run apps/a2a-demo-fastendpoints/coordinator/coordinator.cs
```

3. Client mode: direct call to specialist

```powershell
dotnet run apps/a2a-demo-fastendpoints/coordinator/coordinator.cs -- --client --target specialist --message "hello specialist"
```

4. Client mode: call coordinator (delegates to specialist)

```powershell
dotnet run apps/a2a-demo-fastendpoints/coordinator/coordinator.cs -- --client --target coordinator --message "delegate this"
```

## Expected Output

- Specialist call:
  - SPECIALIST::HELLO SPECIALIST
- Coordinator call:
  - COORDINATOR::delegate this => SPECIALIST::DELEGATE THIS

## HTTP Smoke Tests

Use VS Code REST Client on:

- specialist/test.http
- coordinator/test.http

## Notes

- This demo uses FastEndpoints A2A opt-in metadata with this.A2ASkill(...) in each endpoint Configure() method.
- FastEndpoints.A2A 1.0.0-beta.5 still only dispatches A2A SendMessage over the JSON-RPC binding (no streaming, tasks or HTTP+JSON/gRPC bindings); this demo stays within that path.
