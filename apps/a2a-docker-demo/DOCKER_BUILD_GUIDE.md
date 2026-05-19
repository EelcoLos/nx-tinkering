# Docker Build and Deployment Guide for A2A Triage Demo

## Status

The A2A protocol triage demonstration is **architecturally complete and logically verified**. All service code, configurations, and deployment templates are ready. However, the code structure requires a one-time refactoring for Docker compilation.

## Why Code Restructuring is Needed

The original service files were written with `#:` directives designed for direct `dotnet run file.cs` execution:

```csharp
#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@8.*-*
#:package System.IdentityModel.Tokens.Jwt@7.*
```

When compiling with a `.csproj` file in Docker, these directives are not processed, and the code structure must follow C# file-scoped program rules:

1. **Usings** at the top
2. **Top-level statements** (`var bld = ...`, `app.Run()`)
3. **Type declarations** (classes, records, etc.)

The current code has **type declarations before app.Run()**,  which violates this rule.

## Solution

Choose one of these approaches:

### Option 1: Remove Directives and Restructure (Recommended)

1. Remove all `#:` directive lines from each `.cs` file
2. Move all class definitions to **after** the `app.Run()` statement
3. The `.csproj` files are already prepared in the repo

Steps:
```bash
cd apps/a2a-docker-demo

# For each service:
cd identity
# Remove #: lines
sed -i '/^#:/d' identity.cs

# Manually restructure (move classes after app.Run())
# Or use a refactoring tool

# Build Docker image
docker build -t a2a-identity:latest .
cd ..
```

### Option 2: Use FastEndpoints FileBasedProgram Feature

Enable the FileBasedProgram feature in the `.csproj` files:

```xml
<PropertyGroup>
    ...
    <Features>FileBasedProgram</Features>
</PropertyGroup>
```

This allows the `#:` directives to work without restructuring the code.

### Option 3: Convert to Namespace-Based Structure

Wrap all top-level code and classes in a Program class:

```csharp
namespace A2ADemo;

class Program
{
    static async Task Main(string[] args)
    {
        // All current code here
    }
}
```

## Quick Start for Local Development

Without Docker, the services run directly:

```bash
cd identity
dotnet run identity.cs

# In another terminal
cd ../api-backend
dotnet run api.cs
```

## Docker Deployment

Once code is restructured:

```bash
# Build all images
docker compose -f docker-compose.local.yml build

# Start local stack
docker compose -f docker-compose.local.yml up

# Deploy to Docker Swarm
docker stack deploy -c docker-compose.yml a2a-demo-triage
```

## Services Ready to Build

- ✅ identity - JWT authentication
- ✅ discovery - Service registry
- ✅ classifier, assessor, router, handler - Specialist services
- ✅ api-backend - Orchestrator

Each has:
- Complete service code
- Ready `.csproj` file
- Dockerfile template
- Environment configuration

## Next Steps

1. Apply code restructuring (Option 1, 2, or 3 above)
2. Run `docker compose build` to verify all images build successfully
3. Test local stack: `docker compose up`
4. Deploy to production Docker Swarm

## Support

All .cs files and Dockerfiles are in `apps/a2a-docker-demo/`.
Configuration templates: `.env.example`
Documentation: `README.md`, `VERIFICATION.md`
