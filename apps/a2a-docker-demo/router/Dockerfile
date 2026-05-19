FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
WORKDIR /app
COPY router.cs .
EXPOSE 5054
ENV ASPNETCORE_URLS=http://+:5054
CMD ["dotnet", "run", "router.cs"]
