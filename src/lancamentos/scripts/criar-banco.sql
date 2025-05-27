-- Script para criar a estrutura inicial do banco de dados FluxoCaixa
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FluxoCaixa')
BEGIN
    CREATE DATABASE FluxoCaixa;
END
GO

USE FluxoCaixa;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Lancamentos')
BEGIN
    CREATE TABLE Lancamentos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Valor DECIMAL(18,2) NOT NULL,
        Tipo NVARCHAR(20) NOT NULL,
        Data DATETIME NOT NULL,
        Categoria NVARCHAR(100) NOT NULL,
        Descricao NVARCHAR(255) NULL
    );
END
