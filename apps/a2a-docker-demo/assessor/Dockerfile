FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
WORKDIR /app
COPY assessor.cs .
EXPOSE 5053
ENV ASPNETCORE_URLS=http://+:5053
CMD ["dotnet", "run", "assessor.cs"]
