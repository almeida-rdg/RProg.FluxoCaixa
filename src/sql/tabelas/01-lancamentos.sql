-- Script para criação das tabelas do módulo de lançamentos
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Criação: Tabelas do módulo de lançamentos

USE FluxoCaixa;
GO

-- Tabela principal de lançamentos financeiros
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Lancamentos')
BEGIN
    CREATE TABLE Lancamentos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Valor DECIMAL(18,2) NOT NULL,
        Tipo NVARCHAR(20) NOT NULL,
        Data DATETIME NOT NULL,
        Categoria NVARCHAR(100) NOT NULL,
        Descricao NVARCHAR(255) NULL,
        CONSTRAINT [CK_Lancamentos_Valor] CHECK (Valor <> 0),
        CONSTRAINT [CK_Lancamentos_Tipo] CHECK (Tipo IN ('Credito', 'Debito'))
    );
    
    PRINT 'Tabela Lancamentos criada com sucesso.';
END
ELSE
BEGIN
    PRINT 'Tabela Lancamentos já existe.';
END
GO
