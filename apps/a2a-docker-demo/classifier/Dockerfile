FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
WORKDIR /app
COPY classifier.cs .
EXPOSE 5052
ENV ASPNETCORE_URLS=http://+:5052
CMD ["dotnet", "run", "classifier.cs"]
