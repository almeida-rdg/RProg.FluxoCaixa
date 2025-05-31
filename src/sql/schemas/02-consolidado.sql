-- Script para criação do schema do banco de consolidado
-- FluxoCaixa - Sistema de Gestão de Fluxo de Caixa
-- Criação: Módulo de Consolidação

-- Verificar e criar banco de dados FluxoCaixa_Consolidado
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FluxoCaixa_Consolidado')
BEGIN
    CREATE DATABASE FluxoCaixa_Consolidado;
    PRINT 'Banco FluxoCaixa_Consolidado criado com sucesso.';
END
ELSE
BEGIN
    PRINT 'Banco FluxoCaixa_Consolidado já existe.';
END
GO
