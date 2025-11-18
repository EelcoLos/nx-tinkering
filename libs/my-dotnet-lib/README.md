# MyDotNetLib

This is a minimal .NET library that demonstrates packaging a NuGet package via an Nx `pack` target.

Usage

- Build and create a NuGet package with Nx:

  nx run libs-my-dotnet-lib:pack

Or use the `release` target (alias for pack) so it integrates with Nx release flows that look for a `release` target:

  nx run libs-my-dotnet-lib:release

- Or run dotnet directly from the library folder:

  dotnet restore
  dotnet pack -c Release -o ../../dist/packages

The produced .nupkg files will be placed in `dist/packages`.
