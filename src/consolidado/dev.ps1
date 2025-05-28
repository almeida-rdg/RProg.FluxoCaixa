# PowerShell script para desenvolvimento da API Consolidado
# Uso: .\dev.ps1 [comando]

param(
    [Parameter(Position=0)]
    [string]$Command = "help"
)

# Cores para output
$Red = "Red"
$Green = "Green"
$Yellow = "Yellow"
$Blue = "Cyan"

function Write-Header {
    Write-Host "========================================" -ForegroundColor $Blue
    Write-Host "  RProg FluxoCaixa - API Consolidado   " -ForegroundColor $Blue
    Write-Host "========================================" -ForegroundColor $Blue
}

function Test-DockerRunning {
    try {
        docker info | Out-Null
        return $true
    }
    catch {
        Write-Host "❌ Docker não está rodando. Inicie o Docker e tente novamente." -ForegroundColor $Red
        return $false
    }
}

function Start-Services {
    Write-Host "🔨 Construindo e iniciando serviços..." -ForegroundColor $Yellow
    docker-compose -f docker-compose.dev.yaml up --build -d
    
    Write-Host "⏳ Aguardando serviços ficarem prontos..." -ForegroundColor $Yellow
    Start-Sleep -Seconds 30
    
    # Verificar se os serviços estão rodando
    $status = docker-compose -f docker-compose.dev.yaml ps
    if ($status -match "Up") {
        Write-Host "✅ Serviços iniciados com sucesso!" -ForegroundColor $Green
        Write-Host "📍 API Consolidado disponível em: http://localhost:8081" -ForegroundColor $Blue
        Write-Host "📍 Swagger UI: http://localhost:8081/swagger" -ForegroundColor $Blue
        Write-Host "📍 Health Check: http://localhost:8081/health" -ForegroundColor $Blue
    }
    else {
        Write-Host "❌ Erro ao iniciar serviços." -ForegroundColor $Red
        Show-Logs
    }
}

function Stop-Services {
    Write-Host "🛑 Parando serviços..." -ForegroundColor $Yellow
    docker-compose -f docker-compose.dev.yaml down
    Write-Host "✅ Serviços parados." -ForegroundColor $Green
}

function Show-Logs {
    Write-Host "📋 Mostrando logs do container consolidado..." -ForegroundColor $Yellow
    docker-compose -f docker-compose.dev.yaml logs -f consolidado-api-dev
}

function Invoke-Tests {
    Write-Host "🧪 Executando testes..." -ForegroundColor $Yellow
    Push-Location "RProg.FluxoCaixa.Consolidado.Test"
    try {
        dotnet test --verbosity normal
    }
    finally {
        Pop-Location
    }
}

function Invoke-Cleanup {
    Write-Host "🧹 Limpando recursos Docker..." -ForegroundColor $Yellow
    docker-compose -f docker-compose.dev.yaml down -v
    docker system prune -f
    Write-Host "✅ Limpeza concluída." -ForegroundColor $Green
}

function Show-Status {
    Write-Host "📊 Status dos serviços:" -ForegroundColor $Blue
    docker-compose -f docker-compose.dev.yaml ps
}

function Show-Help {
    Write-Host "Uso: .\dev.ps1 [comando]" -ForegroundColor $Blue
    Write-Host ""
    Write-Host "Comandos disponíveis:" -ForegroundColor $Yellow
    Write-Host "  start    - Constrói e inicia os serviços" -ForegroundColor $Green
    Write-Host "  stop     - Para os serviços" -ForegroundColor $Green
    Write-Host "  restart  - Reinicia os serviços" -ForegroundColor $Green
    Write-Host "  logs     - Mostra logs do container consolidado" -ForegroundColor $Green
    Write-Host "  test     - Executa testes unitários" -ForegroundColor $Green
    Write-Host "  cleanup  - Remove volumes e limpa recursos Docker" -ForegroundColor $Green
    Write-Host "  status   - Mostra status dos containers" -ForegroundColor $Green
    Write-Host ""
    Write-Host "Exemplos:" -ForegroundColor $Blue
    Write-Host "  .\dev.ps1 start    # Inicia o ambiente de desenvolvimento"
    Write-Host "  .\dev.ps1 logs     # Acompanha os logs em tempo real"
    Write-Host "  .\dev.ps1 test     # Executa a suíte de testes"
}

# Main execution
Write-Header

if (-not (Test-DockerRunning)) {
    exit 1
}

switch ($Command.ToLower()) {
    "start" {
        Start-Services
    }
    "stop" {
        Stop-Services
    }
    "restart" {
        Stop-Services
        Start-Services
    }
    "logs" {
        Show-Logs
    }
    "test" {
        Invoke-Tests
    }
    "cleanup" {
        Invoke-Cleanup
    }
    "status" {
        Show-Status
    }
    default {
        Show-Help
    }
}
