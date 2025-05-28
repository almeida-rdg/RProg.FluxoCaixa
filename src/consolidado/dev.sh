#!/bin/bash

# Script para facilitar o desenvolvimento da API Consolidado

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  RProg FluxoCaixa - API Consolidado   ${NC}"
echo -e "${BLUE}========================================${NC}"

# Verificar se Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker não está rodando. Inicie o Docker e tente novamente.${NC}"
    exit 1
fi

# Função para mostrar logs
show_logs() {
    echo -e "${YELLOW}📋 Mostrando logs do container consolidado...${NC}"
    docker-compose -f docker-compose.dev.yaml logs -f consolidado-api-dev
}

# Função para parar serviços
stop_services() {
    echo -e "${YELLOW}🛑 Parando serviços...${NC}"
    docker-compose -f docker-compose.dev.yaml down
    echo -e "${GREEN}✅ Serviços parados.${NC}"
}

# Função para construir e iniciar
start_services() {
    echo -e "${YELLOW}🔨 Construindo e iniciando serviços...${NC}"
    docker-compose -f docker-compose.dev.yaml up --build -d
    
    echo -e "${YELLOW}⏳ Aguardando serviços ficarem prontos...${NC}"
    sleep 30
    
    # Verificar se os serviços estão rodando
    if docker-compose -f docker-compose.dev.yaml ps | grep -q "Up"; then
        echo -e "${GREEN}✅ Serviços iniciados com sucesso!${NC}"
        echo -e "${BLUE}📍 API Consolidado disponível em: http://localhost:8081${NC}"
        echo -e "${BLUE}📍 Swagger UI: http://localhost:8081/swagger${NC}"
        echo -e "${BLUE}📍 Health Check: http://localhost:8081/health${NC}"
    else
        echo -e "${RED}❌ Erro ao iniciar serviços.${NC}"
        show_logs
    fi
}

# Função para executar testes
run_tests() {
    echo -e "${YELLOW}🧪 Executando testes...${NC}"
    cd RProg.FluxoCaixa.Consolidado.Test
    dotnet test --verbosity normal
    cd ..
}

# Função para limpar recursos Docker
cleanup() {
    echo -e "${YELLOW}🧹 Limpando recursos Docker...${NC}"
    docker-compose -f docker-compose.dev.yaml down -v
    docker system prune -f
    echo -e "${GREEN}✅ Limpeza concluída.${NC}"
}

# Menu principal
case "$1" in
    "start")
        start_services
        ;;
    "stop")
        stop_services
        ;;
    "restart")
        stop_services
        start_services
        ;;
    "logs")
        show_logs
        ;;
    "test")
        run_tests
        ;;
    "cleanup")
        cleanup
        ;;
    "status")
        echo -e "${BLUE}📊 Status dos serviços:${NC}"
        docker-compose -f docker-compose.dev.yaml ps
        ;;
    *)
        echo -e "${BLUE}Uso: $0 {start|stop|restart|logs|test|cleanup|status}${NC}"
        echo ""
        echo -e "${YELLOW}Comandos disponíveis:${NC}"
        echo -e "  ${GREEN}start${NC}    - Constrói e inicia os serviços"
        echo -e "  ${GREEN}stop${NC}     - Para os serviços"
        echo -e "  ${GREEN}restart${NC}  - Reinicia os serviços"
        echo -e "  ${GREEN}logs${NC}     - Mostra logs do container consolidado"
        echo -e "  ${GREEN}test${NC}     - Executa testes unitários"
        echo -e "  ${GREEN}cleanup${NC}  - Remove volumes e limpa recursos Docker"
        echo -e "  ${GREEN}status${NC}   - Mostra status dos containers"
        echo ""
        echo -e "${BLUE}Exemplos:${NC}"
        echo -e "  ./dev.sh start    # Inicia o ambiente de desenvolvimento"
        echo -e "  ./dev.sh logs     # Acompanha os logs em tempo real"
        echo -e "  ./dev.sh test     # Executa a suíte de testes"
        exit 1
        ;;
esac
