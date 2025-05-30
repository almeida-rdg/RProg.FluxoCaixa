@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Restore Otimizado - RProg.FluxoCaixa
echo ========================================
echo.

echo Verificando estrutura dos projetos...

REM Verificar se estamos no diretório correto
if not exist "Directory.Packages.props" (
    echo ERRO: Execute este script a partir do diretorio raiz do projeto
    echo       Onde estao localizados os arquivos Directory.Packages.props e global.json
    pause
    exit /b 1
)

echo Estrutura encontrada. Iniciando restore otimizado...
echo.

REM Configurar variáveis de ambiente para otimização
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set NUGET_XMLDOC_MODE=skip

REM Limpar cache local se solicitado
if "%1"=="--clear-cache" (
    echo Limpando cache do NuGet...
    dotnet nuget locals all --clear
    echo Cache limpo.
    echo.
)

REM Restore de todos os projetos em paralelo
echo Executando restore para todos os projetos...
echo.

REM Projetos principais
echo [1/8] Restaurando RProg.FluxoCaixa.Lancamentos...
dotnet restore "src\lancamentos\RProg.FluxoCaixa.Lancamentos\RProg.FluxoCaixa.Lancamentos.csproj" --verbosity minimal

echo [2/8] Restaurando RProg.FluxoCaixa.Consolidado...
dotnet restore "src\consolidado\RProg.FluxoCaixa.Consolidado\RProg.FluxoCaixa.Consolidado.csproj" --verbosity minimal

echo [3/8] Restaurando RProg.FluxoCaixa.Proxy...
dotnet restore "src\proxy\RProg.FluxoCaixa.Proxy\RProg.FluxoCaixa.Proxy.csproj" --verbosity minimal

echo [4/8] Restaurando RProg.FluxoCaixa.Worker...
dotnet restore "src\worker\RProg.FluxoCaixa.Worker\RProg.FluxoCaixa.Worker.csproj" --verbosity minimal

REM Projetos de teste
echo [5/8] Restaurando RProg.FluxoCaixa.Lancamentos.Test...
dotnet restore "src\lancamentos\RProg.FluxoCaixa.Lancamentos.Test\RProg.FluxoCaixa.Lancamentos.Test.csproj" --verbosity minimal

echo [6/8] Restaurando RProg.FluxoCaixa.Consolidado.Test...
dotnet restore "src\consolidado\RProg.FluxoCaixa.Consolidado.Test\RProg.FluxoCaixa.Consolidado.Test.csproj" --verbosity minimal

echo [7/8] Restaurando RProg.FluxoCaixa.Proxy.Test...
dotnet restore "src\proxy\RProg.FluxoCaixa.Proxy.Test\RProg.FluxoCaixa.Proxy.Test.csproj" --verbosity minimal

echo [8/8] Restaurando RProg.FluxoCaixa.Worker.Test...
dotnet restore "src\worker\RProg.FluxoCaixa.Worker.Test\RProg.FluxoCaixa.Worker.Test.csproj" --verbosity minimal

echo.
echo ========================================
echo  Restore concluido com sucesso!
echo ========================================
echo.

REM Verificar se packages.lock.json foram criados
echo Verificando arquivos de lock gerados...
if exist "src\lancamentos\RProg.FluxoCaixa.Lancamentos\packages.lock.json" (
    echo ✓ Lock file criado para Lancamentos
) else (
    echo ⚠ Lock file NAO criado para Lancamentos
)

if exist "src\consolidado\RProg.FluxoCaixa.Consolidado\packages.lock.json" (
    echo ✓ Lock file criado para Consolidado
) else (
    echo ⚠ Lock file NAO criado para Consolidado
)

if exist "src\proxy\RProg.FluxoCaixa.Proxy\packages.lock.json" (
    echo ✓ Lock file criado para Proxy
) else (
    echo ⚠ Lock file NAO criado para Proxy
)

if exist "src\worker\RProg.FluxoCaixa.Worker\packages.lock.json" (
    echo ✓ Lock file criado para Worker
) else (
    echo ⚠ Lock file NAO criado para Worker
)

echo.
echo Dicas para otimizacao:
echo - Os arquivos packages.lock.json devem ser commitados no Git
echo - Para builds de CI/CD, use --locked-mode para restore mais rapido
echo - Use este script sempre que adicionar novos pacotes
echo.

REM Exibir estatísticas do cache
echo Estatisticas do cache NuGet:
dotnet nuget locals global-packages --list
echo.

pause
