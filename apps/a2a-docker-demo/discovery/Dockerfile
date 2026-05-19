FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app
COPY discovery.cs discovery.csproj ./

RUN dotnet publish -c Release -o out

EXPOSE 5051
ENV ASPNETCORE_URLS=http://+:5051

ENTRYPOINT ["dotnet", "out/discovery.dll"]
