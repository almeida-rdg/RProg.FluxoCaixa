using Microsoft.Extensions.Options;

namespace RProg.FluxoCaixa.Proxy.Configuration;

/// <summary>
/// Configurações para o middleware de rate limiting
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Limite de requisições por minuto por IP
    /// </summary>
    public int LimitePorMinuto { get; set; } = 60;

    /// <summary>
    /// Limite de requisições por segundo por IP
    /// </summary>
    public int LimitePorSegundo { get; set; } = 10;

    /// <summary>
    /// Janela de tempo para contagem de requisições
    /// </summary>
    public TimeSpan JanelaTempo { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Tempo de bloqueio quando limite é excedido
    /// </summary>
    public TimeSpan TempoBloqueio { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Intervalo de limpeza de dados antigos
    /// </summary>
    public TimeSpan IntervaloLimpeza { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Lista de IPs que são isentos do rate limiting
    /// </summary>
    public List<string> IpsIsentos { get; set; } = new();

    /// <summary>
    /// Indica se o rate limiting está habilitado
    /// </summary>
    public bool Habilitado { get; set; } = true;
}

/// <summary>
/// Validador para as configurações de rate limiting
/// </summary>
public class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitingOptions options)
    {
        var failures = new List<string>();

        if (options.LimitePorMinuto <= 0)
        {
            failures.Add("LimitePorMinuto deve ser maior que zero");
        }

        if (options.LimitePorSegundo <= 0)
        {
            failures.Add("LimitePorSegundo deve ser maior que zero");
        }

        if (options.JanelaTempo <= TimeSpan.Zero)
        {
            failures.Add("JanelaTempo deve ser maior que zero");
        }

        if (options.TempoBloqueio <= TimeSpan.Zero)
        {
            failures.Add("TempoBloqueio deve ser maior que zero");
        }

        if (options.IntervaloLimpeza <= TimeSpan.Zero)
        {
            failures.Add("IntervaloLimpeza deve ser maior que zero");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}
