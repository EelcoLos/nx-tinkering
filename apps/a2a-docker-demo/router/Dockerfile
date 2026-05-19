FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app
COPY router.cs router.csproj ./

RUN dotnet publish -c Release -o out

EXPOSE 5054
ENV ASPNETCORE_URLS=http://+:5054

ENTRYPOINT ["dotnet", "out/router.dll"]
