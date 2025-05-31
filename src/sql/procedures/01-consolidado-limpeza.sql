-- Script para criação das procedures de limpeza do módulo consolidado
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Criação: Procedures para manutenção e limpeza de dados

USE FluxoCaixa_Consolidado;
GO

-- Procedure para limpeza de lançamentos processados antigos
IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID(N'[dbo].[sp_LimparLancamentosProcessados]'))
BEGIN
    DROP PROCEDURE [dbo].[sp_LimparLancamentosProcessados];
END
GO

CREATE PROCEDURE [dbo].[sp_LimparLancamentosProcessados]
    @DiasParaManutencao INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DataLimite DATETIME2 = DATEADD(DAY, -@DiasParaManutencao, GETUTCDATE());
    DECLARE @RegistrosRemovidos INT = 0;
    
    BEGIN TRY
        DELETE FROM [dbo].[LancamentoProcessado]
        WHERE [DataProcessamento] < @DataLimite;
        
        SET @RegistrosRemovidos = @@ROWCOUNT;
        
        PRINT 'Limpeza concluída. Registros removidos: ' + CAST(@RegistrosRemovidos AS NVARCHAR(10));
        
    END TRY
    BEGIN CATCH
        PRINT 'Erro durante limpeza: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
GO

-- Procedure para estatísticas de consolidação
IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID(N'[dbo].[sp_EstatisticasConsolidacao]'))
BEGIN
    DROP PROCEDURE [dbo].[sp_EstatisticasConsolidacao];
END
GO

CREATE PROCEDURE [dbo].[sp_EstatisticasConsolidacao]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        'ConsolidadoDiario' AS Tabela,
        COUNT(*) AS TotalRegistros,
        COUNT(CASE WHEN [TipoConsolidacao] = 'GERAL' THEN 1 END) AS ConsolidacoesGerais,
        COUNT(CASE WHEN [TipoConsolidacao] = 'CATEGORIA' THEN 1 END) AS ConsolidacoesPorCategoria,
        MIN([Data]) AS DataMaisAntiga,
        MAX([Data]) AS DataMaisRecente
    FROM [dbo].[ConsolidadoDiario]
    
    UNION ALL
    
    SELECT 
        'LancamentoProcessado' AS Tabela,
        COUNT(*) AS TotalRegistros,
        0 AS ConsolidacoesGerais,
        0 AS ConsolidacoesPorCategoria,
        MIN([DataProcessamento]) AS DataMaisAntiga,
        MAX([DataProcessamento]) AS DataMaisRecente
    FROM [dbo].[LancamentoProcessado];
END
GO

PRINT 'Procedures de limpeza do módulo consolidado criadas com sucesso.';
GO
