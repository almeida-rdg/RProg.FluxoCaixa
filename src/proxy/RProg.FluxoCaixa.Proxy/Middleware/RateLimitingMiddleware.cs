using System.Collections.Concurrent;
using System.Net;

namespace RProg.FluxoCaixa.Proxy.Middleware;

/// <summary>
/// Middleware para controle de taxa de requisições e proteção contra ataques DDoS
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _proximo;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _clientes;
    private readonly Timer _limpezaTimer;

    // Configurações de rate limiting
    private readonly int _limitePorMinuto = 60;
    private readonly int _limitePorSegundo = 10;
    private readonly TimeSpan _janelaTempo = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate proximo, ILogger<RateLimitingMiddleware> logger)
    {
        _proximo = proximo;
        _logger = logger;
        _clientes = new ConcurrentDictionary<string, RateLimitInfo>();
        
        // Timer para limpeza periódica dos dados antigos
        _limpezaTimer = new Timer(LimparDadosAntigos, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        var enderecoIp = ObterEnderecoIp(contexto);
        var agora = DateTime.UtcNow;

        var infoRateLimit = _clientes.GetOrAdd(enderecoIp, _ => new RateLimitInfo());

        // Verifica se o cliente está bloqueado
        if (infoRateLimit.EstaBloqueado && agora < infoRateLimit.BloqueadoAte)
        {
            _logger.LogWarning("Cliente {EnderecoIp} bloqueado até {BloqueadoAte}", enderecoIp, infoRateLimit.BloqueadoAte);
            contexto.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await contexto.Response.WriteAsync("Muitas requisições. Tente novamente mais tarde.");
            return;
        }

        // Remove bloqueio se o tempo expirou
        if (infoRateLimit.EstaBloqueado && agora >= infoRateLimit.BloqueadoAte)
        {
            infoRateLimit.EstaBloqueado = false;
            infoRateLimit.BloqueadoAte = null;
        }

        // Limpa requisições antigas
        infoRateLimit.Requisicoes.RemoveAll(r => agora - r > _janelaTempo);

        // Verifica limite de requisições
        if (infoRateLimit.Requisicoes.Count >= _limitePorMinuto)
        {
            // Bloqueia o cliente por 5 minutos
            infoRateLimit.EstaBloqueado = true;
            infoRateLimit.BloqueadoAte = agora.AddMinutes(5);
            
            _logger.LogWarning("Cliente {EnderecoIp} excedeu limite de requisições e foi bloqueado", enderecoIp);
            
            contexto.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await contexto.Response.WriteAsync("Limite de requisições excedido. Bloqueado por 5 minutos.");
            return;
        }

        // Verifica limite por segundo (último segundo)
        var requisicoesUltimoSegundo = infoRateLimit.Requisicoes.Count(r => agora - r < TimeSpan.FromSeconds(1));
        if (requisicoesUltimoSegundo >= _limitePorSegundo)
        {
            _logger.LogWarning("Cliente {EnderecoIp} excedeu limite de requisições por segundo", enderecoIp);
            contexto.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await contexto.Response.WriteAsync("Muitas requisições por segundo. Diminua a frequência.");
            return;
        }

        // Adiciona a requisição atual
        infoRateLimit.Requisicoes.Add(agora);        // Adiciona headers informativos
        contexto.Response.Headers["X-RateLimit-Limit"] = _limitePorMinuto.ToString();
        contexto.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, _limitePorMinuto - infoRateLimit.Requisicoes.Count).ToString();
        contexto.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)agora.Add(_janelaTempo)).ToUnixTimeSeconds().ToString();

        await _proximo(contexto);
    }

    private string ObterEnderecoIp(HttpContext contexto)
    {
        // Verifica headers de proxy
        var enderecoIp = contexto.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(enderecoIp))
        {
            enderecoIp = contexto.Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(enderecoIp))
        {
            enderecoIp = contexto.Connection.RemoteIpAddress?.ToString();
        }

        return enderecoIp ?? "unknown";
    }

    private void LimparDadosAntigos(object? state)
    {
        var agora = DateTime.UtcNow;
        var chavesParaRemover = new List<string>();

        foreach (var cliente in _clientes)
        {
            var info = cliente.Value;
            
            // Remove requisições antigas
            info.Requisicoes.RemoveAll(r => agora - r > _janelaTempo);
            
            // Remove clientes inativos há mais de 1 hora
            if (!info.Requisicoes.Any() && !info.EstaBloqueado)
            {
                chavesParaRemover.Add(cliente.Key);
            }
        }

        foreach (var chave in chavesParaRemover)
        {
            _clientes.TryRemove(chave, out _);
        }

        _logger.LogDebug("Limpeza de rate limiting: {ClientesRemovidos} clientes removidos", chavesParaRemover.Count);
    }

    public void Dispose()
    {
        _limpezaTimer?.Dispose();
    }
}

/// <summary>
/// Informações de rate limiting por cliente
/// </summary>
public class RateLimitInfo
{
    public List<DateTime> Requisicoes { get; set; } = new();
    public bool EstaBloqueado { get; set; }
    public DateTime? BloqueadoAte { get; set; }
}
