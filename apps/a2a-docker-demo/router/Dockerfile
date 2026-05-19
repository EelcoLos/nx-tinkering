FROM mcr.microsoft.com/dotnet/sdk:11.0-preview-alpine AS build
WORKDIR /build
COPY router.csproj .
COPY Program.cs .
COPY appsettings.json .
COPY Infrastructure/ Infrastructure/
COPY Services/ Services/
COPY Skills/ Skills/
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5054
ENV ASPNETCORE_URLS=http://+:5054
ENTRYPOINT ["dotnet", "router.dll"]
