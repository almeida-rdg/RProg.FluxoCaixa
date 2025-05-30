# Script PowerShell para restore otimizado do RProg.FluxoCaixa
# Uso: .\restore-optimized.ps1 [-ClearCache]

param(
    [switch]$ClearCache = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Restore Otimizado - RProg.FluxoCaixa" -ForegroundColor Cyan  
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Verificando estrutura dos projetos..." -ForegroundColor Yellow

# Verificar se estamos no diretório correto
if (-not (Test-Path "Directory.Packages.props")) {
    Write-Host "ERRO: Execute este script a partir do diretório raiz do projeto" -ForegroundColor Red
    Write-Host "       Onde estão localizados os arquivos Directory.Packages.props e global.json" -ForegroundColor Red
    Read-Host "Pressione Enter para sair"
    exit 1
}

Write-Host "Estrutura encontrada. Iniciando restore otimizado..." -ForegroundColor Green
Write-Host ""

# Configurar variáveis de ambiente para otimização
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:NUGET_XMLDOC_MODE = "skip"

# Limpar cache local se solicitado
if ($ClearCache) {
    Write-Host "Limpando cache do NuGet..." -ForegroundColor Yellow
    dotnet nuget locals all --clear
    Write-Host "Cache limpo." -ForegroundColor Green
    Write-Host ""
}

# Lista de projetos para restore
$projetos = @(
    @{ Nome = "RProg.FluxoCaixa.Lancamentos"; Caminho = "src\lancamentos\RProg.FluxoCaixa.Lancamentos\RProg.FluxoCaixa.Lancamentos.csproj" },
    @{ Nome = "RProg.FluxoCaixa.Consolidado"; Caminho = "src\consolidado\RProg.FluxoCaixa.Consolidado\RProg.FluxoCaixa.Consolidado.csproj" },
    @{ Nome = "RProg.FluxoCaixa.Proxy"; Caminho = "src\proxy\RProg.FluxoCaixa.Proxy\RProg.FluxoCaixa.Proxy.csproj" },
    @{ Nome = "RProg.FluxoCaixa.Worker"; Caminho = "src\worker\RProg.FluxoCaixa.Worker\RProg.FluxoCaixa.Worker.csproj" },
    @{ Nome = "RProg.FluxoCaixa.Lancamentos.Test"; Caminho = "src\lancamentos\RProg.FluxoCaixa.Lancamentos.Test\RProg.FluxoCaixa.Lancamentos.Test.csproj" },
    @{ Nome = "RProg.FluxoCaixa.Consolidado.Test"; Caminho = "src\consolidado\RProg.FluxoCaixa.Consolidado.Test\RProg.FluxoCaixa.Consolidado.Test.csproj" },
    @{ Nome = "RProg.FluxoCaixa.Proxy.Test"; Caminho = "src\proxy\RProg.FluxoCaixa.Proxy.Test\RProg.FluxoCaixa.Proxy.Test.csproj" },
    @{ Nome = "RProg.FluxoCaixa.Worker.Test"; Caminho = "src\worker\RProg.FluxoCaixa.Worker.Test\RProg.FluxoCaixa.Worker.Test.csproj" }
)

Write-Host "Executando restore para todos os projetos..." -ForegroundColor Yellow
Write-Host ""

# Executar restore para cada projeto
$contador = 1
$total = $projetos.Count

foreach ($projeto in $projetos) {
    Write-Host "[$contador/$total] Restaurando $($projeto.Nome)..." -ForegroundColor Cyan
    
    $resultado = @(dotnet restore $projeto.Caminho --verbosity minimal)
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Concluído" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Falha no restore" -ForegroundColor Red
        Write-Host "  Erro: $resultado" -ForegroundColor Red
    }
    
    $contador++
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Restore concluído!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar se packages.lock.json foram criados
Write-Host "Verificando arquivos de lock gerados..." -ForegroundColor Yellow

$lockFiles = @(
    @{ Nome = "Lancamentos"; Caminho = "src\lancamentos\RProg.FluxoCaixa.Lancamentos\packages.lock.json" },
    @{ Nome = "Consolidado"; Caminho = "src\consolidado\RProg.FluxoCaixa.Consolidado\packages.lock.json" },
    @{ Nome = "Proxy"; Caminho = "src\proxy\RProg.FluxoCaixa.Proxy\packages.lock.json" },
    @{ Nome = "Worker"; Caminho = "src\worker\RProg.FluxoCaixa.Worker\packages.lock.json" }
)

foreach ($lockFile in $lockFiles) {
    if (Test-Path $lockFile.Caminho) {
        Write-Host "✓ Lock file criado para $($lockFile.Nome)" -ForegroundColor Green
    } else {
        Write-Host "⚠ Lock file NÃO criado para $($lockFile.Nome)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Dicas para otimização:" -ForegroundColor Cyan
Write-Host "- Os arquivos packages.lock.json devem ser commitados no Git" -ForegroundColor White
Write-Host "- Para builds de CI/CD, use --locked-mode para restore mais rápido" -ForegroundColor White
Write-Host "- Use este script sempre que adicionar novos pacotes" -ForegroundColor White
Write-Host ""

# Exibir estatísticas do cache
Write-Host "Estatísticas do cache NuGet:" -ForegroundColor Yellow
dotnet nuget locals global-packages --list
Write-Host ""

Read-Host "Pressione Enter para continuar"
