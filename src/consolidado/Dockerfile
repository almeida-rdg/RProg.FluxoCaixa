# Dockerfile otimizado para RProg.FluxoCaixa.Consolidado
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# OTIMIZAÇÃO: Copiar arquivos de configuração de dependências primeiro
COPY ["../../Directory.Build.props", "./"]
COPY ["../../Directory.Packages.props", "./"]
COPY ["../../nuget.config", "./"]
COPY ["../../global.json", "./"]

# Copiar arquivos de projeto para restore
COPY ["RProg.FluxoCaixa.Consolidado/RProg.FluxoCaixa.Consolidado.csproj", "RProg.FluxoCaixa.Consolidado/"]

# Executar restore (esta layer será cacheada se as dependências não mudarem)
RUN dotnet restore "RProg.FluxoCaixa.Consolidado/RProg.FluxoCaixa.Consolidado.csproj"

# Copiar código fonte e compilar
COPY . .
WORKDIR "/src/RProg.FluxoCaixa.Consolidado"
RUN dotnet build "RProg.FluxoCaixa.Consolidado.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RProg.FluxoCaixa.Consolidado.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Criar diretório para logs
RUN mkdir -p /app/logs

# Copiar aplicação publicada
COPY --from=publish /app/publish .

# Configurar usuário não-root para segurança
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Configurar variáveis de ambiente padrão
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check para container
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Consolidado.dll"]
