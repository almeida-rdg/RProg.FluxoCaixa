@echo off
REM Setup script para o FluxoCaixa Proxy no Windows
echo =================================
echo FluxoCaixa Proxy Setup - Windows
echo =================================

REM Verificar se Docker está instalado
docker --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: Docker não encontrado. Instale o Docker Desktop primeiro.
    pause
    exit /b 1
)

REM Verificar se Docker Compose está instalado
docker-compose --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: Docker Compose não encontrado.
    pause
    exit /b 1
)

echo Docker e Docker Compose encontrados.

REM Criar diretórios necessários
echo Criando diretórios necessários...
if not exist "src\proxy\certs" mkdir "src\proxy\certs"
if not exist "logs" mkdir "logs"
if not exist "data" mkdir "data"

REM Gerar certificado SSL auto-assinado para desenvolvimento
echo Gerando certificado SSL para desenvolvimento...
cd src\proxy\certs
dotnet dev-certs https -ep aspnetapp.pfx -p YourSecurePassword --trust
if %errorlevel% neq 0 (
    echo AVISO: Falha ao gerar certificado. Continuando sem HTTPS.
)
cd ..\..\..

REM Configurar variáveis de ambiente para desenvolvimento
echo Configurando variáveis de ambiente...
set ASPNETCORE_ENVIRONMENT=Development
set DOCKER_BUILDKIT=1

REM Build das imagens Docker
echo Building imagens Docker...
docker-compose build --no-cache
if %errorlevel% neq 0 (
    echo ERRO: Falha no build das imagens.
    pause
    exit /b 1
)

REM Iniciar dependências primeiro
echo Iniciando dependências (SQL Server e RabbitMQ)...
docker-compose up -d sqlserver rabbitmq
if %errorlevel% neq 0 (
    echo ERRO: Falha ao iniciar dependências.
    pause
    exit /b 1
)

REM Aguardar dependências ficarem prontas
echo Aguardando dependências ficarem prontas...
timeout /t 30 /nobreak >nul

REM Iniciar APIs
echo Iniciando APIs...
docker-compose up -d lancamentos-api consolidado-api worker
if %errorlevel% neq 0 (
    echo ERRO: Falha ao iniciar APIs.
    pause
    exit /b 1
)

REM Aguardar APIs ficarem prontas
echo Aguardando APIs ficarem prontas...
timeout /t 20 /nobreak >nul

REM Iniciar proxy
echo Iniciando proxy...
docker-compose up -d proxy
if %errorlevel% neq 0 (
    echo ERRO: Falha ao iniciar proxy.
    pause
    exit /b 1
)

REM Aguardar proxy ficar pronto
echo Aguardando proxy ficar pronto...
timeout /t 15 /nobreak >nul

REM Verificar status
echo Verificando status dos serviços...
docker-compose ps

REM Testar health check
echo Testando health check do proxy...
curl -f http://localhost/health
if %errorlevel% neq 0 (
    echo AVISO: Health check falhou. Verifique logs com: docker-compose logs proxy
) else (
    echo SUCCESS: Proxy está funcionando!
)

echo.
echo =================================
echo         SETUP COMPLETO
echo =================================
echo.
echo Serviços disponíveis:
echo - Proxy:          http://localhost
echo - HTTPS Proxy:    https://localhost (dev cert)
echo - Health Check:   http://localhost/health
echo - Proxy Info:     http://localhost/proxy/info
echo - Swagger UI:     http://localhost/swagger
echo.
echo Para visualizar logs:
echo   docker-compose logs -f proxy
echo.
echo Para parar todos os serviços:
echo   docker-compose down
echo.
echo Para monitoramento:
echo   scripts\monitor-proxy.bat
echo.
pause
