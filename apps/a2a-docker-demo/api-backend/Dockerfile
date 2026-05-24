FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /build
COPY Directory.Packages.props .
COPY common ./common
COPY api-backend ./api-backend
RUN dotnet publish ./api-backend/api-backend.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5056
ENV ASPNETCORE_URLS=http://+:5056
ENTRYPOINT ["dotnet", "api-backend.dll"]
