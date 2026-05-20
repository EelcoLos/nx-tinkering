FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /build
COPY Directory.Packages.props .
COPY assessor/assessor.csproj .
COPY assessor/Program.cs .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5053
ENV ASPNETCORE_URLS=http://+:5053
ENTRYPOINT ["dotnet", "assessor.dll"]
