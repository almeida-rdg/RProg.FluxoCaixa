using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RProg.FluxoCaixa.Proxy.Middleware;

/// <summary>
/// Middleware para cache de respostas HTTP
/// </summary>
public class CacheMiddleware
{
    private readonly RequestDelegate _proximo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheMiddleware> _logger;
    
    // Configurações de cache
    private readonly TimeSpan _duracaoPadrao = TimeSpan.FromMinutes(5);
    private readonly HashSet<string> _metodosPermitidos = new() { "GET" };
    private readonly HashSet<string> _rotasComCache = new() 
    { 
        "/consolidado", 
        "/consolidado/saldo-diario",
        "/lancamentos"
    };

    public CacheMiddleware(RequestDelegate proximo, IMemoryCache cache, ILogger<CacheMiddleware> logger)
    {
        _proximo = proximo;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        // Verifica se deve aplicar cache
        if (!DeveAplicarCache(contexto))
        {
            await _proximo(contexto);
            return;
        }

        var chaveCache = GerarChaveCache(contexto);
          // Verifica se existe no cache
        if (_cache.TryGetValue(chaveCache, out RespostaCacheada? respostaCacheada) && respostaCacheada != null)
        {
            _logger.LogDebug("Cache HIT para {ChaveCache}", chaveCache);
            
            // Adiciona header indicando que veio do cache            contexto.Response.Headers["X-Cache"] = "HIT";
            contexto.Response.Headers["X-Cache-Key"] = chaveCache;
            
            // Retorna resposta do cache
            contexto.Response.StatusCode = respostaCacheada.StatusCode;
            contexto.Response.ContentType = respostaCacheada.ContentType;
              foreach (var header in respostaCacheada.Headers)
            {
                contexto.Response.Headers[header.Key] = header.Value;
            }
            
            await contexto.Response.WriteAsync(respostaCacheada.Conteudo);
            return;
        }

        _logger.LogDebug("Cache MISS para {ChaveCache}", chaveCache);
        
        // Intercepta a resposta
        var respostaOriginal = contexto.Response.Body;
        using var novaResposta = new MemoryStream();
        contexto.Response.Body = novaResposta;

        await _proximo(contexto);

        // Verifica se deve cachear a resposta
        if (DeveCachearResposta(contexto))
        {
            // Lê o conteúdo da resposta
            novaResposta.Seek(0, SeekOrigin.Begin);
            var conteudo = await new StreamReader(novaResposta).ReadToEndAsync();
            
            // Cria objeto para cache
            var objetoCache = new RespostaCacheada
            {
                StatusCode = contexto.Response.StatusCode,
                ContentType = contexto.Response.ContentType ?? "application/json",
                Conteudo = conteudo,
                Headers = contexto.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            };
            
            // Armazena no cache
            var opcoes = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ObterDuracaoCache(contexto.Request.Path),
                Priority = CacheItemPriority.Normal,
                Size = conteudo.Length
            };
            
            _cache.Set(chaveCache, objetoCache, opcoes);
            
            _logger.LogDebug("Resposta armazenada no cache: {ChaveCache}, Tamanho: {Tamanho} bytes", 
                chaveCache, conteudo.Length);
        }        // Adiciona headers informativos
        contexto.Response.Headers["X-Cache"] = "MISS";
        contexto.Response.Headers["X-Cache-Key"] = chaveCache;

        // Retorna resposta original
        novaResposta.Seek(0, SeekOrigin.Begin);
        await novaResposta.CopyToAsync(respostaOriginal);
    }

    private bool DeveAplicarCache(HttpContext contexto)
    {
        // Só aplica cache para métodos GET
        if (!_metodosPermitidos.Contains(contexto.Request.Method.ToUpper()))
            return false;

        // Verifica se é uma rota que deve ter cache
        var caminho = contexto.Request.Path.Value?.ToLower() ?? "";
        return _rotasComCache.Any(rota => caminho.StartsWith(rota));
    }

    private bool DeveCachearResposta(HttpContext contexto)
    {
        // Só cacheia respostas de sucesso
        return contexto.Response.StatusCode >= 200 && contexto.Response.StatusCode < 300;
    }

    private string GerarChaveCache(HttpContext contexto)
    {
        var elementos = new List<string>
        {
            contexto.Request.Method,
            contexto.Request.Path.Value ?? "",
            contexto.Request.QueryString.Value ?? ""
        };

        // Adiciona headers relevantes para o cache
        if (contexto.Request.Headers.ContainsKey("Authorization"))
        {
            var auth = contexto.Request.Headers["Authorization"].ToString();
            // Usa hash do token para não expor dados sensíveis
            elementos.Add($"auth:{CalcularHashSha256(auth)}");
        }

        var chaveCompleta = string.Join("|", elementos);
        return $"cache:{CalcularHashSha256(chaveCompleta)}";
    }

    private TimeSpan ObterDuracaoCache(string caminho)
    {
        return caminho.ToLower() switch
        {
            var p when p.Contains("saldo-diario") => TimeSpan.FromMinutes(10), // Cache mais longo para saldos
            var p when p.Contains("consolidado") => TimeSpan.FromMinutes(5),
            var p when p.Contains("lancamentos") => TimeSpan.FromMinutes(2),   // Cache menor para lançamentos
            _ => _duracaoPadrao
        };
    }

    private static string CalcularHashSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}

/// <summary>
/// Representa uma resposta armazenada em cache
/// </summary>
public class RespostaCacheada
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}
