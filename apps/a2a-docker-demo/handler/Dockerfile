FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /build
COPY Directory.Packages.props .
COPY common ./common
COPY handler ./handler
RUN dotnet publish ./handler/handler.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5055
ENV ASPNETCORE_URLS=http://+:5055
ENTRYPOINT ["dotnet", "handler.dll"]
