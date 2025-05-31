# RProg.FluxoCaixa.Lancamentos API

API responsável pelo gerenciamento de lançamentos financeiros, seguindo os padrões de Clean Architecture, CQRS e boas práticas de desenvolvimento .NET.

## Características

- **Padrão CQRS** com MediatR para queries e comandos
- **Validação de dados** e regras de negócio centralizadas
- **Performance otimizada** com Dapper para acesso a dados
- **Documentação XML** para controllers e DTOs
- **Configurações** centralizadas no appsettings.json
- **Logging** estruturado com Serilog
- **Integração com RabbitMQ** para eventos e mensageria
- **Testes unitários** com xUnit, Moq e FluentAssertions

## Arquitetura

O projeto segue os princípios de Clean Architecture:

```
Domain/
├── Entities/           # Entidades de domínio
Application/
├── DTOs/              # Data Transfer Objects
├── Commands/          # Comandos CQRS
├── Queries/           # Queries CQRS
Infrastructure/
├── Data/              # Repositórios e acesso a dados
Controllers/           # Controllers da API REST
```

Principais padrões e práticas:
- SOLID, KISS, DRY
- Injeção de dependência
- Repositório, CQRS, Mediator
- Logging estruturado
- Mensageria com RabbitMQ

## Configuração

### Banco de Dados
Configure a connection string no `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FluxoCaixa;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### RabbitMQ
Configure a conexão com o RabbitMQ no `appsettings.json`:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### Logging
O projeto utiliza Serilog configurado para:
- Console output em desenvolvimento
- Arquivo de log com rotação diária
- Logs estruturados em JSON

## Execução

### Desenvolvimento Local (.NET)
```cmd
# Compilar
dotnet build

# Executar
dotnet run

# Executar testes
dotnet test

# Acessar Swagger
# Navegue para https://localhost:7001/swagger
```

### Docker

#### Usando Docker Compose
```cmd
docker-compose -f docker-compose.dev.yaml up --build
```

**URLs Docker:**
- API: http://localhost:8082
- Swagger: http://localhost:8082/swagger
- Health Check: http://localhost:8082/health

## Dependências

- **.NET 8.0**
- **MediatR** - Padrão CQRS
- **Dapper** - ORM leve para performance
- **Serilog** - Logging estruturado
- **Microsoft.Data.SqlClient** - Acesso ao SQL Server
- **Swashbuckle** - Documentação Swagger
- **RabbitMQ.Client** - Integração com RabbitMQ
- **AspNetCore.HealthChecks.SqlServer** e **AspNetCore.HealthChecks.Rabbitmq**

## Padrões e Boas Práticas

- Seguir padrões de codificação C# e nomenclatura conforme instruções do repositório
- Utilizar injeção de dependência sempre que possível
- Separar código em métodos coesos e pequenos
- Utilizar comentários XML e explicativos para regras de negócio e integrações
- Utilizar princípios SOLID, KISS e DRY
- Utilizar repositórios para abstração de dados
- Facilitar a criação de testes unitários

## Testes

- Testes unitários obrigatórios para todo novo código
- Utilizar xUnit, Moq, Bogus e FluentAssertions
- Estruturar testes com AAA (Arrange, Act, Assert) e Given/When/Then
- Mocks para dependências externas
- Projeto de testes: `RProg.FluxoCaixa.Lancamentos.Test`

## Links Úteis

- [Especificação de arquitetura](../../docs/documento-arquitetural.md)
- [Diagrama de containers](../../docs/C4DiagramaContainer.png)
- [Diagrama de contexto](../../docs/C4DiagramaContexto.png)

---

> Para dúvidas sobre padrões, consulte o arquivo `.github/instructions/copilot.instructions.md`.
