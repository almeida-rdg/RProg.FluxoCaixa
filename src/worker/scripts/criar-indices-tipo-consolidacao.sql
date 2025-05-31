-- Script para criação de índices otimizados para consultas por tipo de consolidação
-- Execução necessária após implementação do parâmetro obrigatório TipoConsolidacao

USE FluxoCaixa_Consolidado;
GO

PRINT 'Iniciando criação de índices otimizados para consultas por tipo de consolidação...';
GO

-- Índice especializado para consultas por período e tipo de consolidação
-- Este índice substitui o IX_ConsolidadoDiario_Periodo_Otimizado com melhor ordenação
IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_TipoConsolidacao_Data_Otimizado')
BEGIN
    DROP INDEX [IX_ConsolidadoDiario_TipoConsolidacao_Data_Otimizado] ON [dbo].[ConsolidadoDiario];
    PRINT 'Índice antigo IX_ConsolidadoDiario_TipoConsolidacao_Data_Otimizado removido.';
END
GO

-- Índice principal para consultas por tipo de consolidação e período
-- Ordem otimizada: TipoConsolidacao primeiro para melhor seletividade
CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_TipoConsolidacao_Data_Otimizado]
ON [dbo].[ConsolidadoDiario] ([TipoConsolidacao] ASC, [Data] ASC)
INCLUDE ([Categoria], [TotalCreditos], [TotalDebitos], [SaldoLiquido], [QuantidadeLancamentos], [DataAtualizacao])
WITH (
    FILLFACTOR = 90,
    PAD_INDEX = ON,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = OFF,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON
);
GO

PRINT 'Índice IX_ConsolidadoDiario_TipoConsolidacao_Data_Otimizado criado com sucesso.';
GO

-- Índice especializado para consultas de última data de atualização por tipo
IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_UltimaAtualizacao_TipoConsolidacao')
BEGIN
    DROP INDEX [IX_ConsolidadoDiario_UltimaAtualizacao_TipoConsolidacao] ON [dbo].[ConsolidadoDiario];
    PRINT 'Índice antigo IX_ConsolidadoDiario_UltimaAtualizacao_TipoConsolidacao removido.';
END
GO

CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_UltimaAtualizacao_TipoConsolidacao]
ON [dbo].[ConsolidadoDiario] ([TipoConsolidacao] ASC, [DataAtualizacao] DESC)
WITH (
    FILLFACTOR = 95,
    PAD_INDEX = ON,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = OFF,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON
);
GO

PRINT 'Índice IX_ConsolidadoDiario_UltimaAtualizacao_TipoConsolidacao criado com sucesso.';
GO

-- Índice covering específico para consolidação GERAL
-- Filtro especializado para performance máxima em consultas de consolidação geral
IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Geral_Covering')
BEGIN
    DROP INDEX [IX_ConsolidadoDiario_Geral_Covering] ON [dbo].[ConsolidadoDiario];
    PRINT 'Índice antigo IX_ConsolidadoDiario_Geral_Covering removido.';
END
GO

CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Geral_Covering]
ON [dbo].[ConsolidadoDiario] ([Data] ASC)
INCLUDE ([TotalCreditos], [TotalDebitos], [SaldoLiquido], [QuantidadeLancamentos], [DataAtualizacao])
WHERE ([TipoConsolidacao] = 'GERAL')
WITH (
    FILLFACTOR = 90,
    PAD_INDEX = ON,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = OFF,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON
);
GO

PRINT 'Índice IX_ConsolidadoDiario_Geral_Covering criado com sucesso.';
GO

-- Índice covering específico para consolidação por CATEGORIA
-- Filtro especializado para performance máxima em consultas por categoria
IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Categoria_Covering')
BEGIN
    DROP INDEX [IX_ConsolidadoDiario_Categoria_Covering] ON [dbo].[ConsolidadoDiario];
    PRINT 'Índice antigo IX_ConsolidadoDiario_Categoria_Covering removido.';
END
GO

CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Categoria_Covering]
ON [dbo].[ConsolidadoDiario] ([Data] ASC, [Categoria] ASC)
INCLUDE ([TotalCreditos], [TotalDebitos], [SaldoLiquido], [QuantidadeLancamentos], [DataAtualizacao])
WHERE ([TipoConsolidacao] = 'CATEGORIA')
WITH (
    FILLFACTOR = 90,
    PAD_INDEX = ON,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = OFF,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON
);
GO

PRINT 'Índice IX_ConsolidadoDiario_Categoria_Covering criado com sucesso.';
GO

-- Atualizar estatísticas para garantir planos de execução otimizados
UPDATE STATISTICS [dbo].[ConsolidadoDiario] WITH FULLSCAN;
GO

PRINT 'Estatísticas atualizadas para a tabela ConsolidadoDiario.';
GO

-- Verificação dos índices criados
SELECT 
    i.name AS nome_indice,
    i.type_desc AS tipo_indice,
    i.is_unique AS unico,
    i.has_filter AS tem_filtro,
    i.filter_definition AS definicao_filtro,
    STUFF((
        SELECT ', ' + c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END
        FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 2, '') AS colunas_chave,
    STUFF((
        SELECT ', ' + c.name
        FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1
        ORDER BY ic.index_column_id
        FOR XML PATH('')
    ), 1, 2, '') AS colunas_incluidas
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.ConsolidadoDiario')
    AND i.name LIKE '%TipoConsolidacao%' 
    OR i.name LIKE '%Geral_Covering%'
    OR i.name LIKE '%Categoria_Covering%'
ORDER BY i.name;
GO

PRINT 'Script de criação de índices para tipo de consolidação executado com sucesso!';
PRINT 'Novos índices criados e otimizados para consultas por TipoConsolidacao.';
GO
