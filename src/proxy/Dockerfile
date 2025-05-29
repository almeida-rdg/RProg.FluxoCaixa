# Dockerfile para RProg.FluxoCaixa.Proxy - YARP Reverse Proxy
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar arquivo de projeto e restaurar dependências
COPY ["RProg.FluxoCaixa.Proxy/RProg.FluxoCaixa.Proxy.csproj", "RProg.FluxoCaixa.Proxy/"]
RUN dotnet restore "RProg.FluxoCaixa.Proxy/RProg.FluxoCaixa.Proxy.csproj"

# Copiar código fonte e compilar
COPY . .
WORKDIR "/src/RProg.FluxoCaixa.Proxy"
RUN dotnet build "RProg.FluxoCaixa.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RProg.FluxoCaixa.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Instalar dependências necessárias para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Criar diretórios necessários para o proxy
RUN mkdir -p /app/logs /app/cache

# Copiar aplicação publicada
COPY --from=publish /app/publish .

# Configurar usuário não-root para segurança
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Configurar variáveis de ambiente específicas do YARP
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development \
    YARP_ENABLE_HEALTH_CHECKS=true \
    YARP_ENABLE_METRICS=true \
    CACHE_ENABLED=true \
    RATE_LIMITING_ENABLED=true

# Health check para YARP
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Proxy.dll"]