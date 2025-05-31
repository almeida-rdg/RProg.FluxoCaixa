-- Script para criação das tabelas do módulo de consolidado
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Criação: Tabelas do módulo de consolidação

USE FluxoCaixa_Consolidado;
GO

-- Tabela para consolidações diárias otimizada para performance de leitura
IF NOT EXISTS (SELECT *
FROM sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ConsolidadoDiario]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Data] DATE NOT NULL,
        [Categoria] NVARCHAR(100) NULL,
        -- NULL = consolidação geral
        [TipoConsolidacao] AS (CASE WHEN [Categoria] IS NULL THEN 'GERAL' ELSE 'CATEGORIA' END) PERSISTED,
        [TotalCreditos] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalDebitos] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [SaldoLiquido] AS ([TotalCreditos] - ABS([TotalDebitos])) PERSISTED,
        [QuantidadeLancamentos] INT NOT NULL DEFAULT 0,
        [DataCriacao] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [DataAtualizacao] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ConsolidadoDiario_Id] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UK_ConsolidadoDiario_Data_Categoria] UNIQUE NONCLUSTERED ([Data] ASC, [Categoria] ASC),
        CONSTRAINT [CK_ConsolidadoDiario_Creditos] CHECK ([TotalCreditos] >= 0),
        CONSTRAINT [CK_ConsolidadoDiario_Debitos] CHECK ([TotalDebitos] <= 0),
        CONSTRAINT [CK_ConsolidadoDiario_Quantidade] CHECK ([QuantidadeLancamentos] >= 0)
    );

    PRINT 'Tabela ConsolidadoDiario criada com sucesso.';
END
ELSE
BEGIN
    PRINT 'Tabela ConsolidadoDiario já existe.';
END
GO

-- Tabela para controle de lançamentos processados (idempotência)
IF NOT EXISTS (SELECT *
FROM sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[LancamentoProcessado]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LancamentoProcessado]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [LancamentoId] NVARCHAR(255) NOT NULL,
        [DataProcessamento] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [HashConteudo] NVARCHAR(64) NULL,
        [NomeFila] NVARCHAR(100) NULL,
        [Observacoes] NVARCHAR(500) NULL,

        CONSTRAINT [PK_LancamentoProcessado] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UK_LancamentoProcessado_LancamentoId] UNIQUE NONCLUSTERED ([LancamentoId] ASC)
    );

    PRINT 'Tabela LancamentoProcessado criada com sucesso.';
END
ELSE
BEGIN
    PRINT 'Tabela LancamentoProcessado já existe.';
END
GO
