# Use SDK directly - .cs files with #: directives need a project file
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine

WORKDIR /app

# Copy source and project
COPY identity.cs .
COPY identity.csproj .

# Environment
EXPOSE 5050
ENV ASPNETCORE_URLS=http://+:5050

# Run using dotnet run (supports #: directives with project present)
ENTRYPOINT ["dotnet", "run"]
