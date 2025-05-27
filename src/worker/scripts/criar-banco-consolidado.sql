-- Script para criação do banco de dados consolidado
-- RProg.FluxoCaixa.Worker

-- Criar banco de dados (caso não exista)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RProg_FluxoCaixa_Consolidado')
BEGIN
    CREATE DATABASE RProg_FluxoCaixa_Consolidado;
END
GO

USE RProg_FluxoCaixa_Consolidado;
GO

-- Tabela para consolidações diárias
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ConsolidadoDiario] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Data] DATE NOT NULL,
        [Categoria] NVARCHAR(100) NULL,
        [TotalCreditos] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalDebitos] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [SaldoLiquido] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [QuantidadeLancamentos] INT NOT NULL DEFAULT 0,
        [DataCriacao] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [DataAtualizacao] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [PK_ConsolidadoDiario] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UK_ConsolidadoDiario_Data_Categoria] UNIQUE NONCLUSTERED ([Data] ASC, [Categoria] ASC)
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

-- Índices para otimização de consultas
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Data')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Data] ON [dbo].[ConsolidadoDiario] ([Data] ASC);
    PRINT 'Índice IX_ConsolidadoDiario_Data criado com sucesso.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ConsolidadoDiario]') AND name = N'IX_ConsolidadoDiario_Categoria')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ConsolidadoDiario_Categoria] ON [dbo].[ConsolidadoDiario] ([Categoria] ASC);
    PRINT 'Índice IX_ConsolidadoDiario_Categoria criado com sucesso.';
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

PRINT 'Script de criação da estrutura do banco consolidado executado com sucesso!';
GO
