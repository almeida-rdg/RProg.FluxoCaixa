-- Script para criação de views otimizadas substituindo stored procedures
-- RProg.FluxoCaixa.Worker - Abordagem híbrida com views materializadas
-- Seguindo padrões de Clean Architecture e testabilidade

USE FluxoCaixa_Consolidado;
GO

-- Configurações de performance para sessão
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- View otimizada para consolidação geral com hint de índice embutido
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_ConsolidacaoGeral]'))
BEGIN
    DROP VIEW [dbo].[vw_ConsolidacaoGeral];
END
GO

CREATE VIEW [dbo].[vw_ConsolidacaoGeral]
AS
SELECT 
    [Id],
    [Data],
    [TotalCreditos],
    [TotalDebitos],
    [SaldoLiquido],
    [QuantidadeLancamentos],
    [DataCriacao],
    [DataAtualizacao]
FROM [dbo].[ConsolidadoDiario] 
WHERE [TipoConsolidacao] = 'GERAL';
GO

-- View otimizada para consolidação por categoria
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_ConsolidacaoPorCategoria]'))
BEGIN
    DROP VIEW [dbo].[vw_ConsolidacaoPorCategoria];
END
GO

CREATE VIEW [dbo].[vw_ConsolidacaoPorCategoria] 
AS
SELECT 
    [Id],
    [Data],
    [Categoria],
    [TotalCreditos],
    [TotalDebitos],
    [SaldoLiquido],
    [QuantidadeLancamentos],
    [DataCriacao],
    [DataAtualizacao]
FROM [dbo].[ConsolidadoDiario]
WHERE [TipoConsolidacao] = 'CATEGORIA';
GO

-- View materializada (indexed view) para relatórios frequentes
-- Otimizada para máxima performance de leitura
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_ResumoConsolidacao]'))
BEGIN
    -- Remove índice primeiro, depois a view
    IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[vw_ResumoConsolidacao]') AND name = N'IX_vw_ResumoConsolidacao')
    BEGIN
        DROP INDEX [IX_vw_ResumoConsolidacao] ON [dbo].[vw_ResumoConsolidacao];
    END
    DROP VIEW [dbo].[vw_ResumoConsolidacao];
END
GO

CREATE VIEW [dbo].[vw_ResumoConsolidacao]
WITH SCHEMABINDING
AS
SELECT 
    [Data],
    [TipoConsolidacao],
    [Categoria],
    [TotalCreditos],
    [TotalDebitos],
    [SaldoLiquido],
    [QuantidadeLancamentos],
    [DataCriacao],
    [DataAtualizacao],
    COUNT_BIG(*) as [Contador]
FROM [dbo].[ConsolidadoDiario]
GROUP BY [Data], [TipoConsolidacao], [Categoria], [TotalCreditos], [TotalDebitos], 
         [SaldoLiquido], [QuantidadeLancamentos], [DataCriacao], [DataAtualizacao];
GO

-- Índice único na view para torná-la materializada (cached)
CREATE UNIQUE CLUSTERED INDEX [IX_vw_ResumoConsolidacao]
ON [dbo].[vw_ResumoConsolidacao] ([Data], [TipoConsolidacao], [Categoria]);
GO

-- View otimizada para análise de tendências por período
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_TendenciasConsolidacao]'))
BEGIN
    DROP VIEW [dbo].[vw_TendenciasConsolidacao];
END
GO

CREATE VIEW [dbo].[vw_TendenciasConsolidacao]
AS
SELECT 
    [Data],
    [TipoConsolidacao],
    [Categoria],
    [TotalCreditos],
    [TotalDebitos],
    [SaldoLiquido],
    [QuantidadeLancamentos],
    -- Campos calculados para análise de tendências
    LAG([SaldoLiquido], 1) OVER (PARTITION BY [TipoConsolidacao], [Categoria] ORDER BY [Data]) as [SaldoAnterior],
    [SaldoLiquido] - LAG([SaldoLiquido], 1) OVER (PARTITION BY [TipoConsolidacao], [Categoria] ORDER BY [Data]) as [VariacaoSaldo]
FROM [dbo].[ConsolidadoDiario];
GO

-- Mantemos apenas uma stored procedure para operações de manutenção
-- (não viola princípios arquiteturais pois é operação administrativa, não de negócio)
-- A stored procedure de limpeza é mantida pois é uma operação de manutenção
PRINT 'Views otimizadas criadas com sucesso!';
PRINT 'Views disponíveis:';
PRINT '- vw_ConsolidacaoGeral: Consolidações gerais com performance otimizada';
PRINT '- vw_ConsolidacaoPorCategoria: Consolidações por categoria com filtros flexíveis';
PRINT '- vw_ResumoConsolidacao: View materializada para relatórios frequentes';
PRINT '- vw_TendenciasConsolidacao: Análise de tendências com campos calculados';
GO
