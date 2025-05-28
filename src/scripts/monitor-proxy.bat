@echo off
REM Monitor script for Windows - FluxoCaixa Proxy
echo FluxoCaixa Proxy Monitor
echo =======================

:MENU
echo.
echo 1. Status dos containers
echo 2. Health check do proxy
echo 3. Logs do proxy (tempo real)
echo 4. Verificar recursos do sistema
echo 5. Testar APIs via proxy
echo 6. Relatório completo
echo 7. Restart do proxy
echo 8. Sair
echo.
set /p choice="Escolha uma opção (1-8): "

if "%choice%"=="1" goto STATUS
if "%choice%"=="2" goto HEALTH
if "%choice%"=="3" goto LOGS
if "%choice%"=="4" goto RESOURCES
if "%choice%"=="5" goto TEST_APIS
if "%choice%"=="6" goto REPORT
if "%choice%"=="7" goto RESTART
if "%choice%"=="8" goto EXIT
goto MENU

:STATUS
echo.
echo === Status dos Containers ===
docker-compose ps
echo.
echo === Containers em execução ===
docker ps --filter "name=fluxo" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo.
pause
goto MENU

:HEALTH
echo.
echo === Health Check do Proxy ===
curl -s http://localhost/health
if %errorlevel% equ 0 (
    echo.
    echo ✓ Proxy está saudável
) else (
    echo.
    echo ✗ Proxy não está respondendo
)
echo.
pause
goto MENU

:LOGS
echo.
echo === Logs do Proxy (Ctrl+C para sair) ===
docker-compose logs -f proxy
goto MENU

:RESOURCES
echo.
echo === Recursos do Sistema ===
echo Memória:
wmic OS get TotalVisibleMemorySize,FreePhysicalMemory /format:table
echo.
echo CPU:
wmic cpu get loadpercentage /value
echo.
echo Espaço em disco:
wmic logicaldisk get size,freespace,caption /format:table
echo.
pause
goto MENU

:TEST_APIS
echo.
echo === Testando APIs via Proxy ===
echo.
echo Testando Lançamentos API...
curl -s -o nul -w "Status: %%{http_code} - Tempo: %%{time_total}s\n" http://localhost/api/lancamentos/health
echo.
echo Testando Consolidado API...
curl -s -o nul -w "Status: %%{http_code} - Tempo: %%{time_total}s\n" http://localhost/api/consolidado/health
echo.
pause
goto MENU

:REPORT
echo.
echo === Relatório Completo ===
echo Timestamp: %date% %time%
echo.
echo === Status dos Containers ===
docker-compose ps
echo.
echo === Health Checks ===
echo Proxy:
curl -s http://localhost/health
echo.
echo === Últimos 10 logs do proxy ===
docker-compose logs --tail=10 proxy
echo.
echo === Uso de CPU por container ===
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"
echo.
pause
goto MENU

:RESTART
echo.
echo === Reiniciando Proxy ===
docker-compose restart proxy
echo Aguardando proxy ficar pronto...
timeout /t 10 /nobreak >nul
curl -s http://localhost/health
if %errorlevel% equ 0 (
    echo ✓ Proxy reiniciado com sucesso
) else (
    echo ✗ Falha ao reiniciar proxy
)
echo.
pause
goto MENU

:EXIT
echo Saindo do monitor...
exit /b 0
