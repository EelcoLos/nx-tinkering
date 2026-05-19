FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app
COPY assessor.cs assessor.csproj ./

RUN dotnet publish -c Release -o out

EXPOSE 5053
ENV ASPNETCORE_URLS=http://+:5053

ENTRYPOINT ["dotnet", "out/assessor.dll"]
