# Use SDK directly - .cs files with #: directives need a project file
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine

WORKDIR /app

# Copy source and project
COPY api.cs .
COPY api_backend.csproj .

# Environment
EXPOSE 5056
ENV ASPNETCORE_URLS=http://+:5056

# Run using dotnet run (supports #: directives with project present)
ENTRYPOINT ["dotnet", "run"]
