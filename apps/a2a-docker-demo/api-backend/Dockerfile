# Multi-stage build - compile in SDK, runtime with aspnet
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS builder

WORKDIR /build

# Copy and prepare source
COPY api Program.cs
COPY api.csproj app.csproj

# Remove #: directives
RUN sed -i '/^#:/d' Program.cs

# Build and publish
RUN dotnet publish -c Release --no-self-contained --use-current-runtime -o /publish 2>&1 | tail -20

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

WORKDIR /app

COPY --from=builder /publish .

EXPOSE 5056
ENV ASPNETCORE_URLS=http://+:5056
ENV DOTNET_GCHeapCount=1
ENV DOTNET_GCHeapAffinitizeMask=1

ENTRYPOINT ["dotnet", "app.dll"]
