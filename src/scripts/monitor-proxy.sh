#!/bin/bash

# Monitoring script for RProg.FluxoCaixa.Proxy
# Executa verificações de saúde e envia alertas se necessário

set -e

# Configurações
PROXY_URL="http://localhost"
ALERT_EMAIL="admin@rprog.com"
LOG_FILE="/var/log/proxy-monitor.log"
SLACK_WEBHOOK=""  # Configure se usar Slack

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Funções de log
log() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}$(date '+%Y-%m-%d %H:%M:%S') - ERROR: $1${NC}" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}$(date '+%Y-%m-%d %H:%M:%S') - SUCCESS: $1${NC}" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}$(date '+%Y-%m-%d %H:%M:%S') - WARNING: $1${NC}" | tee -a "$LOG_FILE"
}

# Função para enviar alertas
send_alert() {
    local message="$1"
    local severity="$2"
    
    log_error "ALERT: $message"
    
    # Email (se configurado)
    if [ -n "$ALERT_EMAIL" ]; then
        echo "$message" | mail -s "FluxoCaixa Proxy Alert - $severity" "$ALERT_EMAIL"
    fi
    
    # Slack (se configurado)
    if [ -n "$SLACK_WEBHOOK" ]; then
        curl -X POST -H 'Content-type: application/json' \
            --data "{\"text\":\"🚨 FluxoCaixa Proxy Alert [$severity]: $message\"}" \
            "$SLACK_WEBHOOK"
    fi
}

# Verificação de Health Check
check_health() {
    log "Checking proxy health..."
    
    if curl -f -s "$PROXY_URL/health" > /dev/null; then
        log_success "Proxy health check passed"
        return 0
    else
        log_error "Proxy health check failed"
        send_alert "Proxy health check failed at $PROXY_URL/health" "CRITICAL"
        return 1
    fi
}

# Verificação de performance
check_performance() {
    log "Checking proxy performance..."
    
    # Medir tempo de resposta
    response_time=$(curl -o /dev/null -s -w "%{time_total}" "$PROXY_URL/health")
    response_time_ms=$(echo "$response_time * 1000" | bc)
    
    log "Response time: ${response_time_ms}ms"
    
    # Alertar se tempo de resposta > 5000ms
    if (( $(echo "$response_time > 5.0" | bc -l) )); then
        log_warning "High response time: ${response_time_ms}ms"
        send_alert "High response time detected: ${response_time_ms}ms" "WARNING"
        return 1
    fi
    
    return 0
}

# Verificação de containers Docker
check_containers() {
    log "Checking Docker containers..."
    
    # Verificar se proxy está rodando
    if ! docker ps --filter "name=fluxo-proxy" --filter "status=running" | grep -q fluxo-proxy; then
        log_error "Proxy container not running"
        send_alert "Proxy container is not running" "CRITICAL"
        return 1
    fi
    
    # Verificar APIs
    local apis=("lancamentos-api" "consolidado-api")
    for api in "${apis[@]}"; do
        if ! docker ps --filter "name=$api" --filter "status=running" | grep -q "$api"; then
            log_error "$api container not running"
            send_alert "$api container is not running" "HIGH"
        fi
    done
    
    log_success "All containers are running"
    return 0
}

# Verificação de recursos
check_resources() {
    log "Checking system resources..."
    
    # CPU usage
    cpu_usage=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
    log "CPU usage: ${cpu_usage}%"
    
    if (( $(echo "$cpu_usage > 80" | bc -l) )); then
        log_warning "High CPU usage: ${cpu_usage}%"
        send_alert "High CPU usage detected: ${cpu_usage}%" "WARNING"
    fi
    
    # Memory usage
    memory_usage=$(free | grep Mem | awk '{printf "%.2f", $3/$2 * 100}')
    log "Memory usage: ${memory_usage}%"
    
    if (( $(echo "$memory_usage > 80" | bc -l) )); then
        log_warning "High memory usage: ${memory_usage}%"
        send_alert "High memory usage detected: ${memory_usage}%" "WARNING"
    fi
    
    # Disk usage
    disk_usage=$(df / | awk 'NR==2 {print $5}' | cut -d'%' -f1)
    log "Disk usage: ${disk_usage}%"
    
    if (( disk_usage > 85 )); then
        log_warning "High disk usage: ${disk_usage}%"
        send_alert "High disk usage detected: ${disk_usage}%" "WARNING"
    fi
}

# Verificação de logs de erro
check_error_logs() {
    log "Checking for recent errors..."
    
    # Verificar logs dos últimos 5 minutos
    error_count=$(docker logs fluxo-proxy --since 5m 2>&1 | grep -i "error\|exception\|failed" | wc -l)
    
    if [ "$error_count" -gt 10 ]; then
        log_warning "High error count in logs: $error_count errors in last 5 minutes"
        send_alert "High error rate detected: $error_count errors in last 5 minutes" "WARNING"
    fi
    
    # Verificar rate limiting
    rate_limit_count=$(docker logs fluxo-proxy --since 5m 2>&1 | grep -i "rate.limit" | wc -l)
    
    if [ "$rate_limit_count" -gt 50 ]; then
        log_warning "High rate limiting activity: $rate_limit_count events in last 5 minutes"
        send_alert "High rate limiting activity: $rate_limit_count events" "INFO"
    fi
}

# Verificação de APIs backend
check_backend_apis() {
    log "Checking backend APIs..."
    
    # Testar via proxy
    if curl -f -s "$PROXY_URL/api/lancamentos/health" > /dev/null; then
        log_success "Lancamentos API accessible via proxy"
    else
        log_error "Lancamentos API not accessible via proxy"
        send_alert "Lancamentos API not accessible via proxy" "HIGH"
    fi
    
    if curl -f -s "$PROXY_URL/api/consolidado/health" > /dev/null; then
        log_success "Consolidado API accessible via proxy"
    else
        log_error "Consolidado API not accessible via proxy"
        send_alert "Consolidado API not accessible via proxy" "HIGH"
    fi
}

# Verificação de SSL/TLS
check_ssl() {
    log "Checking SSL certificate..."
    
    if openssl s_client -connect localhost:443 -servername localhost < /dev/null 2>/dev/null | grep -q "Verify return code: 0"; then
        log_success "SSL certificate is valid"
    else
        log_warning "SSL certificate issues detected"
        # Não enviar alerta crítico para certificados auto-assinados em dev
        # send_alert "SSL certificate issues detected" "WARNING"
    fi
    
    # Verificar expiração do certificado
    cert_expiry=$(echo | openssl s_client -connect localhost:443 -servername localhost 2>/dev/null | openssl x509 -noout -enddate 2>/dev/null | cut -d= -f2)
    
    if [ -n "$cert_expiry" ]; then
        expiry_epoch=$(date -d "$cert_expiry" +%s)
        current_epoch=$(date +%s)
        days_until_expiry=$(( (expiry_epoch - current_epoch) / 86400 ))
        
        log "SSL certificate expires in $days_until_expiry days"
        
        if [ "$days_until_expiry" -lt 30 ]; then
            log_warning "SSL certificate expires soon: $days_until_expiry days"
            send_alert "SSL certificate expires in $days_until_expiry days" "WARNING"
        fi
    fi
}

# Relatório de status
generate_status_report() {
    log "Generating status report..."
    
    echo "=== FluxoCaixa Proxy Status Report ===" > /tmp/status_report.txt
    echo "Timestamp: $(date)" >> /tmp/status_report.txt
    echo "" >> /tmp/status_report.txt
    
    # Containers status
    echo "Container Status:" >> /tmp/status_report.txt
    docker ps --filter "name=fluxo" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" >> /tmp/status_report.txt
    echo "" >> /tmp/status_report.txt
    
    # Resource usage
    echo "Resource Usage:" >> /tmp/status_report.txt
    echo "CPU: $(top -bn1 | grep "Cpu(s)" | awk '{print $2}')" >> /tmp/status_report.txt
    echo "Memory: $(free -h | grep Mem | awk '{print $3 "/" $2}')" >> /tmp/status_report.txt
    echo "Disk: $(df -h / | awk 'NR==2 {print $3 "/" $2 " (" $5 " used)"}')" >> /tmp/status_report.txt
    echo "" >> /tmp/status_report.txt
    
    # Recent logs summary
    echo "Recent Activity (last 1 hour):" >> /tmp/status_report.txt
    echo "Total requests: $(docker logs fluxo-proxy --since 1h 2>&1 | grep -c "GET\|POST\|PUT\|DELETE" || echo "0")" >> /tmp/status_report.txt
    echo "Errors: $(docker logs fluxo-proxy --since 1h 2>&1 | grep -ci "error\|exception" || echo "0")" >> /tmp/status_report.txt
    echo "Rate limited: $(docker logs fluxo-proxy --since 1h 2>&1 | grep -ci "rate.limit" || echo "0")" >> /tmp/status_report.txt
    
    log "Status report generated at /tmp/status_report.txt"
    
    # Enviar relatório por email se configurado
    if [ -n "$ALERT_EMAIL" ] && [ "$1" == "email" ]; then
        cat /tmp/status_report.txt | mail -s "FluxoCaixa Proxy Status Report" "$ALERT_EMAIL"
        log "Status report sent to $ALERT_EMAIL"
    fi
}

# Função principal
main() {
    local check_type="${1:-all}"
    
    log "=== Starting proxy monitoring - Check type: $check_type ==="
    
    case "$check_type" in
        "health")
            check_health
            ;;
        "performance")
            check_performance
            ;;
        "containers")
            check_containers
            ;;
        "resources")
            check_resources
            ;;
        "logs")
            check_error_logs
            ;;
        "apis")
            check_backend_apis
            ;;
        "ssl")
            check_ssl
            ;;
        "report")
            generate_status_report "$2"
            ;;
        "all")
            check_health && \
            check_containers && \
            check_resources && \
            check_error_logs && \
            check_backend_apis && \
            check_ssl && \
            check_performance
            ;;
        *)
            echo "Usage: $0 [health|performance|containers|resources|logs|apis|ssl|report|all]"
            exit 1
            ;;
    esac
    
    log "=== Monitoring completed ==="
}

# Executar função principal
main "$@"
