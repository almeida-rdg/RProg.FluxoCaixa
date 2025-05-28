namespace RProg.FluxoCaixa.Proxy.Services;

/// <summary>
/// Status de um container
/// </summary>
public class StatusContainer
{
    public string Id { get; set; } = string.Empty;
    
    public string Nome { get; set; } = string.Empty;
    
    public TipoContainer Tipo { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public bool Saudavel { get; set; }
    
    public DateTime UltimaVerificacao { get; set; }
    
    public DateTime UltimaVezSaudavel { get; set; } = DateTime.UtcNow;
    
    public bool DeveSerRecriado { get; set; }
    
    public MetricasContainer? Metricas { get; set; }
}
