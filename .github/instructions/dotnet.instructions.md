---
applyTo: '**/*.csproj,**/Directory.Packages.props'
---

# .NET Guidelines

## Central Package Management (CPM)

This repository uses [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management).
All NuGet package versions are declared centrally in `Directory.Packages.props` at the repo root.

### Rules

- **Never** add a `Version` attribute to a `<PackageReference>` in a `.csproj` file.
- **Always** add or update the version in `Directory.Packages.props` using a `<PackageVersion>` entry.
- Keep `<PackageVersion>` entries in `Directory.Packages.props` sorted alphabetically within their comment group.

### Example

**`Directory.Packages.props`** (add the version here):
```xml
<PackageVersion Include="SomePackage" Version="1.2.3" />
```

**`YourProject.csproj`** (reference without version):
```xml
<PackageReference Include="SomePackage" />
```
