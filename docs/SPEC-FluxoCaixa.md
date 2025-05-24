# SPEC-1: Arquitetura para Controle de Fluxo de Caixa com Balanço Diário

## Background
Um comerciante precisa de um sistema para registrar o fluxo de caixa diário através de lançamentos financeiros categorizados como débitos e créditos. Além disso, ele necessita de um relatório diário consolidado que mostre o saldo acumulado com base nesses lançamentos. 

A arquitetura da solução deve garantir escalabilidade, resiliência, segurança e alto desempenho, assegurando que o serviço de controle de lançamentos continue disponível mesmo em caso de falha no serviço de consolidação. O sistema deverá suportar uma carga de até 50 requisições por segundo no serviço de relatório, permitindo até 5% de perda durante picos de acesso. 

O foco do desafio é evidenciar boas práticas de arquitetura de software, incluindo uso de padrões arquiteturais, integração entre componentes, segurança na troca e persistência de dados, além de documentar de forma clara as decisões arquiteturais tomadas. 

## Requirements 

### Funcionais

#### **Must Have**

- Registrar lançamentos financeiros do tipo débito ou crédito.
- Consultar relatório consolidado diário de saldo.
- Separação entre serviços de lançamento e consolidação.
- Disponibilidade do serviço de lançamento independentemente do status do serviço de consolidação.

#### **Should Have**

- Suporte a múltiplos lançamentos simultâneos.
- Categorização básica dos lançamentos.
- Histórico dos lançamentos acessível por data.

#### **Could Have**

- Exportação do relatório consolidado (ex: CSV, PDF).
- Interface web para visualização dos dados.

#### **Won't Have**

- Multiusuário ou multiempresa.
- Integração com sistemas externos.

### Não Funcionais

#### **Must Have**

- Suporte a 50 rps no consolidado com até 5% de perda.
- Serviço de lançamentos sempre disponível.
- Segurança: JWT, TLS, validação.
- Uso de padrões SOLID, boas práticas.

#### **Should Have**

- Projeto em C# com testes automatizados e README.
- Exposição pública apenas do API Gateway.


#### **Could Have**

- Observabilidade, health checks, logs estruturados.

## Implementation

### Estrutura de Pastas Sugerida

Adotaremos uma estrutura de pastas organizada conforme as convenções .NET e com foco em clareza e separação entre documentação, código-fonte e testes.

```text
/RProg.FluxoCaixa
|-- docs/
|   |-- c4contexto.png
|   |-- c4container.png
|   |-- documento-arquitetural.md
|   |-- SPEC-FluxoCaixa.md
|-- src/
|   |-- proxy/
|   |   |-- RProg.FluxoCaixa.Proxy/
|   |   |-- RProg.FluxoCaixa.Proxy.Test/
|   |-- lancamentos/
|   |   |-- RProg.FluxoCaixa.Lancamentos/
|   |   |-- RProg.FluxoCaixa.Lancamentos.Test/
|   |-- consolidado/
|   |   |-- RProg.FluxoCaixa.Consolidado/
|   |   |-- RProg.FluxoCaixa.Consolidado.Test/
|   |-- worker/
|   |   |-- RProg.FluxoCaixa.Worker/
|   |   |-- RProg.FluxoCaixa.Worker.Test/
|   |-- docker-compose.yml
|   |-- README.md
|-- README.md
|-- RProg.FluxoCaixa.sln
```

### Etapas de Implementação

1. **Configuração do repositório**
2. **Serviço de Lançamentos**
3. **Serviço de Consolidação (API)**
4. **Worker de Consolidação**
5. **API Gateway**
6. **Automação com Docker**
7. **Segurança e Observabilidade**
8. **Documentação**

### Testes e Validação

- Cobertura mínima de 80%
- Testes de integração via docker-compose
- Testes de carga no endpoint do consolidado

## Milestones

### 1. Inicialização
- Criação do repositório no GitHub
- Geração do solution file

### 2. Base de Lançamentos
- Implementação e testes
- Publicação de eventos

### 3. Base de Consolidação
- Implementação e leitura dos saldos

### 4. Worker
- Consumo de fila e idempotência

### 5. API Gateway
- Roteamento e segurança

### 6. Docker e Execução Local
- Dockerfile e docker-compose

### 7. Documentação Final
- Documento arquitetural e diagramas

## Gathering Results

### Critérios de Sucesso

- Disponibilidade dos lançamentos
- 50 rps com no máximo 5% de perda no consolidado
- Cobertura mínima de testes
- Gateway único exposto

### Indicadores Técnicos

- Tempo de resposta médio
- Percentual de perda
- Tempo de startup

### Feedbacks Esperados

- Facilidade de onboarding
- Clareza de responsabilidades
- Facilidade de manutenção