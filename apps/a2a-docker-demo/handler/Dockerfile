FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
WORKDIR /app
COPY handler.cs .
EXPOSE 5055
ENV ASPNETCORE_URLS=http://+:5055
CMD ["dotnet", "run", "handler.cs"]
