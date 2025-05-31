-- Script para criação dos índices base do módulo consolidado
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Criação: Índices básicos para performance

USE FluxoCaixa_Consolidado;
GO

-- Índice filtrado para consultas de consolidação geral
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Geral_Data')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Geral_Data] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC) 
    WHERE [Categoria] IS NULL;

    PRINT 'Índice IX_ConsolidadoDiario_Geral_Data criado com sucesso.';
END
GO

-- Índice básico para consultas por data e tipo
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Data_Tipo')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Data_Tipo] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC, [TipoConsolidacao] ASC)
    INCLUDE ([TotalCreditos], [TotalDebitos], [SaldoLiquido], [QuantidadeLancamentos]);

    PRINT 'Índice IX_ConsolidadoDiario_Data_Tipo criado com sucesso.';
END
GO

-- Índice para controle de lançamentos processados
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[LancamentoProcessado]') AND name = N'IX_LancamentoProcessado_DataProcessamento')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LancamentoProcessado_DataProcessamento] 
    ON [dbo].[LancamentoProcessado] ([DataProcessamento] DESC);

    PRINT 'Índice IX_LancamentoProcessado_DataProcessamento criado com sucesso.';
END
GO
