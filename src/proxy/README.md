# RProg.FluxoCaixa.Proxy

Proxy reverso implementado com YARP para balanceamento de carga, cache, proteção contra ataques e circuit breaker das APIs de lançamento e consolidado.

## Funcionalidades

### 🔄 Balanceamento de Carga
- Distribuição automática de requisições entre múltiplas instâncias
- Health checks contínuos para garantir disponibilidade
- Circuit breaker para failover automático
- Escalabilidade automática baseada em CPU/memória (70% threshold)

### 🛡️ Segurança e Proteção
- **Rate Limiting**: 60 req/min por IP, 10 req/seg burst
- **Proteção contra ataques**: XSS, SQL Injection, Path Traversal
- **Headers de segurança**: HSTS, X-Frame-Options, CSP
- **Autenticação JWT**: Bearer tokens para APIs protegidas
- **CORS**: Configuração flexível para origens permitidas

### ⚡ Cache Inteligente
- Cache em memória com TTL configurável por rota
- Headers informativos (`X-Cache-Status`, `X-Cache-TTL`)
- Invalidação automática de cache
- Configuração específica por endpoint

### 📊 Monitoramento e Observabilidade
- Logs estruturados com Serilog
- Health checks para todos os serviços
- Métricas de performance e disponibilidade
- Monitoramento automático de containers

## Configuração

### Variáveis de Ambiente

```bash
# Ambiente
ASPNETCORE_ENVIRONMENT=Development

# URLs
ASPNETCORE_URLS=http://+:80;https://+:443

# Certificados SSL
Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
Kestrel__Certificates__Default__Password=YourSecurePassword
```

### Configuração YARP (appsettings.Yarp.json)

```json
{
  "ReverseProxy": {
    "Routes": {
      "lancamentos-route": {
        "ClusterId": "lancamentos-cluster",
        "Match": {
          "Path": "/api/lancamentos/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/lancamentos/{**catch-all}" }
        ]
      }
    },
    "Clusters": {
      "lancamentos-cluster": {
        "HealthCheck": {
          "Enabled": true,
          "Interval": "00:00:30",
          "Timeout": "00:00:10",
          "Policy": "ConsecutiveFailures",
          "Path": "/health"
        },
        "Destinations": {
          "lancamentos-api-1": {
            "Address": "http://lancamentos-api:8080/"
          }
        }
      }
    }
  }
}
```

## Deployment

### Docker Compose

```bash
# Subir apenas o proxy e dependências
docker-compose -f docker-compose.proxy.yaml up -d

# Subir todo o ambiente
docker-compose up -d
```

### Escalabilidade Automática

Os containers são configurados para escalar automaticamente quando:
- CPU > 70%
- Memória > 70%
- Falhas de health check > 1 minuto

```yaml
deploy:
  replicas: 1
  resources:
    limits:
      memory: 512M
      cpus: '1.0'
    reservations:
      memory: 256M
      cpus: '0.5'
```

## Endpoints

### APIs Proxificadas
- `GET /api/lancamentos/*` → Lançamentos API
- `GET /api/consolidado/*` → Consolidado API

### Health Checks
- `GET /health` → Status do proxy
- `GET /health/ready` → Readiness probe
- `GET /health/live` → Liveness probe

### Métricas
- `GET /metrics` → Métricas Prometheus (futuro)

## Segurança

### Rate Limiting
- **Global**: 60 requisições por minuto por IP
- **Burst**: 10 requisições por segundo
- **Bloqueio**: IPs abusivos bloqueados por 5 minutos

### Proteção contra Ataques
- Validação de input para SQL Injection
- Headers XSS-Protection
- Detecção de Path Traversal
- Blacklist de User Agents suspeitos

### Autenticação
```csharp
// JWT Bearer Token
Authorization: Bearer <token>
```

## Monitoramento

### Logs
Logs estruturados em JSON com informações de:
- Request/Response times
- Cache hits/misses
- Rate limiting events
- Security violations
- Health check results

### Health Checks
- **Intervalo**: 30 segundos
- **Timeout**: 10 segundos
- **Retries**: 3 tentativas
- **Start Period**: 60 segundos

## Desenvolvimento

### Executar Testes
```bash
dotnet test src/proxy/RProg.FluxoCaixa.Proxy.Test/
```

### Executar Localmente
```bash
cd src/proxy/RProg.FluxoCaixa.Proxy
dotnet run
```

### Debug
O proxy expõe informações de debug em desenvolvimento:
- Headers `X-Debug-*` com informações internas
- Logs verbose para troubleshooting
- Swagger UI disponível em `/swagger`

## Arquitetura

```
Internet → [Proxy YARP] → [Load Balancer] → [APIs]
              ↓
          [Cache Layer]
              ↓
          [Security Layer]
              ↓
          [Rate Limiting]
              ↓
          [Circuit Breaker]
```

### Componentes

1. **RateLimitingMiddleware**: Controle de taxa de requisições
2. **CacheMiddleware**: Cache inteligente com TTL
3. **SecurityMiddleware**: Proteção contra ataques
4. **ConfiguracaoYarpProvider**: Configuração dinâmica do YARP
5. **MonitoramentoContainersService**: Monitoramento e escalabilidade

## Troubleshooting

### Logs Importantes
```bash
# Visualizar logs do proxy
docker logs fluxo-proxy -f

# Logs de rate limiting
grep "Rate limit exceeded" logs/

# Logs de cache
grep "Cache" logs/

# Logs de segurança
grep "Security violation" logs/
```

### Problemas Comuns

**502 Bad Gateway**: Verifique se as APIs estão rodando e saudáveis
```bash
curl http://localhost/health
```

**429 Too Many Requests**: Rate limiting ativo, aguarde ou revise limites
```bash
# Verificar configuração de rate limiting
grep -A 10 "RateLimiting" appsettings.json
```

**SSL/TLS Issues**: Verifique certificados em `./proxy/certs/`
```bash
ls -la ./proxy/certs/
```
