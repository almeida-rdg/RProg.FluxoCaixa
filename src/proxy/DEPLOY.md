# Guia de Deploy - RProg.FluxoCaixa.Proxy

Este guia apresenta os passos para deployment do proxy reverso em diferentes ambientes.

## Pré-requisitos

- Docker 20.10+
- Docker Compose 2.0+
- .NET 8.0 SDK (para desenvolvimento)
- Certificados SSL (para produção)

## Configuração de Certificados SSL

### Desenvolvimento (Auto-assinado)

```bash
# Criar diretório para certificados
mkdir -p src/proxy/certs

# Gerar certificado auto-assinado
dotnet dev-certs https -ep src/proxy/certs/aspnetapp.pfx -p YourSecurePassword
dotnet dev-certs https --trust
```

### Produção (Let's Encrypt)

```bash
# Instalar Certbot
sudo apt install certbot

# Gerar certificados
sudo certbot certonly --standalone -d yourdomain.com

# Converter para PFX
sudo openssl pkcs12 -export -out /etc/ssl/certs/aspnetapp.pfx \
  -inkey /etc/letsencrypt/live/yourdomain.com/privkey.pem \
  -in /etc/letsencrypt/live/yourdomain.com/cert.pem \
  -password pass:YourSecurePassword
```

## Ambientes de Deploy

### 1. Desenvolvimento Local

```bash
# 1. Configurar variáveis de ambiente
cp src/proxy/RProg.FluxoCaixa.Proxy/appsettings.Development.json.example \
   src/proxy/RProg.FluxoCaixa.Proxy/appsettings.Development.json

# 2. Subir dependências
docker-compose up -d sqlserver rabbitmq

# 3. Executar APIs
cd src/lancamentos && dotnet run &
cd src/consolidado && dotnet run &

# 4. Executar proxy
cd src/proxy/RProg.FluxoCaixa.Proxy
dotnet run
```

### 2. Docker Compose - Desenvolvimento

```bash
# 1. Build das imagens
docker-compose build

# 2. Subir todos os serviços
docker-compose up -d

# 3. Verificar status
docker-compose ps
docker-compose logs proxy -f
```

### 3. Docker Compose - Produção

```bash
# 1. Configurar ambiente de produção
export ASPNETCORE_ENVIRONMENT=Production

# 2. Usar compose específico para produção
docker-compose -f docker-compose.proxy.yaml up -d

# 3. Verificar health checks
curl http://localhost/health
curl https://localhost/health
```

### 4. Kubernetes (Futuro)

```yaml
# k8s-proxy-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: fluxo-proxy
spec:
  replicas: 3
  selector:
    matchLabels:
      app: fluxo-proxy
  template:
    metadata:
      labels:
        app: fluxo-proxy
    spec:
      containers:
      - name: proxy
        image: rprog/fluxocaixa-proxy:latest
        ports:
        - containerPort: 80
        - containerPort: 443
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 10
```

## Configuração por Ambiente

### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Yarp": {
    "LoadTest": true,
    "DebugHeaders": true
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "TimeWindow": "00:01:00"
  }
}
```

### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yarp": "Warning"
    }
  },
  "Yarp": {
    "LoadTest": false,
    "DebugHeaders": false
  },
  "RateLimiting": {
    "PermitLimit": 60,
    "TimeWindow": "00:01:00"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://+:80"
      },
      "Https": {
        "Url": "https://+:443",
        "Certificate": {
          "Path": "/https/aspnetapp.pfx",
          "Password": "${SSL_CERT_PASSWORD}"
        }
      }
    }
  }
}
```

## Scripts de Deploy

### deploy.sh (Linux/Mac)
```bash
#!/bin/bash
set -e

echo "=== Deploy RProg.FluxoCaixa.Proxy ==="

# Verificar pré-requisitos
command -v docker >/dev/null 2>&1 || { echo "Docker não encontrado!" >&2; exit 1; }
command -v docker-compose >/dev/null 2>&1 || { echo "Docker Compose não encontrado!" >&2; exit 1; }

# Definir ambiente
ENVIRONMENT=${1:-development}
echo "Ambiente: $ENVIRONMENT"

# Build das imagens
echo "Building images..."
docker-compose build --no-cache

# Parar serviços existentes
echo "Stopping existing services..."
docker-compose down

# Subir banco e dependências primeiro
echo "Starting dependencies..."
docker-compose up -d sqlserver rabbitmq

# Aguardar banco ficar pronto
echo "Waiting for SQL Server..."
sleep 30

# Subir APIs
echo "Starting APIs..."
docker-compose up -d lancamentos-api consolidado-api

# Aguardar APIs ficarem prontas
echo "Waiting for APIs..."
sleep 20

# Subir proxy
echo "Starting proxy..."
docker-compose up -d proxy

# Verificar status
echo "Checking health..."
sleep 10
curl -f http://localhost/health || exit 1

echo "Deploy completed successfully!"
echo "Proxy available at: http://localhost"
echo "HTTPS: https://localhost"
```

### deploy.ps1 (Windows)
```powershell
# Deploy script for Windows
param(
    [string]$Environment = "development"
)

Write-Host "=== Deploy RProg.FluxoCaixa.Proxy ===" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow

# Verificar Docker
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker não encontrado!"
    exit 1
}

# Build e deploy
try {
    Write-Host "Building images..." -ForegroundColor Blue
    docker-compose build --no-cache

    Write-Host "Stopping existing services..." -ForegroundColor Blue
    docker-compose down

    Write-Host "Starting services..." -ForegroundColor Blue
    docker-compose up -d

    Start-Sleep 30

    Write-Host "Checking health..." -ForegroundColor Blue
    $response = Invoke-WebRequest -Uri "http://localhost/health" -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "Deploy completed successfully!" -ForegroundColor Green
        Write-Host "Proxy available at: http://localhost" -ForegroundColor Cyan
        Write-Host "HTTPS: https://localhost" -ForegroundColor Cyan
    } else {
        throw "Health check failed"
    }
} catch {
    Write-Error "Deploy failed: $_"
    exit 1
}
```

## Monitoramento Pós-Deploy

### Health Checks
```bash
# Status geral
curl http://localhost/health

# Status detalhado (development)
curl http://localhost/health/detailed

# Métricas de cache
curl http://localhost/health | jq '.cache'

# Status dos backends
curl http://localhost/health | jq '.backends'
```

### Logs
```bash
# Logs em tempo real
docker-compose logs -f proxy

# Logs de segurança
docker-compose logs proxy | grep "Security"

# Logs de cache
docker-compose logs proxy | grep "Cache"

# Logs de rate limiting
docker-compose logs proxy | grep "RateLimit"
```

### Métricas de Performance
```bash
# Teste de carga simples
for i in {1..100}; do
  curl -s http://localhost/api/lancamentos > /dev/null &
done
wait

# Verificar cache hits
docker-compose logs proxy | grep "Cache-Hit" | wc -l
```

## Rollback

### Rollback Rápido
```bash
# Voltar para versão anterior
docker-compose down
docker tag rprog/fluxocaixa-proxy:previous rprog/fluxocaixa-proxy:latest
docker-compose up -d proxy
```

### Rollback com Zero Downtime
```bash
# Scale up nova versão
docker-compose up -d --scale proxy=2

# Aguardar health check
sleep 30

# Remover versão antiga
docker-compose stop proxy
docker-compose rm -f proxy
docker-compose up -d --scale proxy=1
```

## Troubleshooting

### Problemas Comuns

**Proxy não inicia**
```bash
# Verificar logs
docker-compose logs proxy

# Verificar certificados
ls -la src/proxy/certs/

# Verificar portas
netstat -tulpn | grep :80
netstat -tulpn | grep :443
```

**502 Bad Gateway**
```bash
# Verificar APIs
curl http://localhost:8080/health  # lancamentos
curl http://localhost:8081/health  # consolidado

# Verificar rede Docker
docker network ls
docker network inspect src_proxy-net
```

**SSL/TLS Errors**
```bash
# Verificar certificado
openssl x509 -in src/proxy/certs/aspnetapp.pfx -text -noout

# Regenerar certificado
rm -rf src/proxy/certs/*
dotnet dev-certs https -ep src/proxy/certs/aspnetapp.pfx -p YourSecurePassword
```

## Backup e Restore

### Backup da Configuração
```bash
# Backup dos arquivos de configuração
tar -czf proxy-config-backup-$(date +%Y%m%d).tar.gz \
  src/proxy/RProg.FluxoCaixa.Proxy/appsettings*.json \
  src/proxy/certs/ \
  docker-compose.yaml
```

### Restore
```bash
# Restore da configuração
tar -xzf proxy-config-backup-YYYYMMDD.tar.gz
docker-compose down
docker-compose up -d
```
