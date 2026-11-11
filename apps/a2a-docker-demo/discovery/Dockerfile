FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine AS build
WORKDIR /build
COPY Directory.Packages.props .
COPY common ./common
COPY discovery ./discovery
RUN dotnet publish ./discovery/discovery.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5051
ENV ASPNETCORE_URLS=http://+:5051
ENTRYPOINT ["dotnet", "discovery.dll"]
