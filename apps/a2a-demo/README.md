# A2A Demo (Single-File .NET PoC)

This folder contains a local, keyless proof-of-concept for A2A v1 using stable A2A primitives:

- A2A
- A2A.AspNetCore

Scope:

- Standalone single-file apps under apps/a2a-demo
- Not wired into nx-tinker.slnx or Nx project graph
- Deterministic behavior only (no Azure/OpenAI keys)

## Apps

- specialist/specialist.cs
  - Hosts SpecialistAgent on http://localhost:5062
  - Exposes A2A endpoint at /a2a/specialist
- coordinator/coordinator.cs
  - Server mode: hosts CoordinatorAgent on http://localhost:5063
  - Client mode: calls either specialist or coordinator via A2A discovery

## Run

Open two terminals for server mode and optional third terminal for client mode.

1. Start specialist server

```powershell
dotnet run apps/a2a-demo/specialist/specialist.cs
```

2. Start coordinator server

```powershell
dotnet run apps/a2a-demo/coordinator/coordinator.cs
```

3. Client mode: direct call to specialist (proves client -> server)

```powershell
dotnet run apps/a2a-demo/coordinator/coordinator.cs -- --client --target specialist --message "hello specialist"
```

4. Client mode: call coordinator (proves coordinator server -> specialist server delegation)

```powershell
dotnet run apps/a2a-demo/coordinator/coordinator.cs -- --client --target coordinator --message "delegate this"
```

## Expected Output

- Specialist client call returns text like:
  - SPECIALIST::HELLO SPECIALIST
- Coordinator client call returns text like:
  - COORDINATOR::delegate this => SPECIALIST::DELEGATE THIS

## HTTP Smoke Tests

Use VS Code REST Client on:

- specialist/test.http
- coordinator/test.http

Each verifies server info and /.well-known/agent-card.json.

## Troubleshooting

- Port already in use
  - Change port constants in specialist.cs and coordinator.cs.
- Card resolves but send fails
  - Check that SupportedInterfaces URL points to the mapped endpoint.
- Connection refused from coordinator to specialist
  - Ensure specialist server is running before coordinator delegation tests.
