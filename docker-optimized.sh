#!/bin/bash
# Script para utilizar o ambiente Docker otimizado com cache compartilhado
# Este script garante que cada pacote NuGet seja baixado apenas uma vez

set -e

echo "🚀 FluxoCaixa - Build Otimizado com Cache Compartilhado"
echo "======================================================="

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Função para log colorido
log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Verificar se estamos no diretório correto
if [ ! -f "src/Dockerfile.optimized" ]; then
    log_error "Este script deve ser executado no diretório raiz do projeto RProg.FluxoCaixa"
    exit 1
fi

# Função para criar cache compartilhado inicial
build_shared_cache() {
    log_info "Criando cache compartilhado de pacotes NuGet..."
    
    # Build do stage 'build' para criar cache compartilhado
    docker build \
        -f src/Dockerfile.optimized \
        --target build \
        -t fluxocaixa/shared-cache:latest \
        . || {
        log_error "Falha ao criar cache compartilhado"
        exit 1
    }
    
    log_success "Cache compartilhado criado com sucesso!"
}

# Função principal para executar ambiente
run_optimized() {
    log_info "Verificando se cache compartilhado existe..."
    
    if ! docker image inspect fluxocaixa/shared-cache:latest >/dev/null 2>&1; then
        log_warning "Cache compartilhado não encontrado. Criando..."
        build_shared_cache
    else
        log_success "Cache compartilhado encontrado!"
    fi
    
    log_info "Iniciando ambiente Docker otimizado..."
    
    # Executar docker-compose otimizado
    docker-compose -f src/docker-compose.optimized.yaml up --build "$@" || {
        log_error "Falha ao iniciar ambiente Docker"
        exit 1
    }
}

# Função para rebuild completo
rebuild_all() {
    log_warning "Realizando rebuild completo..."
    
    # Remover imagens antigas
    log_info "Removendo imagens antigas..."
    docker-compose -f src/docker-compose.optimized.yaml down --rmi all --volumes --remove-orphans || true
    docker rmi fluxocaixa/shared-cache:latest 2>/dev/null || true
    
    # Rebuild cache
    build_shared_cache
    
    # Rebuild e executar
    run_optimized "$@"
}

# Função para mostrar estatísticas de cache
show_cache_stats() {
    log_info "Estatísticas do cache Docker:"
    echo ""
    
    # Mostrar imagens relacionadas
    echo "📦 Imagens FluxoCaixa:"
    docker images | grep -E "(fluxocaixa|rprog)" || echo "Nenhuma imagem encontrada"
    echo ""
    
    # Mostrar uso de espaço
    echo "💾 Uso de espaço Docker:"
    docker system df
    echo ""
    
    # Mostrar cache de build
    echo "🔧 Cache de build:"
    docker builder du || echo "BuildKit cache não disponível"
}

# Menu principal
case "${1:-help}" in
    "run")
        run_optimized "${@:2}"
        ;;
    "rebuild")
        rebuild_all "${@:2}"
        ;;
    "cache")
        build_shared_cache
        ;;
    "stats")
        show_cache_stats
        ;;
    "clean")
        log_warning "Limpando cache e volumes Docker..."
        docker-compose -f src/docker-compose.optimized.yaml down --rmi all --volumes --remove-orphans
        docker system prune -af --volumes
        log_success "Limpeza concluída!"
        ;;
    "help"|*)
        echo ""
        echo "🐳 Script de Build Otimizado - FluxoCaixa"
        echo ""
        echo "Uso: $0 [COMANDO] [OPÇÕES]"
        echo ""
        echo "COMANDOS:"
        echo "  run      - Executar ambiente otimizado (padrão: detached)"
        echo "  rebuild  - Rebuild completo forçando recriação de cache"
        echo "  cache    - Criar apenas o cache compartilhado"
        echo "  stats    - Mostrar estatísticas de cache e uso de espaço"
        echo "  clean    - Limpar todos os caches e volumes Docker"
        echo "  help     - Mostrar esta ajuda"
        echo ""
        echo "OPÇÕES PARA 'run' e 'rebuild':"
        echo "  -d       - Executar em background (detached)"
        echo "  --scale SERVICE=NUM - Escalar serviço específico"
        echo ""
        echo "EXEMPLOS:"
        echo "  $0 run -d                    # Executar em background"
        echo "  $0 run --scale worker=3      # Executar com 3 workers"
        echo "  $0 rebuild                   # Rebuild completo"
        echo "  $0 stats                     # Ver estatísticas"
        echo ""
        echo "OTIMIZAÇÕES IMPLEMENTADAS:"
        echo "✅ Cache compartilhado de pacotes NuGet"
        echo "✅ Cada pacote baixado apenas uma vez"
        echo "✅ Layers Docker otimizadas"
        echo "✅ Imagens de produção (sem testes)"
        echo "✅ Build paralelo de serviços"
        echo ""
        ;;
esac
