FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app

# Copy source and create Program.cs
COPY router.cs Program.cs

# Create minimal csproj for dotnet run
RUN echo '<Project Sdk="Microsoft.NET.Sdk.Web">' > app.csproj && \
    echo '  <PropertyGroup>' >> app.csproj && \
    echo '    <TargetFramework>net9.0</TargetFramework>' >> app.csproj && \
    echo '    <Nullable>enable</Nullable>' >> app.csproj && \
    echo '    <ImplicitUsings>enable</ImplicitUsings>' >> app.csproj && \
    echo '    <LangVersion>latest</LangVersion>' >> app.csproj && \
    echo '    <Features>FileBasedProgram</Features>' >> app.csproj && \
    echo '  </PropertyGroup>' >> app.csproj && \
    echo '  <ItemGroup>' >> app.csproj && \
    echo '    <PackageReference Include="FastEndpoints" Version="8.*" />' >> app.csproj && \
    echo '    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.*" />' >> app.csproj && \
    echo '    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.*" />' >> app.csproj && \
    echo '  </ItemGroup>' >> app.csproj && \
    echo '</Project>' >> app.csproj

EXPOSE 5054
ENV ASPNETCORE_URLS=http://+:5054

ENTRYPOINT ["dotnet", "run"]
