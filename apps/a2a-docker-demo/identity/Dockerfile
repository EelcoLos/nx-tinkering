FROM mcr.microsoft.com/dotnet/sdk:11.0-preview-alpine AS build
WORKDIR /build
COPY identity.csproj .
COPY Program.cs .
COPY appsettings.json .
COPY Infrastructure/ Infrastructure/
COPY Services/ Services/
COPY Skills/ Skills/
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5050
ENV ASPNETCORE_URLS=http://+:5050
ENTRYPOINT ["dotnet", "identity.dll"]
