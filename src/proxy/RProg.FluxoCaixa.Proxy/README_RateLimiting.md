# Rate Limiting Configuration

Este documento descreve as opções de configuração disponíveis para o middleware de Rate Limiting.

## Configuração Básica

As configurações de Rate Limiting são definidas na seção `RateLimiting` dos arquivos `appsettings.json`:

```json
{
  "RateLimiting": {
    "LimitePorMinuto": 60,
    "LimitePorSegundo": 10,
    "JanelaTempo": "00:01:00",
    "TempoBloqueio": "00:05:00",
    "IntervaloLimpeza": "00:05:00",
    "Habilitado": true,
    "IpsIsentos": [
      "127.0.0.1",
      "::1"
    ]
  }
}
```

## Opções de Configuração

### LimitePorMinuto
- **Tipo**: `int`
- **Obrigatório**: Sim
- **Descrição**: Número máximo de requisições permitidas por minuto por IP
- **Valor mínimo**: 1

### LimitePorSegundo
- **Tipo**: `int`
- **Obrigatório**: Sim
- **Descrição**: Número máximo de requisições permitidas por segundo por IP
- **Valor mínimo**: 1

### JanelaTempo
- **Tipo**: `TimeSpan`
- **Obrigatório**: Sim
- **Descrição**: Janela de tempo para contagem de requisições
- **Formato**: "HH:mm:ss" (ex: "00:01:00" para 1 minuto)
- **Valor mínimo**: 00:00:01

### TempoBloqueio
- **Tipo**: `TimeSpan`
- **Obrigatório**: Sim
- **Descrição**: Tempo que um IP fica bloqueado após exceder o limite
- **Formato**: "HH:mm:ss" (ex: "00:05:00" para 5 minutos)
- **Valor mínimo**: 00:00:01

### IntervaloLimpeza
- **Tipo**: `TimeSpan`
- **Obrigatório**: Sim
- **Descrição**: Intervalo para limpeza automática de entradas antigas
- **Formato**: "HH:mm:ss" (ex: "00:05:00" para 5 minutos)
- **Valor mínimo**: 00:00:01

### Habilitado
- **Tipo**: `bool`
- **Obrigatório**: Sim
- **Descrição**: Define se o rate limiting está ativo
- **Padrão**: `true`

### IpsIsentos
- **Tipo**: `List<string>`
- **Obrigatório**: Não
- **Descrição**: Lista de IPs que são isentos do rate limiting
- **Formato**: Array de strings com IPs válidos (IPv4 ou IPv6)

## Configurações por Ambiente

### Desenvolvimento (appsettings.Development.json)
```json
{
  "RateLimiting": {
    "LimitePorMinuto": 120,
    "LimitePorSegundo": 20,
    "TempoBloqueio": "00:01:00",
    "IntervaloLimpeza": "00:02:00"
  }
}
```

### Produção (appsettings.Production.json)
```json
{
  "RateLimiting": {
    "LimitePorMinuto": 60,
    "LimitePorSegundo": 10,
    "TempoBloqueio": "00:15:00",
    "IntervaloLimpeza": "00:10:00"
  }
}
```

## Validação

As configurações são automaticamente validadas na inicialização da aplicação. Se algum valor for inválido, a aplicação não iniciará e mostrará uma mensagem de erro específica.

## Headers de Resposta

O middleware adiciona os seguintes headers nas respostas:

- `X-RateLimit-Limit`: Limite de requisições por minuto
- `X-RateLimit-Remaining`: Número de requisições restantes na janela atual
- `X-RateLimit-Reset`: Timestamp de quando o limite será resetado

## Detecção de IP

O middleware detecta o IP do cliente na seguinte ordem de prioridade:

1. Header `X-Forwarded-For` (primeiro IP da lista)
2. Header `X-Real-IP`
3. `HttpContext.Connection.RemoteIpAddress`

## Logs

O middleware registra as seguintes informações:

- Quando um IP é bloqueado por exceder o limite
- Erros durante o processamento
- Informações de debug sobre contadores de requisições (em modo Debug)
