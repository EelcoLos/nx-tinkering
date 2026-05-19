FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app
COPY classifier.cs classifier.csproj ./

RUN dotnet publish -c Release -o out

EXPOSE 5052
ENV ASPNETCORE_URLS=http://+:5052

ENTRYPOINT ["dotnet", "out/classifier.dll"]
