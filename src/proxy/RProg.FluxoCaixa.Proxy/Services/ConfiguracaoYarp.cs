using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace RProg.FluxoCaixa.Proxy.Services;

/// <summary>
/// Implementação da configuração do YARP
/// </summary>
public class ConfiguracaoYarp : IProxyConfig
{
    private readonly CancellationChangeToken _changeToken;

    public ConfiguracaoYarp(IReadOnlyList<RouteConfig> rotas, IReadOnlyList<ClusterConfig> clusters, CancellationToken cancellationToken)
    {
        Routes = rotas;
        Clusters = clusters;
        _changeToken = new CancellationChangeToken(cancellationToken);
    }

    public IReadOnlyList<RouteConfig> Routes { get; }
    
    public IReadOnlyList<ClusterConfig> Clusters { get; }
    
    public IChangeToken ChangeToken => _changeToken;

    internal void SignalChange()
    {
        // O token de cancelamento já foi configurado no construtor
        // Esta implementação permite que o YARP saiba quando a configuração mudou
    }
}
