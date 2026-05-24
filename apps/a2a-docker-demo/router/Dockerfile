FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /build
COPY Directory.Packages.props .
COPY common ./common
COPY router ./router
RUN dotnet publish ./router/router.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5054
ENV ASPNETCORE_URLS=http://+:5054
ENTRYPOINT ["dotnet", "router.dll"]
