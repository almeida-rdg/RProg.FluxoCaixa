-- Script mestre para inicialização completa dos bancos de dados
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Executa todos os scripts de criação em ordem de dependência

PRINT '=== INICIANDO CONFIGURAÇÃO DOS BANCOS DE DADOS FLUXOCAIXA ===';
PRINT 'Data/Hora: ' + CONVERT(NVARCHAR, GETDATE(), 120);
GO

-- ========================================
-- 1. CRIAÇÃO DOS SCHEMAS (BANCOS)
-- ========================================
PRINT '';
PRINT '--- Etapa 1: Criando schemas dos bancos ---';

PRINT 'Executando: Criação do banco FluxoCaixa (Lançamentos)';
:r ./schemas/01-lancamentos.sql

PRINT 'Executando: Criação do banco FluxoCaixa_Consolidado';
:r ./schemas/02-consolidado.sql

-- ========================================
-- 2. CRIAÇÃO DAS TABELAS
-- ========================================
PRINT '';
PRINT '--- Etapa 2: Criando estrutura das tabelas ---';

PRINT 'Executando: Criação das tabelas de lançamentos';
:r ./tabelas/01-lancamentos.sql

PRINT 'Executando: Criação das tabelas de consolidação';
:r ./tabelas/02-consolidado-tabelas.sql

-- ========================================
-- 3. CRIAÇÃO DOS ÍNDICES BASE
-- ========================================
PRINT '';
PRINT '--- Etapa 3: Criando índices básicos ---';

PRINT 'Executando: Criação dos índices base do consolidado';
:r ./indices/01-consolidado-indices-base.sql

-- ========================================
-- 4. CRIAÇÃO DAS VIEWS
-- ========================================
PRINT '';
PRINT '--- Etapa 4: Criando views otimizadas ---';

PRINT 'Executando: Criação das views do consolidado';
:r ./views/01-consolidado-views.sql

-- ========================================
-- 5. CRIAÇÃO DAS PROCEDURES
-- ========================================
PRINT '';
PRINT '--- Etapa 5: Criando procedures de manutenção ---';

PRINT 'Executando: Criação das procedures de limpeza';
:r ./procedures/01-consolidado-limpeza.sql

-- ========================================
-- 6. ÍNDICES OTIMIZADOS (OPCIONAL)
-- ========================================
PRINT '';
PRINT '--- Etapa 6: Criando índices otimizados (OPCIONAL) ---';
:r ./indices/02-consolidado-indices-otimizados.sql

-- ========================================
-- FINALIZAÇÃO
-- ========================================
PRINT '';
PRINT '=== CONFIGURAÇÃO CONCLUÍDA COM SUCESSO ===';
PRINT 'Bancos criados: FluxoCaixa, FluxoCaixa_Consolidado';
PRINT 'Estrutura completa instalada e pronta para uso';
PRINT 'Data/Hora: ' + CONVERT(NVARCHAR, GETDATE(), 120);
GO
