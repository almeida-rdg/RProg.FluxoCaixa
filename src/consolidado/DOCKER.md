# Docker - API Consolidado

Este documento descreve como executar a API Consolidado usando Docker.

## 🐳 Configuração Docker

### Ambiente de Desenvolvimento

Para desenvolvimento local com apenas a API Consolidado:

```cmd
cd o:\source\RProg.FluxoCaixa\src\consolidado
docker-compose -f docker-compose.dev.yaml up --build
```

**Serviços incluídos:**
- **SQL Server** (porta 1434): Banco de dados isolado para desenvolvimento
- **API Consolidado** (porta 8081): API de consulta dos dados consolidados

**URLs disponíveis:**
- API: http://localhost:8081
- Swagger: http://localhost:8081/swagger
- Health Check: http://localhost:8081/health

### Ambiente Completo (Produção)

Para executar junto com todos os outros serviços:

```cmd
cd o:\source\RProg.FluxoCaixa\src
docker-compose up --build
```

## 🔧 Scripts de Desenvolvimento

### Windows (PowerShell/CMD)

Para facilitar o desenvolvimento, você pode usar os seguintes comandos:

```cmd
# Iniciar serviços
docker-compose -f docker-compose.dev.yaml up --build -d

# Ver logs
docker-compose -f docker-compose.dev.yaml logs -f consolidado-api-dev

# Parar serviços
docker-compose -f docker-compose.dev.yaml down

# Limpar volumes (reset completo)
docker-compose -f docker-compose.dev.yaml down -v
docker system prune -f
```

### Linux/Mac (usando script dev.sh)

```bash
# Tornar o script executável
chmod +x dev.sh

# Iniciar serviços
./dev.sh start

# Ver logs
./dev.sh logs

# Executar testes
./dev.sh test

# Parar serviços
./dev.sh stop

# Limpar tudo
./dev.sh cleanup
```

## 🏗️ Arquitetura de Redes

A aplicação usa redes Docker isoladas para segurança:

### Desenvolvimento (docker-compose.dev.yaml)
- **consolidado-dev-net**: Comunicação entre API e banco
- **db-dev-net**: Rede do banco de dados

### Produção (docker-compose.yaml)
- **consolidado-net**: Rede isolada da API Consolidado
- **db-net**: Rede compartilhada do banco de dados
- **proxy-net**: Rede do proxy reverso (quando habilitado)

## 📊 Monitoramento

### Health Checks

A aplicação inclui health checks automáticos:

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 60s
```

### Logs

Os logs são persistidos em volumes Docker:

- **Desenvolvimento**: `consolidado_dev_logs`
- **Produção**: `consolidado_logs`

Para acessar logs:

```cmd
# Logs em tempo real
docker-compose -f docker-compose.dev.yaml logs -f consolidado-api-dev

# Logs do volume
docker volume inspect consolidado_consolidado_dev_logs
```

### Métricas de Recursos

Limites configurados para produção:

```yaml
deploy:
  resources:
    limits:
      memory: 512M
    reservations:
      memory: 256M
```

## 🐛 Troubleshooting

### Erro de Conexão com Banco

1. Verificar se o SQL Server está rodando:
   ```cmd
   docker-compose -f docker-compose.dev.yaml ps
   ```

2. Verificar logs do SQL Server:
   ```cmd
   docker-compose -f docker-compose.dev.yaml logs sqlserver-dev
   ```

3. Testar conexão manual:
   ```cmd
   docker exec -it fluxo-sqlserver-dev /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P Dev_password123 -C
   ```

### Erro de Health Check

1. Verificar se a aplicação está respondendo:
   ```cmd
   curl http://localhost:8081/health
   ```

2. Verificar logs da aplicação:
   ```cmd
   docker-compose -f docker-compose.dev.yaml logs consolidado-api-dev
   ```

### Problemas de Build

1. Limpar cache do Docker:
   ```cmd
   docker builder prune -f
   ```

2. Rebuild sem cache:
   ```cmd
   docker-compose -f docker-compose.dev.yaml build --no-cache
   ```

### Portas em Uso

Se as portas estiverem em uso, você pode alterar no docker-compose:

```yaml
ports:
  - "8082:8080"  # Muda para porta 8082
```

## 🔐 Segurança

### Usuário Não-Root

O container roda com usuário `appuser` (não-root) para segurança.

### Secrets

Em produção, substitua senhas por Docker Secrets:

```yaml
secrets:
  db_password:
    file: ./secrets/db_password.txt

services:
  consolidado-api:
    secrets:
      - db_password
```

### Variáveis de Ambiente

Variáveis sensíveis devem ser definidas em arquivo `.env`:

```env
SA_PASSWORD=sua_senha_segura_aqui
ASPNETCORE_ENVIRONMENT=Production
```

## 📈 Performance

### Otimizações Implementadas

1. **Multi-stage build**: Reduz tamanho da imagem final
2. **Health checks**: Monitoramento automático de saúde
3. **Logs estruturados**: Serilog com output otimizado
4. **Limite de recursos**: Evita consumo excessivo de memória
5. **Restart policy**: Auto-restart em caso de falha

### Monitoramento de Performance

```cmd
# Ver uso de recursos
docker stats fluxo-consolidado-dev

# Ver logs de performance
docker-compose -f docker-compose.dev.yaml logs consolidado-api-dev | grep -i "performance\|slow\|timeout"
```
