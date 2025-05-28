using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Health;

namespace RProg.FluxoCaixa.Proxy.Services;

/// <summary>
/// Provedor de configuração dinâmica para o YARP
/// </summary>
public class ConfiguracaoYarpProvider : IProxyConfigProvider
{
    private volatile ConfiguracaoYarp _configuracao;
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger<ConfiguracaoYarpProvider> _logger;
    private readonly IConfiguration _configuration;

    public ConfiguracaoYarpProvider(ILogger<ConfiguracaoYarpProvider> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _configuracao = CriarConfiguracaoInicial();
    }

    public IProxyConfig GetConfig() => _configuracao;

    /// <summary>
    /// Atualiza a configuração dinamicamente
    /// </summary>
    public void AtualizarConfiguracao(IEnumerable<string> destinosLancamentos, IEnumerable<string> destinosConsolidado)
    {
        _logger.LogInformation("Atualizando configuração YARP com novos destinos");
        
        var novaConfiguracao = CriarConfiguracao(destinosLancamentos, destinosConsolidado);
        var configAnterior = Interlocked.Exchange(ref _configuracao, novaConfiguracao);
        
        _logger.LogInformation("Configuração YARP atualizada com sucesso");
    }

    private ConfiguracaoYarp CriarConfiguracaoInicial()
    {
        var destinosLancamentos = ObterDestinosConfigurados("Lancamentos");
        var destinosConsolidado = ObterDestinosConfigurados("Consolidado");
        
        return CriarConfiguracao(destinosLancamentos, destinosConsolidado);
    }

    private IEnumerable<string> ObterDestinosConfigurados(string servico)
    {
        var section = _configuration.GetSection($"Yarp:Clusters:{servico}:Destinations");
        var destinos = new List<string>();
        
        foreach (var item in section.GetChildren())
        {
            var address = item.GetValue<string>("Address");
            if (!string.IsNullOrEmpty(address))
            {
                destinos.Add(address);
            }
        }
        
        // Fallback para configurações padrão se não encontrar na configuração
        if (!destinos.Any())
        {
            destinos = servico.ToLower() switch
            {
                "lancamentos" => new List<string> { "http://lancamentos-api:8080/" },
                "consolidado" => new List<string> { "http://consolidado-api:8080/" },
                _ => new List<string>()
            };
        }
        
        return destinos;
    }

    private ConfiguracaoYarp CriarConfiguracao(IEnumerable<string> destinosLancamentos, IEnumerable<string> destinosConsolidado)
    {
        var rotas = new List<RouteConfig>
        {
            new()
            {
                RouteId = "lancamentos-route",
                ClusterId = "lancamentos-cluster",
                Match = new RouteMatch
                {
                    Path = "/api/lancamentos/{**catch-all}"
                },
                Transforms = new[]
                {
                    new Dictionary<string, string>
                    {
                        ["PathPattern"] = "/api/lancamentos/{**catch-all}"
                    }
                }
            },
            new()
            {
                RouteId = "consolidado-route",
                ClusterId = "consolidado-cluster",
                Match = new RouteMatch
                {
                    Path = "/api/consolidado/{**catch-all}"
                },
                Transforms = new[]
                {
                    new Dictionary<string, string>
                    {
                        ["PathPattern"] = "/api/consolidado/{**catch-all}"
                    }
                }
            }
        };

        var clusters = new List<ClusterConfig>
        {
            CriarCluster("lancamentos-cluster", destinosLancamentos),
            CriarCluster("consolidado-cluster", destinosConsolidado)
        };

        return new ConfiguracaoYarp(rotas.AsReadOnly(), clusters.AsReadOnly(), _cts.Token);
    }    private ClusterConfig CriarCluster(string clusterId, IEnumerable<string> destinos)
    {
        var destinations = destinos.Select((destino, index) => 
            new KeyValuePair<string, DestinationConfig>(
                $"{clusterId}-destination-{index + 1}",
                new DestinationConfig
                {
                    Address = destino
                }
            )
        ).ToDictionary(x => x.Key, x => x.Value);

        return new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = destinations,
            LoadBalancingPolicy = "RoundRobin",            HttpClient = new HttpClientConfig
            {
                DangerousAcceptAnyServerCertificate = true // Apenas para desenvolvimento
            }
        };
    }
}
