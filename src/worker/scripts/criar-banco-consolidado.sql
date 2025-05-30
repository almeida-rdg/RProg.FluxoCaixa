-- Criar banco de dados (caso não exista)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'FluxoCaixa_Consolidado')
BEGIN
    CREATE DATABASE FluxoCaixa_Consolidado;

    PRINT 'Banco FluxoCaixa_Consolidado criado com sucesso.';
END
GO

USE FluxoCaixa_Consolidado;
GO

-- Tabela para consolidações diárias otimizada para performance de leitura
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ConsolidadoDiario] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Data] DATE NOT NULL,
        [Categoria] NVARCHAR(100) NULL, -- NULL = consolidação geral
        [TipoConsolidacao] AS (CASE WHEN [Categoria] IS NULL THEN 'GERAL' ELSE 'CATEGORIA' END) PERSISTED,
        [TotalCreditos] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalDebitos] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [SaldoLiquido] AS ([TotalCreditos] - ABS([TotalDebitos])) PERSISTED,
        [QuantidadeLancamentos] INT NOT NULL DEFAULT 0,
        [DataCriacao] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [DataAtualizacao] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [PK_ConsolidadoDiario_Id] PRIMARY KEY NONCLUSTERED ([Id] ASC),
        INDEX [IX_ConsolidadoDiario_Categoria_Data] CLUSTERED ([Data] ASC, [TipoConsolidacao] ASC, [Categoria] ASC),
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
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LancamentoProcessado]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LancamentoProcessado] (
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
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Geral_Data')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Geral_Data] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC) 
    WHERE [TipoConsolidacao] = 'GERAL'
    WITH (FILLFACTOR = 95, PAD_INDEX = ON, ONLINE = OFF);
    
    PRINT 'Índice IX_ConsolidadoDiario_Geral_Data criado com sucesso.';
END
GO

-- Índice filtrado para consultas por categoria
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Categoria_Data')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Categoria_Data] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC, [Categoria] ASC) 
    WHERE [TipoConsolidacao] = 'CATEGORIA'
    WITH (FILLFACTOR = 95, PAD_INDEX = ON, ONLINE = OFF);
    
    PRINT 'Índice IX_ConsolidadoDiario_Categoria_Data criado com sucesso.';
END
GO

-- Índice covering para relatórios completos
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Relatorios')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Relatorios] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC, [TipoConsolidacao] ASC) 
    INCLUDE ([Categoria], [TotalCreditos], [TotalDebitos], [SaldoLiquido], [QuantidadeLancamentos])
    WITH (FILLFACTOR = 90, PAD_INDEX = ON, ONLINE = OFF);
    
    PRINT 'Índice IX_ConsolidadoDiario_Relatorios criado com sucesso.';
END
GO

-- Índice para busca por período específico
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Periodo')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Periodo] 
    ON [dbo].[ConsolidadoDiario] ([Data] ASC, [TipoConsolidacao] ASC, [Categoria] ASC)
    WITH (FILLFACTOR = 95, PAD_INDEX = ON, ONLINE = OFF);
    
    PRINT 'Índice IX_ConsolidadoDiario_Periodo criado com sucesso.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[LancamentoProcessado]') AND name = N'IX_LancamentoProcessado_DataProcessamento')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LancamentoProcessado_DataProcessamento] ON [dbo].[LancamentoProcessado] ([DataProcessamento] ASC);
    PRINT 'Índice IX_LancamentoProcessado_DataProcessamento criado com sucesso.';
END
GO

-- Stored Procedure para limpeza de registros antigos
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_LimparLancamentosProcessadosAntigos]') AND type in (N'P', N'PC'))
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

-- Stored Procedure otimizada para consulta de consolidação geral por período
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ObterConsolidacaoGeralPorPeriodo]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_ObterConsolidacaoGeralPorPeriodo];
END
GO

CREATE PROCEDURE [dbo].[sp_ObterConsolidacaoGeralPorPeriodo]
    @DataInicio DATE,
    @DataFim DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Utiliza índice filtrado IX_ConsolidadoDiario_Geral_Data para máxima performance
    SELECT 
        [Data],
        [TotalCreditos],
        [TotalDebitos],
        [SaldoLiquido],
        [QuantidadeLancamentos],
        [DataAtualizacao]
    FROM [dbo].[ConsolidadoDiario] WITH (INDEX(IX_ConsolidadoDiario_Geral_Data))
    WHERE [Data] BETWEEN @DataInicio AND @DataFim
      AND [TipoConsolidacao] = 'GERAL'
    ORDER BY [Data] ASC;
END
GO

-- Stored Procedure otimizada para consulta de consolidação por categoria e período
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ObterConsolidacaoPorCategoriaPeriodo]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_ObterConsolidacaoPorCategoriaPeriodo];
END
GO

CREATE PROCEDURE [dbo].[sp_ObterConsolidacaoPorCategoriaPeriodo]
    @DataInicio DATE,
    @DataFim DATE,
    @Categoria NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Utiliza índice filtrado IX_ConsolidadoDiario_Categoria_Data para máxima performance
    SELECT 
        [Data],
        [Categoria],
        [TotalCreditos],
        [TotalDebitos],
        [SaldoLiquido],
        [QuantidadeLancamentos],
        [DataAtualizacao]
    FROM [dbo].[ConsolidadoDiario] WITH (INDEX(IX_ConsolidadoDiario_Categoria_Data))
    WHERE [Data] BETWEEN @DataInicio AND @DataFim
      AND [TipoConsolidacao] = 'CATEGORIA'
      AND (@Categoria IS NULL OR [Categoria] = @Categoria)
    ORDER BY [Data] ASC, [Categoria] ASC;
END
GO

-- Stored Procedure para relatório completo (geral + categorias) otimizada
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ObterRelatorioCompletoConsolidacao]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_ObterRelatorioCompletoConsolidacao];
END
GO

CREATE PROCEDURE [dbo].[sp_ObterRelatorioCompletoConsolidacao]
    @DataInicio DATE,
    @DataFim DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Utiliza índice covering IX_ConsolidadoDiario_Relatorios para máxima performance
    SELECT 
        [Data],
        [TipoConsolidacao],
        [Categoria],
        [TotalCreditos],
        [TotalDebitos],
        [SaldoLiquido],
        [QuantidadeLancamentos]
    FROM [dbo].[ConsolidadoDiario] WITH (INDEX(IX_ConsolidadoDiario_Relatorios))
    WHERE [Data] BETWEEN @DataInicio AND @DataFim
    ORDER BY [Data] ASC, [TipoConsolidacao] ASC, [Categoria] ASC;
END
GO

PRINT 'Script de criação da estrutura do banco consolidado executado com sucesso!';
GO
