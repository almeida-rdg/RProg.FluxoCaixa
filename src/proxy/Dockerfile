# ==============================================================================
# Dockerfile para RProg.FluxoCaixa.Proxy - YARP Reverse Proxy
# ==============================================================================
# 
# Este Dockerfile cria uma imagem otimizada para o YARP Proxy do FluxoCaixa
# com suporte completo a monitoramento Docker, cache, autenticação e logging.
#
# Build: docker build -t rprog-fluxocaixa-proxy:latest .
# Run:   docker run -p 8080:8080 -v /var/run/docker.sock:/var/run/docker.sock rprog-fluxocaixa-proxy:latest
# ==============================================================================

# Estágio base - Runtime do .NET 8 otimizado
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base

# Configurar usuário não-root para segurança
RUN groupadd -r appuser && useradd -r -g appuser appuser

WORKDIR /app

# Expor portas para HTTP e HTTPS
EXPOSE 8080
EXPOSE 8443

# Instalar dependências necessárias para monitoramento Docker e operações do proxy
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        ca-certificates \
        gnupg \
        lsb-release \
        jq \
        htop \
        procps \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null \
    && apt-get update \
    && apt-get install -y --no-install-recommends docker-ce-cli \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/* /var/cache/apt/*

# Criar diretórios necessários para o proxy
RUN mkdir -p /app/logs /app/cache /app/config /app/metrics /app/temp \
    && chown -R appuser:appuser /app \
    && chmod 755 /app/logs /app/cache /app/config /app/metrics /app/temp

# ==============================================================================
# Estágio de build - SDK do .NET 8
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build

WORKDIR /src

# Copiar arquivo de projeto primeiro para aproveitar cache do Docker
COPY ["RProg.FluxoCaixa.Proxy/RProg.FluxoCaixa.Proxy.csproj", "RProg.FluxoCaixa.Proxy/"]

# Restaurar dependências (aproveitando cache se não houver mudanças no .csproj)
RUN dotnet restore "RProg.FluxoCaixa.Proxy/RProg.FluxoCaixa.Proxy.csproj" \
    --runtime linux-x64 \
    --verbosity minimal

# Copiar código fonte completo
COPY ["RProg.FluxoCaixa.Proxy/", "RProg.FluxoCaixa.Proxy/"]

# Build da aplicação com otimizações
WORKDIR "/src/RProg.FluxoCaixa.Proxy"
RUN dotnet build "RProg.FluxoCaixa.Proxy.csproj" \
    -c Release \
    -o /app/build \
    --no-restore \
    --verbosity minimal \
    /p:UseAppHost=false

# ==============================================================================
# Estágio de publicação com otimizações
FROM build AS publish

RUN dotnet publish "RProg.FluxoCaixa.Proxy.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --no-build \
    --verbosity minimal \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false \
    /p:PublishReadyToRun=true \
    /p:RuntimeIdentifier=linux-x64

# ==============================================================================
# Estágio final - Imagem de produção otimizada
FROM base AS final

WORKDIR /app

# Copiar aplicação publicada
COPY --from=publish /app/publish .

# Definir variáveis de ambiente para produção do YARP Proxy
ENV ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_EnableDiagnostics=0 \
    TZ=America/Sao_Paulo

# Configurações específicas do YARP e monitoramento
ENV YARP_ENABLE_HEALTH_CHECKS=true \
    YARP_ENABLE_METRICS=true \
    YARP_LOG_LEVEL=Information \
    YARP_PROXY_TIMEOUT=30 \
    DOCKER_HOST=unix:///var/run/docker.sock \
    CACHE_ENABLED=true \
    RATE_LIMITING_ENABLED=true \
    JWT_VALIDATION_ENABLED=true

# Configurações de performance e logging
ENV LOGGING__LOGLEVEL__DEFAULT=Information \
    LOGGING__LOGLEVEL__MICROSOFT=Warning \
    LOGGING__LOGLEVEL__SYSTEM=Warning \
    SERILOG__MINIMUMLEVEL=Information

# Labels para metadados e versionamento
LABEL maintainer="RProg FluxoCaixa Team <dev@rprog.com.br>" \
      version="1.0.0" \
      description="YARP Reverse Proxy para sistema FluxoCaixa com monitoramento Docker" \
      component="proxy" \
      tier="gateway" \
      technology="dotnet8,yarp,docker" \
      environment="production"

# Health check otimizado para YARP
HEALTHCHECK --interval=30s \
            --timeout=10s \
            --start-period=60s \
            --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Volume para logs persistentes (opcional)
VOLUME ["/app/logs", "/app/cache"]

# Mudar para usuário não-root para segurança
USER appuser

# Ponto de entrada da aplicação
ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Proxy.dll"]
