#!/bin/bash

# Auto-scaling script for RProg.FluxoCaixa.Proxy
# Monitora métricas e escala containers automaticamente

set -e

# Configurações
CPU_THRESHOLD=70
MEMORY_THRESHOLD=70
MIN_REPLICAS=1
MAX_REPLICAS=5
SCALE_UP_COOLDOWN=300    # 5 minutos
SCALE_DOWN_COOLDOWN=600  # 10 minutos
LOG_FILE="/var/log/proxy-autoscale.log"

# Arquivos de estado
STATE_DIR="/tmp/proxy-autoscale"
LAST_SCALE_UP_FILE="$STATE_DIR/last_scale_up"
LAST_SCALE_DOWN_FILE="$STATE_DIR/last_scale_down"

# Criar diretório de estado
mkdir -p "$STATE_DIR"

# Funções de log
log() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log_action() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - ACTION: $1" | tee -a "$LOG_FILE"
}

# Obter métricas de CPU de um container
get_container_cpu_usage() {
    local container_name="$1"
    
    # Usar docker stats para obter CPU usage
    docker stats --no-stream --format "table {{.CPUPerc}}" "$container_name" | tail -n 1 | sed 's/%//'
}

# Obter métricas de memória de um container
get_container_memory_usage() {
    local container_name="$1"
    
    # Usar docker stats para obter Memory usage
    local mem_usage=$(docker stats --no-stream --format "table {{.MemUsage}}" "$container_name" | tail -n 1)
    local used=$(echo "$mem_usage" | cut -d'/' -f1 | sed 's/[^0-9.]*//g')
    local total=$(echo "$mem_usage" | cut -d'/' -f2 | sed 's/[^0-9.]*//g')
    
    # Calcular percentual
    echo "scale=2; $used / $total * 100" | bc
}

# Obter número atual de réplicas
get_current_replicas() {
    local service_name="$1"
    docker service ls --filter "name=$service_name" --format "{{.Replicas}}" | cut -d'/' -f1
}

# Verificar se está em período de cooldown
is_in_cooldown() {
    local action="$1"
    local cooldown_file=""
    local cooldown_period=""
    
    if [ "$action" == "up" ]; then
        cooldown_file="$LAST_SCALE_UP_FILE"
        cooldown_period=$SCALE_UP_COOLDOWN
    else
        cooldown_file="$LAST_SCALE_DOWN_FILE"
        cooldown_period=$SCALE_DOWN_COOLDOWN
    fi
    
    if [ -f "$cooldown_file" ]; then
        local last_action=$(cat "$cooldown_file")
        local current_time=$(date +%s)
        local time_diff=$((current_time - last_action))
        
        if [ $time_diff -lt $cooldown_period ]; then
            log "Still in cooldown for scale $action. Remaining: $((cooldown_period - time_diff))s"
            return 0
        fi
    fi
    
    return 1
}

# Escalar serviço
scale_service() {
    local service_name="$1"
    local new_replicas="$2"
    local action="$3"
    
    log_action "Scaling $service_name to $new_replicas replicas"
    
    # Atualizar serviço Docker Swarm ou Docker Compose
    if docker service ls | grep -q "$service_name"; then
        # Docker Swarm
        docker service scale "$service_name=$new_replicas"
    else
        # Docker Compose
        docker-compose up -d --scale "$service_name=$new_replicas"
    fi
    
    # Registrar timestamp da ação
    if [ "$action" == "up" ]; then
        echo "$(date +%s)" > "$LAST_SCALE_UP_FILE"
    else
        echo "$(date +%s)" > "$LAST_SCALE_DOWN_FILE"
    fi
    
    log_action "Successfully scaled $service_name to $new_replicas replicas"
}

# Monitorar e escalar APIs
monitor_and_scale_api() {
    local api_name="$1"
    local container_pattern="$2"
    
    log "Monitoring $api_name..."
    
    # Obter containers da API
    local containers=$(docker ps --filter "name=$container_pattern" --format "{{.Names}}")
    
    if [ -z "$containers" ]; then
        log "No containers found for $api_name"
        return
    fi
    
    local total_cpu=0
    local total_memory=0
    local container_count=0
    
    # Calcular médias de CPU e memória
    for container in $containers; do
        local cpu=$(get_container_cpu_usage "$container")
        local memory=$(get_container_memory_usage "$container")
        
        if [ -n "$cpu" ] && [ -n "$memory" ]; then
            total_cpu=$(echo "scale=2; $total_cpu + $cpu" | bc)
            total_memory=$(echo "scale=2; $total_memory + $memory" | bc)
            container_count=$((container_count + 1))
            
            log "$container - CPU: ${cpu}%, Memory: ${memory}%"
        fi
    done
    
    if [ $container_count -eq 0 ]; then
        log "No valid metrics collected for $api_name"
        return
    fi
    
    # Calcular médias
    local avg_cpu=$(echo "scale=2; $total_cpu / $container_count" | bc)
    local avg_memory=$(echo "scale=2; $total_memory / $container_count" | bc)
    
    log "$api_name averages - CPU: ${avg_cpu}%, Memory: ${avg_memory}%"
    
    # Decisão de escalabilidade
    local current_replicas=$container_count
    local should_scale_up=false
    local should_scale_down=false
    
    # Verificar se deve escalar para cima
    if (( $(echo "$avg_cpu > $CPU_THRESHOLD" | bc -l) )) || (( $(echo "$avg_memory > $MEMORY_THRESHOLD" | bc -l) )); then
        if [ $current_replicas -lt $MAX_REPLICAS ] && ! is_in_cooldown "up"; then
            should_scale_up=true
        fi
    fi
    
    # Verificar se deve escalar para baixo
    if (( $(echo "$avg_cpu < 30" | bc -l) )) && (( $(echo "$avg_memory < 30" | bc -l) )); then
        if [ $current_replicas -gt $MIN_REPLICAS ] && ! is_in_cooldown "down"; then
            should_scale_down=true
        fi
    fi
    
    # Executar escala
    if [ "$should_scale_up" = true ]; then
        local new_replicas=$((current_replicas + 1))
        log_action "High resource usage detected for $api_name (CPU: ${avg_cpu}%, Memory: ${avg_memory}%)"
        scale_service "$api_name" "$new_replicas" "up"
    elif [ "$should_scale_down" = true ]; then
        local new_replicas=$((current_replicas - 1))
        log_action "Low resource usage detected for $api_name (CPU: ${avg_cpu}%, Memory: ${avg_memory}%)"
        scale_service "$api_name" "$new_replicas" "down"
    else
        log "$api_name resource usage within normal range"
    fi
}

# Verificar saúde dos containers antes de escalar
check_container_health() {
    local container_name="$1"
    
    # Verificar se container está rodando
    if ! docker ps --filter "name=$container_name" --filter "status=running" | grep -q "$container_name"; then
        return 1
    fi
    
    # Verificar health check se disponível
    local health_status=$(docker inspect "$container_name" --format='{{.State.Health.Status}}' 2>/dev/null || echo "unknown")
    
    if [ "$health_status" == "healthy" ] || [ "$health_status" == "unknown" ]; then
        return 0
    fi
    
    return 1
}

# Remover containers não saudáveis
remove_unhealthy_containers() {
    log "Checking for unhealthy containers..."
    
    # Verificar containers das APIs
    local apis=("lancamentos-api" "consolidado-api")
    
    for api in "${apis[@]}"; do
        local containers=$(docker ps -a --filter "name=$api" --format "{{.Names}}")
        
        for container in $containers; do
            if ! check_container_health "$container"; then
                log_action "Removing unhealthy container: $container"
                docker stop "$container" 2>/dev/null || true
                docker rm "$container" 2>/dev/null || true
                
                # Recrear container
                log_action "Recreating container for $api"
                docker-compose up -d "$api"
            fi
        done
    done
}

# Monitorar proxy especificamente
monitor_proxy() {
    log "Monitoring proxy container..."
    
    local proxy_container="fluxo-proxy"
    
    if ! check_container_health "$proxy_container"; then
        log_action "Proxy container is unhealthy, restarting..."
        docker-compose restart proxy
        return
    fi
    
    local cpu=$(get_container_cpu_usage "$proxy_container")
    local memory=$(get_container_memory_usage "$proxy_container")
    
    log "Proxy metrics - CPU: ${cpu}%, Memory: ${memory}%"
    
    # Alertar se proxy está com alto uso de recursos
    if (( $(echo "$cpu > 80" | bc -l) )) || (( $(echo "$memory > 80" | bc -l) )); then
        log_action "WARNING: Proxy high resource usage - CPU: ${cpu}%, Memory: ${memory}%"
        # Aqui poderia enviar alerta ou reiniciar proxy se necessário
    fi
}

# Função principal
main() {
    log "=== Starting auto-scaling monitoring ==="
    
    # Verificar se Docker está rodando
    if ! docker info > /dev/null 2>&1; then
        log "ERROR: Docker is not running"
        exit 1
    fi
    
    # Verificar se há containers rodando
    if [ -z "$(docker ps -q)" ]; then
        log "No containers running, skipping auto-scaling"
        exit 0
    fi
    
    # Monitorar proxy
    monitor_proxy
    
    # Monitorar e escalar APIs
    monitor_and_scale_api "lancamentos-api" "lancamentos"
    monitor_and_scale_api "consolidado-api" "consolidado"
    
    # Remover containers não saudáveis
    remove_unhealthy_containers
    
    log "=== Auto-scaling monitoring completed ==="
}

# Executar função principal
main "$@"
