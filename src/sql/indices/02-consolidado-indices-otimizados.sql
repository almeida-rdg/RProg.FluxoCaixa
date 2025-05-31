-- Script para criação dos índices otimizados do módulo consolidado
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Criação: Índices especializados para máxima performance (opcional em produção)

USE FluxoCaixa_Consolidado;
GO

-- Índice especializado para categoria e data com filtros otimizados
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Categoria_Data_Otimizado')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Categoria_Data_Otimizado] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC, [TipoConsolidacao] ASC, [Categoria] ASC)
    INCLUDE ([TotalCreditos], [TotalDebitos], [SaldoLiquido], [QuantidadeLancamentos], [DataAtualizacao])
    WITH (FILLFACTOR = 95, PAD_INDEX = ON);

    PRINT 'Índice IX_ConsolidadoDiario_Categoria_Data_Otimizado criado com sucesso.';
END
GO

-- Índice para consultas de relatórios por período
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Periodo_Relatorios')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Periodo_Relatorios] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC, [Categoria] ASC)
    INCLUDE ([TotalCreditos], [TotalDebitos], [SaldoLiquido], [QuantidadeLancamentos])
    WITH (FILLFACTOR = 90);

    PRINT 'Índice IX_ConsolidadoDiario_Periodo_Relatorios criado com sucesso.';
END
GO

-- Índice para limpeza de dados antigos
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[LancamentoProcessado]') AND name = N'IX_LancamentoProcessado_Limpeza')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LancamentoProcessado_Limpeza] 
    ON [dbo].[LancamentoProcessado] ([DataProcessamento] ASC)
    INCLUDE ([LancamentoId], [NomeFila])
    WITH (FILLFACTOR = 85);

    PRINT 'Índice IX_LancamentoProcessado_Limpeza criado com sucesso.';
END
GO
