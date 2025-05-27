# RProg.FluxoCaixa.Consolidado API

API para consulta de dados consolidados por período e categoria utilizando padrão CQRS.

## Características

- ✅ **Padrão CQRS** com MediatR para queries otimizadas
- ✅ **Validação de período** - data final não pode ser inferior à inicial  
- ✅ **Último horário de consolidação** retornado em cada consulta
- ✅ **Performance otimizada** com queries SQL diretas via Dapper
- ✅ **Documentação XML** completa para controllers e DTOs
- ✅ **Configurações** centralizadas no appsettings.json
- ✅ **Logging** estruturado com Serilog
- ✅ **Testes unitários** com xUnit, Moq e FluentAssertions

## Endpoints

### GET /api/consolidado
Consulta dados consolidados por período.

**Parâmetros:**
- `dataInicial` (DateTime): Data inicial do período
- `dataFinal` (DateTime): Data final do período

**Exemplo:**
```http
GET /api/consolidado?dataInicial=2024-01-01&dataFinal=2024-01-31
```

### GET /api/consolidado/categoria/{categoria}
Consulta dados consolidados por período e categoria específica.

**Parâmetros:**
- `categoria` (string): Categoria específica para filtro
- `dataInicial` (DateTime): Data inicial do período
- `dataFinal` (DateTime): Data final do período

**Exemplo:**
```http
GET /api/consolidado/categoria/ALIMENTACAO?dataInicial=2024-01-01&dataFinal=2024-01-31
```

## Resposta

```json
{
  "dataInicial": "2024-01-01T00:00:00",
  "dataFinal": "2024-01-31T00:00:00",
  "consolidados": [
    {
      "data": "2024-01-01T00:00:00",
      "categoria": "ALIMENTACAO",
      "tipoConsolidacao": "CATEGORIA",
      "totalCreditos": 0.00,
      "totalDebitos": -150.75,
      "saldoLiquido": -150.75,
      "quantidadeLancamentos": 3,
      "dataAtualizacao": "2024-01-01T18:30:00"
    }
  ],
  "ultimaConsolidacao": "2024-01-01T18:30:00",
  "totalRegistros": 1
}
```

## Configuração

### Banco de Dados
Configure a connection string no `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=RProg_FluxoCaixa;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### Logging
O projeto utiliza Serilog configurado para:
- Console output em desenvolvimento
- Arquivo de log com rotação diária
- Logs estruturados em JSON

## Execução

```bash
# Compilar
dotnet build

# Executar
dotnet run

# Executar testes
dotnet test

# Acessar Swagger
# Navegue para https://localhost:7002/
```

## Arquitetura

O projeto segue os princípios de Clean Architecture:

```
Domain/
├── Entities/           # Entidades de domínio
Application/
├── DTOs/              # Data Transfer Objects
├── Queries/           # Queries CQRS com handlers
Infrastructure/
├── Data/              # Repositórios e acesso a dados
Controllers/           # Controllers da API REST
```

## Dependências

- **.NET 8.0**
- **MediatR** - Padrão CQRS
- **Dapper** - ORM leve para performance
- **Serilog** - Logging estruturado
- **Microsoft.Data.SqlClient** - Acesso ao SQL Server
- **Swashbuckle** - Documentação Swagger

## Validações

- Data final deve ser maior ou igual à data inicial
- Categoria não pode ser vazia quando especificada na rota
- Parâmetros de data são obrigatórios
- Tratamento de exceções com logging detalhado
