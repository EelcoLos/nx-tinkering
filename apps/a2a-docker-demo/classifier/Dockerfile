FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /build
COPY Directory.Packages.props .
COPY classifier/classifier.csproj .
COPY classifier/Program.cs .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5052
ENV ASPNETCORE_URLS=http://+:5052
ENTRYPOINT ["dotnet", "classifier.dll"]
