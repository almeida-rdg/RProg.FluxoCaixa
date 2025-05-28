@echo off
REM Test script for FluxoCaixa Proxy
echo ========================================
echo FluxoCaixa Proxy - Teste de Integração
echo ========================================

set PROXY_URL=http://localhost
set FAILED_TESTS=0
set TOTAL_TESTS=0

REM Função para executar teste
:TEST
set /a TOTAL_TESTS+=1
echo.
echo [TESTE %TOTAL_TESTS%] %~1
%~2
if %errorlevel% neq 0 (
    echo ✗ FALHOU: %~1
    set /a FAILED_TESTS+=1
) else (
    echo ✓ PASSOU: %~1
)
goto :eof

REM Iniciar testes
echo Iniciando testes do proxy...
echo.

REM Teste 1: Health Check
call :TEST "Health Check do Proxy" "curl -f -s %PROXY_URL%/health"

REM Teste 2: Proxy Info
call :TEST "Informações do Proxy" "curl -f -s %PROXY_URL%/proxy/info"

REM Teste 3: Métricas
call :TEST "Endpoint de Métricas" "curl -f -s %PROXY_URL%/metrics"

REM Teste 4: API Lançamentos via Proxy
call :TEST "API Lançamentos via Proxy" "curl -f -s %PROXY_URL%/api/lancamentos/health"

REM Teste 5: API Consolidado via Proxy
call :TEST "API Consolidado via Proxy" "curl -f -s %PROXY_URL%/api/consolidado/health"

REM Teste 6: Rate Limiting (burst test)
echo.
echo [TESTE %TOTAL_TESTS%] Rate Limiting - Teste de Burst
echo Enviando 15 requisições rápidas...
set RATE_LIMIT_FAILED=0
for /l %%i in (1,1,15) do (
    curl -s -o nul -w "%%{http_code}" %PROXY_URL%/health | findstr "429" >nul
    if !errorlevel! equ 0 set RATE_LIMIT_FAILED=1
)
if %RATE_LIMIT_FAILED% equ 1 (
    echo ✓ PASSOU: Rate Limiting funcionando (429 detectado)
) else (
    echo ✗ FALHOU: Rate Limiting não ativou
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

REM Teste 7: Cache Headers
echo.
echo [TESTE %TOTAL_TESTS%] Cache Headers
curl -I -s %PROXY_URL%/health | findstr "X-Cache-Status" >nul
if %errorlevel% equ 0 (
    echo ✓ PASSOU: Headers de cache presentes
) else (
    echo ✗ FALHOU: Headers de cache ausentes
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

REM Teste 8: Security Headers
echo.
echo [TESTE %TOTAL_TESTS%] Security Headers
curl -I -s %PROXY_URL%/health | findstr "X-Frame-Options" >nul
if %errorlevel% equ 0 (
    echo ✓ PASSOU: Headers de segurança presentes
) else (
    echo ✗ FALHOU: Headers de segurança ausentes
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

REM Teste 9: CORS Headers
echo.
echo [TESTE %TOTAL_TESTS%] CORS Headers
curl -H "Origin: http://localhost:3000" -I -s %PROXY_URL%/health | findstr "Access-Control-Allow-Origin" >nul
if %errorlevel% equ 0 (
    echo ✓ PASSOU: CORS configurado
) else (
    echo ✗ FALHOU: CORS não configurado
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

REM Teste 10: Performance (tempo de resposta)
echo.
echo [TESTE %TOTAL_TESTS%] Performance - Tempo de Resposta
for /f %%i in ('curl -o nul -s -w "%%{time_total}" %PROXY_URL%/health') do set RESPONSE_TIME=%%i
echo Tempo de resposta: %RESPONSE_TIME%s
REM Verificar se resposta foi menor que 2 segundos (usando comparação de string simplificada)
echo %RESPONSE_TIME% | findstr "^[01]\." >nul
if %errorlevel% equ 0 (
    echo ✓ PASSOU: Tempo de resposta bom (^<%RESPONSE_TIME%s^)
) else (
    echo ✗ FALHOU: Tempo de resposta alto (%RESPONSE_TIME%s)
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

REM Teste 11: SSL/HTTPS (se disponível)
echo.
echo [TESTE %TOTAL_TESTS%] HTTPS Support
curl -k -f -s https://localhost/health >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ PASSOU: HTTPS funcionando
) else (
    echo ! AVISO: HTTPS não disponível (normal em desenvolvimento)
)
set /a TOTAL_TESTS+=1

REM Teste 12: Container Health
echo.
echo [TESTE %TOTAL_TESTS%] Status dos Containers
docker ps --filter "name=fluxo-proxy" --filter "status=running" | findstr "fluxo-proxy" >nul
if %errorlevel% equ 0 (
    echo ✓ PASSOU: Container do proxy rodando
) else (
    echo ✗ FALHOU: Container do proxy não está rodando
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

REM Teste 13: Logs de Error
echo.
echo [TESTE %TOTAL_TESTS%] Verificação de Logs de Erro
docker logs fluxo-proxy --since 5m 2>&1 | findstr /i "error exception fatal" >nul
if %errorlevel% neq 0 (
    echo ✓ PASSOU: Nenhum erro crítico nos logs recentes
) else (
    echo ! AVISO: Erros encontrados nos logs recentes
    echo Últimos erros:
    docker logs fluxo-proxy --since 5m 2>&1 | findstr /i "error exception fatal" | head -3
)
set /a TOTAL_TESTS+=1

REM Teste 14: Memory Usage
echo.
echo [TESTE %TOTAL_TESTS%] Uso de Memória do Proxy
for /f "tokens=4" %%i in ('docker stats fluxo-proxy --no-stream --format "table {{.MemUsage}}"') do set MEM_USAGE=%%i
echo Uso de memória: %MEM_USAGE%
echo ✓ INFO: Uso de memória registrado
set /a TOTAL_TESTS+=1

REM Teste 15: CPU Usage
echo.
echo [TESTE %TOTAL_TESTS%] Uso de CPU do Proxy
for /f "tokens=2" %%i in ('docker stats fluxo-proxy --no-stream --format "table {{.CPUPerc}}"') do set CPU_USAGE=%%i
echo Uso de CPU: %CPU_USAGE%
echo ✓ INFO: Uso de CPU registrado
set /a TOTAL_TESTS+=1

REM Teste de Carga Simples
echo.
echo [TESTE EXTRA] Teste de Carga Simples
echo Executando 50 requisições simultâneas...
set LOAD_TEST_ERRORS=0
for /l %%i in (1,1,50) do (
    start /b curl -s -o nul %PROXY_URL%/health
)
timeout /t 5 /nobreak >nul
echo Teste de carga concluído

REM Resultados finais
echo.
echo ========================================
echo           RESULTADO DOS TESTES
echo ========================================
echo Total de testes: %TOTAL_TESTS%
echo Testes aprovados: %calc_passed%
set /a calc_passed=%TOTAL_TESTS%-%FAILED_TESTS%
echo Testes reprovados: %FAILED_TESTS%
echo.

if %FAILED_TESTS% equ 0 (
    echo ✓ TODOS OS TESTES PASSARAM!
    echo O proxy está funcionando corretamente.
) else (
    echo ✗ %FAILED_TESTS% TESTE(S) FALHARAM!
    echo Verifique os logs e configurações.
    echo.
    echo Para diagnóstico:
    echo - docker-compose logs proxy
    echo - docker-compose ps
    echo - curl -v %PROXY_URL%/health
)

echo.
echo Para monitoramento contínuo, execute:
echo   scripts\monitor-proxy.bat
echo.
pause
