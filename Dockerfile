# Imagem Debian (não Alpine) de propósito: traz o tzdata que o fuso America/Sao_Paulo exige.
# Sem ele, a resolução do fuso da academia falha logo no primeiro uso.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Os .csproj vêm primeiro para que a camada de restore seja reaproveitada enquanto
# só o código-fonte mudar.
COPY global.json Directory.Build.props .editorconfig ./
COPY src/AnbuFight.Domain/AnbuFight.Domain.csproj src/AnbuFight.Domain/
COPY src/AnbuFight.Application/AnbuFight.Application.csproj src/AnbuFight.Application/
COPY src/AnbuFight.Infrastructure/AnbuFight.Infrastructure.csproj src/AnbuFight.Infrastructure/
COPY src/AnbuFight.Api/AnbuFight.Api.csproj src/AnbuFight.Api/
RUN dotnet restore src/AnbuFight.Api/AnbuFight.Api.csproj

COPY src/ src/
RUN dotnet publish src/AnbuFight.Api/AnbuFight.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app .

# Usuário sem privilégios, já definido nas imagens oficiais.
USER $APP_UID

ENTRYPOINT ["dotnet", "AnbuFight.Api.dll"]
