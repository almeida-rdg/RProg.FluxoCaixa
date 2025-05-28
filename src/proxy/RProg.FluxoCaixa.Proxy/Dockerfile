# Usar imagem base do .NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# Instalar Docker CLI para comunicação com Docker host
RUN apt-get update \
    && apt-get install -y curl \
    && curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null \
    && apt-get update \
    && apt-get install -y docker-ce-cli \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RProg.FluxoCaixa.Proxy.csproj", "."]
RUN dotnet restore "./RProg.FluxoCaixa.Proxy.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "RProg.FluxoCaixa.Proxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RProg.FluxoCaixa.Proxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Criar diretório para logs
RUN mkdir -p /app/logs

# Definir variáveis de ambiente
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080;https://+:8443

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Proxy.dll"]
