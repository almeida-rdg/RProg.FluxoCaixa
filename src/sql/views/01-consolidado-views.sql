-- Script para criação das views do módulo consolidado
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Criação: Views otimizadas para consultas frequentes

USE FluxoCaixa_Consolidado;
GO

-- Configurações de performance para sessão
SET ANSI_NULLS ON;
GO

-- View otimizada para consolidação geral
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

-- View para resumo diário consolidado (sem schemabinding para simplicidade)
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_ResumoConsolidacao]'))
BEGIN
    DROP VIEW [dbo].[vw_ResumoConsolidacao];
END
GO

CREATE VIEW [dbo].[vw_ResumoConsolidacao]
AS
SELECT 
    [Data],
    [TipoConsolidacao],
    [Categoria],
    [TotalCreditos],
    [TotalDebitos],
    [SaldoLiquido],
    [QuantidadeLancamentos],
    [DataAtualizacao]
FROM [dbo].[ConsolidadoDiario];
GO

PRINT 'Views do módulo consolidado criadas com sucesso.';
GO
