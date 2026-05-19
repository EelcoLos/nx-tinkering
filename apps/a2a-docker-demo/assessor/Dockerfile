# Use SDK directly - .cs files with #: directives need a project file
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine

WORKDIR /app

# Copy source and project
COPY assessor.cs .
COPY assessor.csproj .

# Environment
EXPOSE 5053
ENV ASPNETCORE_URLS=http://+:5053

# Run using dotnet run (supports #: directives with project present)
ENTRYPOINT ["dotnet", "run"]
