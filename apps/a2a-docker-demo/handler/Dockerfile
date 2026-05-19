FROM mcr.microsoft.com/dotnet/sdk:11.0-preview-alpine AS build
WORKDIR /build
COPY handler.csproj .
COPY Program.cs .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5055
ENV ASPNETCORE_URLS=http://+:5055
ENTRYPOINT ["dotnet", "handler.dll"]
