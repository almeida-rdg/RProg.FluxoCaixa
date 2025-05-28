namespace RProg.FluxoCaixa.Proxy.Services;

/// <summary>
/// Métricas de um container
/// </summary>
public class MetricasContainer
{
    public string ContainerId { get; set; } = string.Empty;
    
    public double CpuUsagePercent { get; set; }
    
    public double MemoryUsagePercent { get; set; }
    
    public long MemoryUsageBytes { get; set; }
    
    public long MemoryLimitBytes { get; set; }
    
    public double NetworkRxBytes { get; set; }
    
    public double NetworkTxBytes { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public TimeSpan Uptime { get; set; }
}
