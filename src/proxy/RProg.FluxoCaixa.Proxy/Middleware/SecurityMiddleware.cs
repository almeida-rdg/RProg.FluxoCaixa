using System.Text.RegularExpressions;

namespace RProg.FluxoCaixa.Proxy.Middleware;

/// <summary>
/// Middleware para proteção contra ataques de segurança
/// </summary>
public class SecurityMiddleware
{
    private readonly RequestDelegate _proximo;
    private readonly ILogger<SecurityMiddleware> _logger;

    // Padrões de ataques conhecidos
    private readonly List<Regex> _padroesAtaques = new()
    {
        new Regex(@"(\<script\>|\<\/script\>)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(union\s+select|drop\s+table|delete\s+from)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(exec\s*\(|eval\s*\(|javascript:|vbscript:)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(\.\./|\.\.\\)", RegexOptions.Compiled), // Path traversal
        new Regex(@"(\%3C|\%3E|\%27|\%22)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(?i)(\b(?:OR|AND)\b[^=]{1,50}?=\s*[^;\s]{1,50})|(--|#)|(/\*.*?\*/)|;\s*(?=\b(?:SELECT|INSERT|DELETE|UPDATE|DROP|UNION|EXEC|XP_)\b)|\b(?:SELECT|INSERT|DELETE|UPDATE|DROP|UNION|EXEC|XP_)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    // User agents suspeitos
    private readonly HashSet<string> _userAgentsSuspeitos = new()
    {
        "sqlmap", "nikto", "nmap", "masscan", "nessus", "openvas",
        "w3af", "skipfish", "gobuster", "dirb", "dirbuster"
    };

    public SecurityMiddleware(RequestDelegate proximo, ILogger<SecurityMiddleware> logger)
    {
        _proximo = proximo;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            // Adiciona headers de segurança
            AdicionarHeadersSeguranca(contexto);

            // Valida requisição
            var resultadoValidacao = await ValidarRequisicao(contexto);
            if (!resultadoValidacao.EhValida)
            {
                _logger.LogWarning("Requisição suspeita bloqueada: {Motivo} - IP: {IP} - UserAgent: {UserAgent}",
                    resultadoValidacao.Motivo,
                    ObterEnderecoIp(contexto),
                    contexto.Request.Headers["User-Agent"].FirstOrDefault());

                contexto.Response.StatusCode = 403;
                await contexto.Response.WriteAsync("Acesso negado - Requisição suspeita detectada");
                return;
            }

            await _proximo(contexto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no middleware de segurança");
            throw;
        }
    }
    
    private void AdicionarHeadersSeguranca(HttpContext contexto)
    {
        var headers = contexto.Response.Headers;

        // Previne clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Previne MIME sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Ativa proteção XSS do navegador
        headers["X-XSS-Protection"] = "1; mode=block";

        // Força HTTPS
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Content Security Policy básico
        headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";

        // Remove headers que revelam informações do servidor
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");

        // Adiciona header customizado
        headers["X-Security-Policy"] = "FluxoCaixa-Proxy-v1.0";
    }

    private async Task<ResultadoValidacao> ValidarRequisicao(HttpContext contexto)
    {
        // 1. Verifica User-Agent suspeito
        var userAgent = contexto.Request.Headers["User-Agent"].FirstOrDefault()?.ToLower() ?? "";
        if (_userAgentsSuspeitos.Any(ua => userAgent.Contains(ua)))
        {
            return new ResultadoValidacao(false, "User-Agent suspeito detectado");
        }

        // 2. Verifica tamanho da requisição
        if (contexto.Request.ContentLength > 50 * 1024 * 1024) // 50MB
        {
            return new ResultadoValidacao(false, "Requisição muito grande");
        }

        // 3. Verifica padrões de ataque na URL
        var url = contexto.Request.Path.Value + contexto.Request.QueryString.Value;
        foreach (var padrao in _padroesAtaques)
        {
            if (padrao.IsMatch(url))
            {
                return new ResultadoValidacao(false, $"Padrão de ataque detectado na URL: {padrao}");
            }
        }

        // 4. Verifica headers suspeitos
        foreach (var header in contexto.Request.Headers)
        {
            var valor = string.Join(" ", header.Value.ToArray());
            foreach (var padrao in _padroesAtaques)
            {
                if (padrao.IsMatch(valor))
                {
                    return new ResultadoValidacao(false, $"Padrão de ataque detectado no header {header.Key}");
                }
            }
        }

        // 5. Verifica body se existir
        if (contexto.Request.ContentLength > 0 && contexto.Request.Body.CanSeek)
        {
            contexto.Request.Body.Position = 0;
            using var reader = new StreamReader(contexto.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            contexto.Request.Body.Position = 0;

            foreach (var padrao in _padroesAtaques)
            {
                if (padrao.IsMatch(body))
                {
                    return new ResultadoValidacao(false, "Padrão de ataque detectado no body");
                }
            }
        }

        return new ResultadoValidacao(true, "Requisição válida");
    }

    private string ObterEnderecoIp(HttpContext contexto)
    {
        return contexto.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? contexto.Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? contexto.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }
}

/// <summary>
/// Resultado da validação de segurança
/// </summary>
public record ResultadoValidacao(bool EhValida, string Motivo);
