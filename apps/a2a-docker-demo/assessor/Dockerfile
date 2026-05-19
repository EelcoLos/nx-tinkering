FROM mcr.microsoft.com/dotnet/sdk:11.0-preview-alpine AS build
WORKDIR /build
COPY assessor.csproj .
COPY Program.cs .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5053
ENV ASPNETCORE_URLS=http://+:5053
ENTRYPOINT ["dotnet", "assessor.dll"]
