# RProg.FluxoCaixa

## Objetivo

A aplicação **RProg.FluxoCaixa** foi projetada para atender comerciantes que precisam registrar lançamentos financeiros diários (créditos e débitos) e consultar o saldo consolidado por dia. A arquitetura garante alta disponibilidade, resiliência, segurança e escalabilidade, separando os serviços de forma independente e permitindo comunicação assíncrona entre os módulos.

O sistema foi construído com foco em boas práticas de engenharia de software, testes, observabilidade e facilidade de evolução futura.

## Estrutura do Projeto

```
RProg.FluxoCaixa/
├── docs/                         # Documentação arquitetural e diagramas
├── src/
│   ├── proxy/                    # Proxy reverso (YARP)
│   │   ├── RProg.FluxoCaixa.Proxy/         # Código-fonte do proxy
│   │   └── RProg.FluxoCaixa.Proxy.Test/   # Testes do proxy
│   ├── lancamentos/              # API de lançamentos financeiros
│   │   ├── RProg.FluxoCaixa.Lancamentos/        # Código-fonte
│   │   └── RProg.FluxoCaixa.Lancamentos.Test/  # Testes
│   ├── consolidado/              # API de consolidação de saldos
│   │   ├── RProg.FluxoCaixa.Consolidado/        # Código-fonte
│   │   └── RProg.FluxoCaixa.Consolidado.Test/  # Testes
│   ├── worker/                   # Worker de consolidação assíncrona
│   │   ├── RProg.FluxoCaixa.Worker/            # Código-fonte
│   │   └── RProg.FluxoCaixa.Worker.Test/      # Testes
│   ├── scripts/                  # Scripts auxiliares e de banco de dados
│   └── docker-compose.yaml       # Orquestração local dos serviços
├── Directory.Packages.props      # Gerenciamento centralizado de pacotes NuGet
├── nuget.config                  # Configuração de cache NuGet
├── README.md                     # Este arquivo
└── RProg.FluxoCaixa.slnx         # Solution principal
```

## Componentes

- **Proxy (YARP)**: Balanceamento de carga, autenticação JWT, rate limiting, proteção e roteamento para as APIs de Lançamentos e Consolidação.
- **API de Lançamentos**: Gerenciamento de lançamentos financeiros, integração com RabbitMQ, validação e regras de negócio.
- **API de Consolidação**: Consulta de saldos consolidados por período e categoria, queries otimizadas, validação de parâmetros.
- **Worker**: Processamento assíncrono de lançamentos, consolidação diária, idempotência e integração com RabbitMQ e SQL Server.
- **Infraestrutura**: Orquestração via Docker Compose, scripts de banco de dados, logs estruturados, health checks e métricas.

## Execução do Projeto

### Ambiente Completo (Recomendado)

```cmd
cd src/
docker-compose up --build
```

### Execução Individual

Cada serviço possui instruções detalhadas em seu respectivo `README.md`:
- [Proxy](src/proxy/README.md)
- [Lançamentos](src/lancamentos/README.md)
- [Consolidado](src/consolidado/README.md)
- [Worker](src/worker/README.md)

### URLs Padrão

- Proxy: http://localhost:8080
- Lançamentos API: http://localhost:8081
- Consolidado API: http://localhost:8082
- Swagger: `/swagger` em cada serviço
- Health Check: `/api/health` ou `/health` em cada serviço

## Padrões e Boas Práticas

- Padrão de codificação C# e nomenclatura conforme instruções do repositório
- Princípios SOLID, KISS, DRY
- Injeção de dependência
- Testes unitários obrigatórios (xUnit, Moq, Bogus, FluentAssertions)
- Separação de responsabilidades por contexto de domínio
- Documentação XML e comentários explicativos
- Observabilidade: logs estruturados, health checks, métricas
- Orquestração e automação via Docker Compose

## Documentação

A arquitetura, requisitos e decisões técnicas estão descritos em:
- [docs/documento-arquitetural.md](docs/documento-arquitetural.md)
- [docs/SPEC-FluxoCaixa.md](docs/SPEC-FluxoCaixa.md)
- [docs/C4DiagramaContainer.png](docs/C4DiagramaContainer.png)
- [docs/C4DiagramaContexto.png](docs/C4DiagramaContexto.png)

## Otimizações e Infraestrutura

- Gerenciamento centralizado de pacotes NuGet (`Directory.Packages.props`)
- Cache de pacotes compartilhado entre projetos
- Dockerfiles otimizados para build e produção
- Scripts para automação de restore, build e testes

## Contribuição

- Siga os padrões definidos em `.github/instructions/copilot.instructions.md`
- Documente código e regras de negócio
- Escreva testes para todo novo código
- Utilize branches em `kebab-case` e pull requests para revisão

---

> Para dúvidas sobre padrões, consulte o arquivo `.github/instructions/copilot.instructions.md`.