# MyDotNetLib

This is a minimal .NET library that demonstrates packaging a NuGet package via the `pack` target that `@nx/dotnet` infers.

Usage

- Restore (CI restores via `dotnet restore nx-tinker.slnx`), then build a Release package with Nx:

  nx run libs-my-dotnet-lib:restore
  nx run libs-my-dotnet-lib:pack

- Or run dotnet directly from the library folder:

  dotnet pack -c Release

The produced .nupkg files will be placed in `dist/packages` (`PackageOutputPath` in the csproj).
