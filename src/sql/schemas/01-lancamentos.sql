-- Script para criação do schema do banco de lançamentos
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Criação: Módulo de Lançamentos

-- Verificar e criar banco de dados FluxoCaixa
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FluxoCaixa')
BEGIN
    CREATE DATABASE FluxoCaixa;
    PRINT 'Banco FluxoCaixa criado com sucesso.';
END
ELSE
BEGIN
    PRINT 'Banco FluxoCaixa já existe.';
END
GO
