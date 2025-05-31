# RProg.FluxoCaixa.Proxy

Proxy reverso implementado com YARP para balanceamento de carga, proteção, autenticação JWT, rate limiting e monitoramento das APIs de Lançamentos e Consolidado.

## Funcionalidades

- **Balanceamento de carga** entre múltiplas instâncias backend (YARP, round-robin)
- **Health checks** contínuos para APIs de destino
- **Rate limiting** configurável por IP (AspNetCoreRateLimit)
- **Proteção contra ataques**: headers de segurança, validação de input, CORS
- **Autenticação JWT** para rotas protegidas
- **Logs estruturados** com Serilog (console e arquivo)
- **Monitoramento de saúde** via endpoints padrão
- **Swagger/OpenAPI** para documentação

## Arquitetura

```
Internet → [Proxy YARP] → [APIs Lançamentos/Consolidado]
```

- Middleware de segurança, rate limiting e autenticação aplicados antes do roteamento YARP.
- Health checks e métricas expostos via endpoints.

## Configuração

### Variáveis de Ambiente

```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:80;https://+:443
Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
Kestrel__Certificates__Default__Password=YourSecurePassword
```

### Exemplo de Configuração YARP (appsettings.json)

```json
{
  "ReverseProxy": {
    "Routes": {
      "lancamentos-route": {
        "ClusterId": "lancamentos-cluster",
        "Match": { "Path": "/api/lancamentos/{**catch-all}" }
      },
      "consolidado-route": {
        "ClusterId": "consolidado-cluster",
        "Match": { "Path": "/api/consolidado/{**catch-all}" }
      }
    },
    "Clusters": {
      "lancamentos-cluster": {
        "Destinations": {
          "lancamentos-api-1": { "Address": "http://lancamentos-api:8080/" }
        },
        "HealthCheck": {
          "Enabled": true,
          "Interval": "00:00:30",
          "Timeout": "00:00:10",
          "Policy": "ConsecutiveFailures",
          "Path": "/health"
        }
      },
      "consolidado-cluster": {
        "Destinations": {
          "consolidado-api-1": { "Address": "http://consolidado-api:8080/" }
        },
        "HealthCheck": {
          "Enabled": true,
          "Interval": "00:00:30",
          "Timeout": "00:00:10",
          "Policy": "ConsecutiveFailures",
          "Path": "/health"
        }
      }
    }
  }
}
```

### Exemplo de Rate Limiting (appsettings.json)

```json
{
  "RateLimiting": {
    "LimitePorMinuto": 60,
    "LimitePorSegundo": 10,
    "JanelaTempo": "00:01:00",
    "TempoBloqueio": "00:05:00",
    "IntervaloLimpeza": "00:05:00",
    "Habilitado": true,
    "IpsIsentos": ["127.0.0.1", "::1"]
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

## Execução

### Desenvolvimento Local (.NET)
```cmd
# Compilar
dotnet build

# Executar
dotnet run --project src/proxy/RProg.FluxoCaixa.Proxy/RProg.FluxoCaixa.Proxy.csproj

# Executar testes
dotnet test src/proxy/RProg.FluxoCaixa.Proxy.Test/

# Acessar Swagger
# Navegue para http://localhost:8080/swagger
```

### Docker

#### Usando Docker Compose
```cmd
# Subir apenas o proxy e dependências
docker-compose -f docker-compose.proxy.yaml up --build

# Subir todo o ambiente
docker-compose up --build
```

**URLs Docker:**
- Proxy: http://localhost:8080
- Swagger: http://localhost:8080/swagger
- Health Check: http://localhost:8080/health

## Endpoints

- `GET /api/lancamentos/*` → Lançamentos API
- `GET /api/consolidado/*` → Consolidado API
- `GET /health` → Health check do proxy
- `GET /health/ready` → Readiness probe
- `GET /health/live` → Liveness probe
- `GET /swagger` → Documentação OpenAPI

## Segurança

- **Rate Limiting**: Limites configuráveis por IP
- **Proteção**: Headers de segurança, validação de input, CORS
- **Autenticação JWT**: Bearer tokens para rotas protegidas

## Monitoramento

- Logs estruturados (Serilog)
- Health checks detalhados

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

## Dependências Principais

- **.NET 8.0**
- **YARP** (Yarp.ReverseProxy)
- **Serilog** (console, arquivo)
- **Swashbuckle.AspNetCore** (Swagger)
- **AspNetCoreRateLimit** (rate limiting)
- **Microsoft.AspNetCore.Authentication.JwtBearer** (JWT)
- **Microsoft.Extensions.Diagnostics.HealthChecks**

## Referências e Links Úteis

- [Documentação YARP](https://microsoft.github.io/reverse-proxy/)
- [Documentação Serilog](https://serilog.net/)
- [Documentação oficial .NET](https://docs.microsoft.com/dotnet/)
- [Especificação de arquitetura](../../docs/documento-arquitetural.md)
- [Diagrama de containers](../../docs/C4DiagramaContainer.png)
- [Diagrama de contexto](../../docs/C4DiagramaContexto.png)

---

> Para dúvidas sobre padrões, consulte o arquivo `.github/instructions/copilot.instructions.md`.
