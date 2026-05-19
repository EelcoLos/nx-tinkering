FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app
COPY api.cs api.csproj ./

RUN dotnet publish -c Release -o out

EXPOSE 5056
ENV ASPNETCORE_URLS=http://+:5056

ENTRYPOINT ["dotnet", "out/api.dll"]
