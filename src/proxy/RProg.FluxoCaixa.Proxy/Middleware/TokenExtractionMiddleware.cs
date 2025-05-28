namespace RProg.FluxoCaixa.Proxy.Middleware;

/// <summary>
/// Middleware para extrair e processar tokens JWT das requisições
/// </summary>
public class TokenExtractionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenExtractionMiddleware> _logger;

    public TokenExtractionMiddleware(RequestDelegate next, ILogger<TokenExtractionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Extrair token do header Authorization
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // Adicionar token ao contexto para uso posterior
                context.Items["AuthToken"] = token;
                
                _logger.LogDebug("Token JWT extraído da requisição para {Path}", context.Request.Path);
            }
              // Adicionar headers de rastreamento
            if (!context.Request.Headers.ContainsKey("X-Request-ID"))
            {
                var requestId = Guid.NewGuid().ToString();
                context.Request.Headers["X-Request-ID"] = requestId;
                context.Items["RequestId"] = requestId;
                
                _logger.LogDebug("Request ID gerado: {RequestId} para {Path}", requestId, context.Request.Path);
            }
            
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no TokenExtractionMiddleware para {Path}", context.Request.Path);
            await _next(context);
        }
    }
}
