# Docker Configuration - RProg.FluxoCaixa.Proxy

## ✅ Configuração Completa

O Dockerfile para o projeto YARP Proxy foi configurado corretamente no diretório `src/proxy/`.

### 📁 Estrutura dos Arquivos Docker

```
src/proxy/
├── Dockerfile              # ✅ Dockerfile otimizado para produção
├── .dockerignore           # ✅ Arquivo para otimizar build
└── RProg.FluxoCaixa.Proxy/ # 📂 Código fonte do projeto
```

### 🐳 Dockerfile - Características Principais

**Estágios Multi-Stage Build:**
1. **Base**: Runtime .NET 8 com usuário não-root para segurança
2. **Build**: SDK .NET 8 para compilação
3. **Publish**: Publicação otimizada com ReadyToRun
4. **Final**: Imagem de produção mínima

**Funcionalidades Incluídas:**
- ✅ **Segurança**: Usuário não-root (`appuser`)
- ✅ **Docker CLI**: Para monitoramento de containers
- ✅ **Health Check**: Endpoint `/health` com verificação automática
- ✅ **Otimizações**: Cache de layers, publish otimizado
- ✅ **Logging**: Diretórios para logs persistentes
- ✅ **Variáveis de Ambiente**: Configurações específicas do YARP

**Portas Expostas:**
- `8080`: HTTP (principal)
- `8443`: HTTPS (opcional)

### 🚫 .dockerignore - Arquivos Excluídos

O `.dockerignore` está configurado para excluir:
- Diretórios de build (`bin/`, `obj/`)
- Arquivos de configuração de IDE
- Logs e arquivos temporários
- Documentação e scripts de desenvolvimento
- Arquivos de teste e coverage
- Certificados e chaves sensíveis

### 🔧 Como Usar

**Build da Imagem:**
```bash
cd src/proxy
docker build -t rprog-fluxocaixa-proxy:latest .
```

**Executar Container:**
```bash
docker run -d \
  --name fluxocaixa-proxy \
  -p 8080:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  rprog-fluxocaixa-proxy:latest
```

**Com Variáveis de Ambiente:**
```bash
docker run -d \
  --name fluxocaixa-proxy \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e YARP_LOG_LEVEL=Information \
  -e CACHE_ENABLED=true \
  -v /var/run/docker.sock:/var/run/docker.sock \
  rprog-fluxocaixa-proxy:latest
```

### 🎯 Integração com Docker Compose

O proxy pode ser facilmente integrado ao `docker-compose.yaml` principal do projeto:

```yaml
services:
  proxy:
    build:
      context: ./proxy
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - YARP_ENABLE_HEALTH_CHECKS=true
    depends_on:
      - lancamentos
      - consolidado
```

### 📊 Benefícios da Configuração

1. **Performance**: Build otimizado com cache de layers
2. **Segurança**: Usuário não-root e exclusão de arquivos sensíveis
3. **Monitoramento**: Health checks automáticos
4. **Flexibilidade**: Configuração via variáveis de ambiente
5. **Produção Ready**: Otimizações específicas para ambiente produtivo

### 🔍 Validação

A configuração está pronta para:
- ✅ Build em ambiente de desenvolvimento
- ✅ Deploy em ambiente de produção
- ✅ Integração com orchestração (Docker Compose/Kubernetes)
- ✅ Monitoramento de containers Docker via Docker API
- ✅ Cache e otimizações de performance
