FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
WORKDIR /app
COPY discovery.cs .
EXPOSE 5051
ENV ASPNETCORE_URLS=http://+:5051
CMD ["dotnet", "run", "discovery.cs"]
