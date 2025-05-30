# Dockerfile otimizado para RProg.FluxoCaixa.Worker
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# OTIMIZAÇÃO: Copiar arquivos de configuração de dependências primeiro
COPY ["../Directory.Build.props", "./"]
COPY ["../Directory.Packages.props", "./"]
COPY ["../nuget.config", "./"]
COPY ["../global.json", "./"]

# Copiar arquivo de projeto para restore
COPY ["RProg.FluxoCaixa.Worker/RProg.FluxoCaixa.Worker.csproj", "RProg.FluxoCaixa.Worker/"]

# Executar restore (esta layer será cacheada se as dependências não mudarem)
RUN dotnet restore "RProg.FluxoCaixa.Worker/RProg.FluxoCaixa.Worker.csproj"

# Copiar código fonte e compilar
COPY . .
WORKDIR "/src/RProg.FluxoCaixa.Worker"
RUN dotnet build "RProg.FluxoCaixa.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RProg.FluxoCaixa.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Criar diretório para logs
RUN mkdir -p /app/logs

# Copiar aplicação publicada
COPY --from=publish /app/publish .

# Configurar usuário não-root para segurança (já configurado na imagem base)
# USER app já definido na imagem base

# Configurar variáveis de ambiente padrão
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check para worker
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Worker.dll"]