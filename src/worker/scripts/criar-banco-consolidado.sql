-- Criar banco de dados (caso não exista)
IF NOT EXISTS (SELECT name
FROM sys.databases
WHERE name = 'FluxoCaixa_Consolidado')
BEGIN
    CREATE DATABASE FluxoCaixa_Consolidado;

    PRINT 'Banco FluxoCaixa_Consolidado criado com sucesso.';
END
GO

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

-- Índices especializados para máxima performance de leitura
-- Índice filtrado para consultas de consolidação geral
-- Instruções WITH removidas: inserções sempre sequenciais por Data
-- Com padrão sequencial, não há necessidade de controle de fragmentação
-- Filtro corrigido: usar Categoria IS NULL ao invés da coluna computada TipoConsolidacao
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

-- Índice especializado para categoria e data (já será criado na própria tabela)
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Categoria_Data_Adicional')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Categoria_Data_Adicional] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC, [TipoConsolidacao] ASC, [Categoria] ASC)
    WITH (FILLFACTOR = 95, PAD_INDEX = ON, ONLINE = OFF);

    PRINT 'Índice IX_ConsolidadoDiario_Categoria_Data_Adicional criado com sucesso.';
END
GO

-- Índice covering para relatórios completos
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Relatorios_Otimizado')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Relatorios_Otimizado] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC, [TipoConsolidacao] ASC) 
    INCLUDE ([Categoria], [TotalCreditos], [TotalDebitos], [SaldoLiquido], [QuantidadeLancamentos])
    WITH (FILLFACTOR = 90, PAD_INDEX = ON, ONLINE = OFF);

    PRINT 'Índice IX_ConsolidadoDiario_Relatorios_Otimizado criado com sucesso.';
END
GO

-- Índice para busca por período específico
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Periodo_Otimizado')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Periodo_Otimizado] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC, [TipoConsolidacao] ASC, [Categoria] ASC)
    WITH (FILLFACTOR = 95, PAD_INDEX = ON, ONLINE = OFF);

    PRINT 'Índice IX_ConsolidadoDiario_Periodo_Otimizado criado com sucesso.';
END
GO

-- Índice para tabela de controle com padrão de inserção sequencial
-- Instruções WITH removidas: DataProcessamento sempre cresce sequencialmente
-- Inserções cronológicas eliminam necessidade de controle de fragmentação
IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[dbo].[LancamentoProcessado]') AND name = N'IX_LancamentoProcessado_DataProcessamento_Otimizado')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LancamentoProcessado_DataProcessamento_Otimizado] ON [dbo].[LancamentoProcessado] ([DataProcessamento] ASC);
    PRINT 'Índice IX_LancamentoProcessado_DataProcessamento_Otimizado criado com sucesso.';
END
GO

-- Stored Procedure para limpeza de registros antigos
IF EXISTS (SELECT *
FROM sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[sp_LimparLancamentosProcessadosAntigos]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_LimparLancamentosProcessadosAntigos];
END
GO

CREATE PROCEDURE [dbo].[sp_LimparLancamentosProcessadosAntigos]
    @DiasParaManter INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DataLimite DATETIME2 = DATEADD(DAY, -@DiasParaManter, GETUTCDATE());
    DECLARE @RegistrosRemovidos INT;

    DELETE FROM [dbo].[LancamentoProcessado] 
    WHERE [DataProcessamento] < @DataLimite;

    SET @RegistrosRemovidos = @@ROWCOUNT;

    PRINT CONCAT('Limpeza concluída. Registros removidos: ', @RegistrosRemovidos);

    RETURN @RegistrosRemovidos;
END
GO

PRINT 'Script de criação da estrutura do banco consolidado executado com sucesso!';
GO
