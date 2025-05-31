-- Script de validação da estrutura de bancos de dados
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Verifica se todas as estruturas foram criadas corretamente

PRINT '=== INICIANDO VALIDAÇÃO DA ESTRUTURA DOS BANCOS ===';
PRINT 'Data/Hora: ' + CONVERT(NVARCHAR, GETDATE(), 120);
GO

-- ========================================
-- VALIDAÇÃO DO BANCO FLUXOCAIXA
-- ========================================
PRINT '';
PRINT '--- Validando banco FluxoCaixa (Lançamentos) ---';

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'FluxoCaixa')
BEGIN
    PRINT '✓ Banco FluxoCaixa existe';
    
    USE FluxoCaixa;
    
    -- Verificar tabela Lancamentos
    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'Lancamentos')
    BEGIN
        PRINT '✓ Tabela Lancamentos existe';
        
        -- Verificar colunas obrigatórias
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Lancamentos') AND name = 'Id')
            AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Lancamentos') AND name = 'Valor')
            AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Lancamentos') AND name = 'Tipo')
            AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Lancamentos') AND name = 'Data')
            AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Lancamentos') AND name = 'Categoria')
        BEGIN
            PRINT '✓ Estrutura da tabela Lancamentos está correta';
        END
        ELSE
        BEGIN
            PRINT '✗ ERRO: Estrutura da tabela Lancamentos incompleta';
        END
    END
    ELSE
    BEGIN
        PRINT '✗ ERRO: Tabela Lancamentos não encontrada';
    END
END
ELSE
BEGIN
    PRINT '✗ ERRO: Banco FluxoCaixa não encontrado';
END

-- ========================================
-- VALIDAÇÃO DO BANCO FLUXOCAIXA_CONSOLIDADO
-- ========================================
PRINT '';
PRINT '--- Validando banco FluxoCaixa_Consolidado ---';

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'FluxoCaixa_Consolidado')
BEGIN
    PRINT '✓ Banco FluxoCaixa_Consolidado existe';
    
    USE FluxoCaixa_Consolidado;
    
    -- Verificar tabela ConsolidadoDiario
    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'ConsolidadoDiario')
    BEGIN
        PRINT '✓ Tabela ConsolidadoDiario existe';
    END
    ELSE
    BEGIN
        PRINT '✗ ERRO: Tabela ConsolidadoDiario não encontrada';
    END
    
    -- Verificar tabela LancamentoProcessado
    IF EXISTS (SELECT * FROM sys.tables WHERE name = N'LancamentoProcessado')
    BEGIN
        PRINT '✓ Tabela LancamentoProcessado existe';
    END
    ELSE
    BEGIN
        PRINT '✗ ERRO: Tabela LancamentoProcessado não encontrada';
    END
    
    -- Verificar views
    IF EXISTS (SELECT * FROM sys.views WHERE name = N'vw_ConsolidacaoGeral')
    BEGIN
        PRINT '✓ View vw_ConsolidacaoGeral existe';
    END
    ELSE
    BEGIN
        PRINT '✗ ERRO: View vw_ConsolidacaoGeral não encontrada';
    END
    
    IF EXISTS (SELECT * FROM sys.views WHERE name = N'vw_ConsolidacaoPorCategoria')
    BEGIN
        PRINT '✓ View vw_ConsolidacaoPorCategoria existe';
    END
    ELSE
    BEGIN
        PRINT '✗ ERRO: View vw_ConsolidacaoPorCategoria não encontrada';
    END
    
    -- Verificar procedures
    IF EXISTS (SELECT * FROM sys.procedures WHERE name = N'sp_LimparLancamentosProcessados')
    BEGIN
        PRINT '✓ Procedure sp_LimparLancamentosProcessados existe';
    END
    ELSE
    BEGIN
        PRINT '✗ ERRO: Procedure sp_LimparLancamentosProcessados não encontrada';
    END
    
    -- Verificar índices principais
    IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'ConsolidadoDiario') AND name = N'IX_ConsolidadoDiario_Geral_Data')
    BEGIN
        PRINT '✓ Índice IX_ConsolidadoDiario_Geral_Data existe';
    END
    ELSE
    BEGIN
        PRINT '✗ ERRO: Índice IX_ConsolidadoDiario_Geral_Data não encontrado';
    END
END
ELSE
BEGIN
    PRINT '✗ ERRO: Banco FluxoCaixa_Consolidado não encontrado';
END

-- ========================================
-- ESTATÍSTICAS FINAIS
-- ========================================
PRINT '';
PRINT '--- Estatísticas dos bancos ---';

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'FluxoCaixa')
BEGIN
    USE FluxoCaixa;
    SELECT 
        'FluxoCaixa' AS Banco,
        COUNT(CASE WHEN type = 'U' THEN 1 END) AS Tabelas,
        COUNT(CASE WHEN type = 'V' THEN 1 END) AS Views,
        COUNT(CASE WHEN type = 'P' THEN 1 END) AS Procedures
    FROM sys.objects 
    WHERE type IN ('U', 'V', 'P');
END

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'FluxoCaixa_Consolidado')
BEGIN
    USE FluxoCaixa_Consolidado;
    SELECT 
        'FluxoCaixa_Consolidado' AS Banco,
        COUNT(CASE WHEN type = 'U' THEN 1 END) AS Tabelas,
        COUNT(CASE WHEN type = 'V' THEN 1 END) AS Views,
        COUNT(CASE WHEN type = 'P' THEN 1 END) AS Procedures
    FROM sys.objects 
    WHERE type IN ('U', 'V', 'P');
END

PRINT '';
PRINT '=== VALIDAÇÃO CONCLUÍDA ===';
PRINT 'Data/Hora: ' + CONVERT(NVARCHAR, GETDATE(), 120);
GO
