# Scripts SQL do FluxoCaixa

Esta pasta contém todos os scripts SQL organizados de forma modular para facilitar a manutenção e versionamento do sistema.

## Estrutura de Pastas

```
sql/
├── schemas/                    # Criação dos bancos de dados
│   ├── 01-lancamentos.sql     # Banco FluxoCaixa
│   └── 02-consolidado.sql     # Banco FluxoCaixa_Consolidado
├── tabelas/                   # Criação das tabelas
│   ├── 01-lancamentos.sql     # Tabelas do módulo de lançamentos
│   └── 02-consolidado-tabelas.sql # Tabelas do módulo de consolidação
├── indices/                   # Criação dos índices
│   ├── 01-consolidado-indices-base.sql      # Índices básicos
│   └── 02-consolidado-indices-otimizados.sql # Índices otimizados (opcional)
├── views/                     # Criação das views
│   └── 01-consolidado-views.sql # Views do módulo de consolidação
├── procedures/                # Criação das stored procedures
│   └── 01-consolidado-limpeza.sql # Procedures de manutenção
├── init-databases.sql         # Script mestre de inicialização
└── validate-structure.sql     # Script de validação da estrutura
```

## Scripts Principais

### init-databases.sql
Script mestre que executa todos os scripts na ordem correta. Use este script para inicialização completa dos bancos.

### validate-structure.sql
Script para validar se todas as estruturas foram criadas corretamente após a execução do init-databases.sql.

## Ordem de Execução

1. **Schemas** - Criação dos bancos de dados
2. **Tabelas** - Criação das estruturas de dados
3. **Índices Base** - Índices essenciais para performance
4. **Views** - Views otimizadas para consultas frequentes
5. **Procedures** - Procedures de manutenção e limpeza
6. **Índices Otimizados** - (Opcional) Índices especializados para máxima performance

## Como Usar

### Execução Completa
```sql
-- Executar o script mestre
:r init-databases.sql

-- Validar a estrutura criada
:r validate-structure.sql
```

### Execução Modular
```sql
-- Executar apenas schemas
:r schemas\01-lancamentos.sql
:r schemas\02-consolidado.sql

-- Executar apenas tabelas
:r tabelas\01-lancamentos.sql
:r tabelas\02-consolidado-tabelas.sql
```

## Integração com Docker

O docker-compose.yaml está configurado para executar automaticamente o `init-databases.sql` durante a inicialização do container SQL Server.

```yaml
volumes:
  - ./sql:/docker-entrypoint-initdb.d/sql
```

## Padrões Seguidos

- **Nomenclatura**: PascalCase para objetos SQL, prefixos descritivos
- **Organização**: Um script por responsabilidade/módulo
- **Documentação**: Comentários explicativos em cada script
- **Versionamento**: Estrutura facilita controle de mudanças
- **Idempotência**: Scripts podem ser executados múltiplas vezes sem erro
- **Performance**: Índices organizados por importância (base vs otimizados)

## Manutenção

### Adicionar Nova Funcionalidade
1. Criar script específico na pasta apropriada
2. Atualizar o `init-databases.sql` se necessário
3. Atualizar o `validate-structure.sql` para incluir validações
4. Documentar alterações neste README

### Modificar Estrutura Existente
1. Criar script de migração na pasta apropriada
2. Testar em ambiente de desenvolvimento
3. Atualizar scripts de validação
4. Documentar breaking changes

## Troubleshooting

### Erro de Dependência
Se um script falhar por dependência, verifique:
1. Ordem de execução no `init-databases.sql`
2. Se todos os scripts referenciados existem
3. Se as dependências estão corretas

### Performance
Para otimizar performance em produção:
1. Execute os índices otimizados: `02-consolidado-indices-otimizados.sql`
2. Monitore o uso dos índices com DMVs do SQL Server
3. Ajuste fillfactor conforme padrão de uso

### Validação
Execute sempre o `validate-structure.sql` após mudanças para garantir integridade da estrutura.
