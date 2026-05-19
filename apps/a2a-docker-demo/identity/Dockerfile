FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app
COPY identity.cs .

EXPOSE 5050
ENV ASPNETCORE_URLS=http://+:5050

CMD ["dotnet", "run", "identity.cs"]
