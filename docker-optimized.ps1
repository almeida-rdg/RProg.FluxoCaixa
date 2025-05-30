# Script PowerShell para utilizar o ambiente Docker otimizado com cache compartilhado
# Este script garante que cada pacote NuGet seja baixado apenas uma vez

param(
    [Parameter(Position=0)]
    [ValidateSet("run", "rebuild", "cache", "stats", "clean", "help")]
    [string]$Command = "help",
    
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$AdditionalArgs
)

# Configurações
$ErrorActionPreference = "Stop"

# Função para log colorido
function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Header {
    Write-Host ""
    Write-Host "🚀 FluxoCaixa - Build Otimizado com Cache Compartilhado" -ForegroundColor Cyan
    Write-Host "=======================================================" -ForegroundColor Cyan
    Write-Host ""
}

# Verificar se estamos no diretório correto
function Test-ProjectDirectory {
    if (-not (Test-Path "src\Dockerfile.optimized")) {
        Write-Error "Este script deve ser executado no diretório raiz do projeto RProg.FluxoCaixa"
        exit 1
    }
}

# Função para criar cache compartilhado inicial
function Build-SharedCache {
    Write-Info "Criando cache compartilhado de pacotes NuGet..."
    
    try {
        # Build do stage 'build' para criar cache compartilhado
        docker build `
            -f src/Dockerfile.optimized `
            --target build `
            -t fluxocaixa/shared-cache:latest `
            .
        
        Write-Success "Cache compartilhado criado com sucesso!"
    }
    catch {
        Write-Error "Falha ao criar cache compartilhado: $($_.Exception.Message)"
        exit 1
    }
}

# Função principal para executar ambiente
function Start-OptimizedEnvironment {
    param([string[]]$Args)
    
    Write-Info "Verificando se cache compartilhado existe..."
    
    # Verificar se a imagem de cache existe
    $cacheExists = $false
    try {
        docker image inspect fluxocaixa/shared-cache:latest | Out-Null
        $cacheExists = $true
        Write-Success "Cache compartilhado encontrado!"
    }
    catch {
        Write-Warning "Cache compartilhado não encontrado. Criando..."
        Build-SharedCache
    }
    
    Write-Info "Iniciando ambiente Docker otimizado..."
    
    try {
        # Executar docker-compose otimizado
        $composeArgs = @("-f", "src/docker-compose.optimized.yaml", "up", "--build") + $Args
        & docker-compose $composeArgs
    }
    catch {
        Write-Error "Falha ao iniciar ambiente Docker: $($_.Exception.Message)"
        exit 1
    }
}

# Função para rebuild completo
function Invoke-RebuildAll {
    param([string[]]$Args)
    
    Write-Warning "Realizando rebuild completo..."
    
    # Remover imagens antigas
    Write-Info "Removendo imagens antigas..."
    try {
        docker-compose -f src/docker-compose.optimized.yaml down --rmi all --volumes --remove-orphans
    } catch {
        # Ignorar erros na limpeza
    }
    
    try {
        docker rmi fluxocaixa/shared-cache:latest
    } catch {
        # Ignorar se a imagem não existe
    }
    
    # Rebuild cache
    Build-SharedCache
    
    # Rebuild e executar
    Start-OptimizedEnvironment $Args
}

# Função para mostrar estatísticas de cache
function Show-CacheStats {
    Write-Info "Estatísticas do cache Docker:"
    Write-Host ""
    
    # Mostrar imagens relacionadas
    Write-Host "📦 Imagens FluxoCaixa:" -ForegroundColor Cyan
    try {
        docker images | Select-String -Pattern "(fluxocaixa|rprog)"
    }
    catch {
        Write-Host "Nenhuma imagem encontrada"
    }
    Write-Host ""
    
    # Mostrar uso de espaço
    Write-Host "💾 Uso de espaço Docker:" -ForegroundColor Cyan
    docker system df
    Write-Host ""
    
    # Mostrar cache de build
    Write-Host "🔧 Cache de build:" -ForegroundColor Cyan
    try {
        docker builder du
    }
    catch {
        Write-Host "BuildKit cache não disponível"
    }
}

# Função para limpeza
function Invoke-Clean {
    Write-Warning "Limpando cache e volumes Docker..."
    
    try {
        docker-compose -f src/docker-compose.optimized.yaml down --rmi all --volumes --remove-orphans
        docker system prune -af --volumes
        Write-Success "Limpeza concluída!"
    }
    catch {
        Write-Error "Erro durante limpeza: $($_.Exception.Message)"
    }
}

# Função para mostrar ajuda
function Show-Help {
    Write-Host ""
    Write-Host "🐳 Script de Build Otimizado - FluxoCaixa" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Uso: .\docker-optimized.ps1 [COMANDO] [OPÇÕES]"
    Write-Host ""
    Write-Host "COMANDOS:"
    Write-Host "  run      - Executar ambiente otimizado (padrão: detached)"
    Write-Host "  rebuild  - Rebuild completo forçando recriação de cache"
    Write-Host "  cache    - Criar apenas o cache compartilhado"
    Write-Host "  stats    - Mostrar estatísticas de cache e uso de espaço"
    Write-Host "  clean    - Limpar todos os caches e volumes Docker"
    Write-Host "  help     - Mostrar esta ajuda"
    Write-Host ""
    Write-Host "OPÇÕES PARA 'run' e 'rebuild':"
    Write-Host "  -d       - Executar em background (detached)"
    Write-Host "  --scale SERVICE=NUM - Escalar serviço específico"
    Write-Host ""
    Write-Host "EXEMPLOS:"
    Write-Host "  .\docker-optimized.ps1 run -d                    # Executar em background"
    Write-Host "  .\docker-optimized.ps1 run --scale worker=3      # Executar com 3 workers"
    Write-Host "  .\docker-optimized.ps1 rebuild                   # Rebuild completo"
    Write-Host "  .\docker-optimized.ps1 stats                     # Ver estatísticas"
    Write-Host ""
    Write-Host "OTIMIZAÇÕES IMPLEMENTADAS:" -ForegroundColor Green
    Write-Host "✅ Cache compartilhado de pacotes NuGet"
    Write-Host "✅ Cada pacote baixado apenas uma vez"
    Write-Host "✅ Layers Docker otimizadas"
    Write-Host "✅ Imagens de produção (sem testes)"
    Write-Host "✅ Build paralelo de serviços"
    Write-Host ""
}

# Script principal
Write-Header
Test-ProjectDirectory

switch ($Command) {
    "run" {
        Start-OptimizedEnvironment $AdditionalArgs
    }
    "rebuild" {
        Invoke-RebuildAll $AdditionalArgs
    }
    "cache" {
        Build-SharedCache
    }
    "stats" {
        Show-CacheStats
    }
    "clean" {
        Invoke-Clean
    }
    "help" {
        Show-Help
    }
    default {
        Show-Help
    }
}
