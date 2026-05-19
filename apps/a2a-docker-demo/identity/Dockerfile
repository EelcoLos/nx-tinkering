FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app
COPY identity.cs .

# Create webapi project and replace Program.cs
RUN dotnet new webapi -f net9.0 --force -o . && \
    rm Program.cs && \
    cp identity.cs Program.cs && \
    dotnet build

EXPOSE 5050
ENV ASPNETCORE_URLS=http://+:5050

CMD ["dotnet", "run", "--no-build"]
