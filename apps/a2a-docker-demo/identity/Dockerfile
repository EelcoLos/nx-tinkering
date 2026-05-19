FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine as builder

WORKDIR /app
COPY identity.cs .

# Create webapi project, replace Program.cs, build and publish
RUN dotnet new webapi -f net9.0 --force -o . && \
    rm Program.cs && \
    cp identity.cs Program.cs && \
    # Strip #: directives from Program.cs
    sed -i '/^#:/d' Program.cs && \
    dotnet restore && \
    dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

WORKDIR /app
COPY --from=builder /app/out .

EXPOSE 5050
ENV ASPNETCORE_URLS=http://+:5050

ENTRYPOINT ["dotnet", "app.dll"]
