# Dockerfile otimizado para restore compartilhado de pacotes NuGet - PRODUÇÃO
# Remove projetos de teste para otimizar imagem de produção
# Demonstra as melhores práticas para cache de layers no Docker

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage base com utilitários comuns
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# OTIMIZAÇÃO 1: Copiar arquivos de configuração primeiro para melhor cache
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["nuget.config", "./"]
COPY ["global.json", "./"]

# OTIMIZAÇÃO 2: Copiar apenas os arquivos .csproj de produção (sem testes)
COPY ["src/consolidado/RProg.FluxoCaixa.Consolidado/RProg.FluxoCaixa.Consolidado.csproj", "src/consolidado/RProg.FluxoCaixa.Consolidado/"]
COPY ["src/lancamentos/RProg.FluxoCaixa.Lancamentos/RProg.FluxoCaixa.Lancamentos.csproj", "src/lancamentos/RProg.FluxoCaixa.Lancamentos/"]
COPY ["src/proxy/RProg.FluxoCaixa.Proxy/RProg.FluxoCaixa.Proxy.csproj", "src/proxy/RProg.FluxoCaixa.Proxy/"]
COPY ["src/worker/RProg.FluxoCaixa.Worker/RProg.FluxoCaixa.Worker.csproj", "src/worker/RProg.FluxoCaixa.Worker/"]

# OTIMIZAÇÃO 3: Restore centralizado uma única vez para todos os projetos de produção
# Cada pacote NuGet será baixado apenas uma vez e compartilhado entre todos os projetos
RUN dotnet restore "src/consolidado/RProg.FluxoCaixa.Consolidado/RProg.FluxoCaixa.Consolidado.csproj" \
    && dotnet restore "src/lancamentos/RProg.FluxoCaixa.Lancamentos/RProg.FluxoCaixa.Lancamentos.csproj" \
    && dotnet restore "src/proxy/RProg.FluxoCaixa.Proxy/RProg.FluxoCaixa.Proxy.csproj" \
    && dotnet restore "src/worker/RProg.FluxoCaixa.Worker/RProg.FluxoCaixa.Worker.csproj"

# OTIMIZAÇÃO 4: Copiar código fonte apenas após restore
COPY . .

# Stages de build para cada aplicação usando cache compartilhado
FROM build AS build-consolidado
WORKDIR "/src/src/consolidado/RProg.FluxoCaixa.Consolidado"
RUN dotnet build "RProg.FluxoCaixa.Consolidado.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS build-lancamentos  
WORKDIR "/src/src/lancamentos/RProg.FluxoCaixa.Lancamentos"
RUN dotnet build "RProg.FluxoCaixa.Lancamentos.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS build-proxy
WORKDIR "/src/src/proxy/RProg.FluxoCaixa.Proxy"
RUN dotnet build "RProg.FluxoCaixa.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS build-worker
WORKDIR "/src/src/worker/RProg.FluxoCaixa.Worker"
RUN dotnet build "RProg.FluxoCaixa.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Stages de publicação otimizados
FROM build-consolidado AS publish-consolidado
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RProg.FluxoCaixa.Consolidado.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM build-lancamentos AS publish-lancamentos
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RProg.FluxoCaixa.Lancamentos.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM build-proxy AS publish-proxy
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RProg.FluxoCaixa.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM build-worker AS publish-worker
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RProg.FluxoCaixa.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# Imagens finais otimizadas para produção
FROM base AS final-consolidado
WORKDIR /app

# Criar diretório para logs
RUN mkdir -p /app/logs

# Copiar aplicação publicada
COPY --from=publish-consolidado /app/publish .

# Configurar usuário não-root para segurança
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Configurar variáveis de ambiente padrão
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check para container
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Consolidado.dll"]

FROM base AS final-lancamentos
WORKDIR /app
RUN mkdir -p /app/logs
COPY --from=publish-lancamentos /app/publish .
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1
ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Lancamentos.dll"]

FROM base AS final-proxy
WORKDIR /app
RUN mkdir -p /app/logs
COPY --from=publish-proxy /app/publish .
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1
ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Proxy.dll"]

FROM base AS final-worker
WORKDIR /app
RUN mkdir -p /app/logs
COPY --from=publish-worker /app/publish .
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
ENV DOTNET_ENVIRONMENT=Development
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD ps aux | grep dotnet || exit 1
ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Worker.dll"]