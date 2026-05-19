FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine

WORKDIR /app

# Copy source and rename to Program.cs
COPY router.cs Program.cs
RUN sed -i '/^#:/d' Program.cs

# Create csproj using echo (avoiding heredoc parsing issues)
RUN printf '%s\n' \
  '<Project Sdk="Microsoft.NET.Sdk.Web">' \
  '  <PropertyGroup>' \
  '    <TargetFramework>net9.0</TargetFramework>' \
  '    <Nullable>enable</Nullable>' \
  '    <ImplicitUsings>enable</ImplicitUsings>' \
  '    <LangVersion>latest</LangVersion>' \
  '  </PropertyGroup>' \
  '  <ItemGroup>' \
  '    <PackageReference Include="FastEndpoints" Version="8.*" />' \
  '    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.*" />' \
  '    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.*" />' \
  '  </ItemGroup>' \
  '</Project>' > app.csproj

EXPOSE 5054
ENV ASPNETCORE_URLS=http://+:5054

ENTRYPOINT ["dotnet", "run"]
