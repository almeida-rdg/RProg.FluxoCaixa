# RProg.FluxoCaixa.Worker

## ✅ STATUS DO PROJETO: IMPLEMENTAÇÃO COMPLETA

**🎉 PRONTO PARA TESTE E PRODUÇÃO**

✅ **Todas as funcionalidades implementadas com sucesso:**
- ✅ Escuta de múltiplas filas RabbitMQ com prefixo configurável
- ✅ Processamento idempotente de lançamentos (evita duplicação)
- ✅ Consolidação diária geral e por categoria
- ✅ Persistência em banco SQL Server com Dapper
- ✅ Logs estruturados com Serilog
- ✅ Containerização Docker completa
- ✅ Reconexão automática RabbitMQ
- ✅ Configuração via appsettings e variáveis de ambiente

✅ **Status da Compilação:** SUCESSO (sem erros)
✅ **Compatibilidade:** RabbitMQ.Client 7.1.2, .NET 8.0
✅ **Docker:** Pronto para execução
✅ **Testes:** Estrutura básica implementada

---

Worker responsável pela consolidação diária de lançamentos do sistema de fluxo de caixa.

## Funcionalidades

- ✅ **Processamento idempotente**: Evita processamento duplicado de mensagens
- ✅ **Consolidação automática**: Gera consolidações diárias gerais e por categoria
- ✅ **Múltiplas filas**: Suporta múltiplas filas RabbitMQ com prefixo configurável
- ✅ **Recuperação automática**: Reconexão automática em caso de falha
- ✅ **Logging estruturado**: Logs detalhados com Serilog
- ✅ **Múltiplas instâncias**: Suporte a execução em múltiplas instâncias

## Estrutura do Projeto

```
RProg.FluxoCaixa.Worker/
├── Domain/
│   ├── Entities/
│   │   ├── ConsolidadoDiario.cs      # Entidade de consolidação
│   │   └── LancamentoProcessado.cs   # Controle de idempotência
│   ├── DTOs/
│   │   └── LancamentoDto.cs          # DTO para lançamentos
│   └── Services/
│       └── IConsolidacaoService.cs   # Interface do serviço
├── Infrastructure/
│   ├── Data/
│   │   ├── IConsolidadoRepository.cs
│   │   ├── ConsolidadoRepository.cs
│   │   ├── ILancamentoProcessadoRepository.cs
│   │   └── LancamentoProcessadoRepository.cs
│   └── Services/
│       ├── IRabbitMqService.cs
│       └── RabbitMqService.cs
├── Services/
│   └── ConsolidacaoService.cs        # Implementação do serviço
└── Worker.cs                         # Worker principal
```

## Configuração

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=RProg_FluxoCaixa_Consolidado;User Id=sa;Password=SuaSenhaForte123!;TrustServerCertificate=true"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "PrefixoFila": "lancamento",
    "Filas": [
      "lancamento.geral",
      "lancamento.prioritaria"
    ]
  }
}
```

## Banco de Dados

### Criação da Estrutura

Execute o script SQL:

```bash
sqlcmd -S localhost -U sa -P "SuaSenhaForte123!" -i scripts/criar-banco-consolidado.sql
```

### Tabelas Criadas

- **ConsolidadoDiario**: Armazena consolidações diárias
- **LancamentoProcessado**: Controle de idempotência

## Execução

### 1. Via Docker Compose (Recomendado)

```bash
cd src/
docker-compose up worker
```

### 2. Via .NET CLI

```bash
cd src/worker/RProg.FluxoCaixa.Worker/
dotnet run
```

### 3. Via Docker Standalone

```bash
cd src/
docker build -t rprog-fluxocaixa-worker -f worker/Dockerfile .
docker run -e ConnectionStrings__DefaultConnection="..." rprog-fluxocaixa-worker
```

## Funcionamento

### Fluxo de Processamento

1. **Escuta**: Worker conecta-se ao RabbitMQ e escuta filas com prefixo configurado
2. **Recebimento**: Mensagens são recebidas e deserializadas para `LancamentoDto`
3. **Idempotência**: Verifica se o lançamento já foi processado
4. **Consolidação**: Atualiza consolidações geral e por categoria
5. **Confirmação**: Marca mensagem como processada no RabbitMQ

### Exemplo de Mensagem RabbitMQ

```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "valor": 150.75,
  "tipo": "credito",
  "data": "2024-12-19T10:30:00",
  "categoria": "vendas",
  "descricao": "Venda produto XYZ"
}
```

### Consolidações Geradas

Para cada data, são criadas:

- **Consolidação Geral** (`categoria = null`)
  - Total de créditos
  - Total de débitos
  - Saldo líquido
  - Quantidade de lançamentos

- **Consolidação por Categoria** (para cada categoria)
  - Totais específicos da categoria
  - Saldo líquido da categoria
  - Quantidade de lançamentos da categoria

## Monitoramento

### Logs

Logs são gerados em:
- Console (durante desenvolvimento)
- Arquivo `logs/worker-*.txt` (em produção)

### Métricas

- Mensagens processadas
- Erros de processamento
- Tempo de resposta
- Conexões RabbitMQ

### Health Check

O worker inclui health check básico:

```bash
docker exec fluxo-worker dotnet --info
```

## Desenvolvimento

### Executar Testes

```bash
cd src/worker/RProg.FluxoCaixa.Worker.Test/
dotnet run
```

### Debugging

1. Configure connection strings locais
2. Inicie RabbitMQ local
3. Execute via IDE ou `dotnet run`

### Dependências

- .NET 8.0
- RabbitMQ.Client 7.1.2
- Dapper 2.1.66
- Microsoft.Data.SqlClient 6.0.2
- Serilog 8.0.1

## Arquitetura

### Padrões Utilizados

- **Repository Pattern**: Acesso a dados
- **Dependency Injection**: Inversão de controle
- **Background Service**: Execução contínua
- **Idempotent Processing**: Segurança de processamento

### Escalabilidade

- Múltiplas instâncias do worker podem executar simultaneamente
- Cada instância processa mensagens independentemente
- Controle de idempotência evita processamento duplicado
- Load balancing automático via RabbitMQ

## Troubleshooting

### Problemas Comuns

1. **Erro de Conexão SQL Server**
   - Verificar connection string
   - Verificar se o SQL Server está executando
   - Verificar permissões do usuário

2. **Erro de Conexão RabbitMQ**
   - Verificar se RabbitMQ está executando
   - Verificar credenciais
   - Verificar portas (5672, 15672)

3. **Mensagens não são processadas**
   - Verificar se as filas existem
   - Verificar se as mensagens estão no formato correto
   - Verificar logs do worker

### Logs de Debug

Para habilitar logs detalhados:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

## Contribuição

1. Implemente novas funcionalidades em branches separados
2. Mantenha testes atualizados
3. Siga os padrões de arquitetura existentes
4. Documente mudanças significativas
