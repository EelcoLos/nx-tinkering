FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app
COPY handler.cs handler.csproj ./

RUN dotnet publish -c Release -o out

EXPOSE 5055
ENV ASPNETCORE_URLS=http://+:5055

ENTRYPOINT ["dotnet", "out/handler.dll"]
