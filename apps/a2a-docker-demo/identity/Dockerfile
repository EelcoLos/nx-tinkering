# Multi-stage build - compile in SDK, runtime with aspnet
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS builder

WORKDIR /build

# Copy the original file  
COPY identity.cs Program.cs

# Extract the setup code and classes into separate files
# Remove #: directives
RUN sed -i '/^#:/d' Program.cs

# Copy the csproj
COPY identity.csproj .

# Build and publish
RUN dotnet publish -c Release --no-self-contained --use-current-runtime -o /publish 2>&1 | tail -20

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

WORKDIR /app

COPY --from=builder /publish .

EXPOSE 5050
ENV ASPNETCORE_URLS=http://+:5050
ENV DOTNET_GCHeapCount=1
ENV DOTNET_GCHeapAffinitizeMask=1

ENTRYPOINT ["dotnet", "app.dll"]
