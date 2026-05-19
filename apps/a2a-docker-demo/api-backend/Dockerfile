FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
WORKDIR /app
COPY api.cs .
EXPOSE 5056
ENV ASPNETCORE_URLS=http://+:5056
CMD ["dotnet", "run", "api.cs"]
