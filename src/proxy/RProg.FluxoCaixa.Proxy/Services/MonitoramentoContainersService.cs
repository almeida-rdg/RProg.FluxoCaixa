using Docker.DotNet;
using Docker.DotNet.Models;
using System.Collections.Concurrent;

namespace RProg.FluxoCaixa.Proxy.Services;

/// <summary>
/// Serviço para monitoramento de saúde dos containers e escalabilidade automática
/// </summary>
public class MonitoramentoContainersService : BackgroundService
{
    private readonly ILogger<MonitoramentoContainersService> _logger;
    private readonly ConfiguracaoYarpProvider _configuracaoProvider;
    private readonly DockerClient _dockerClient;
    private readonly ConcurrentDictionary<string, StatusContainer> _statusContainers;
    private readonly Timer _timerVerificacaoSaude;
    private readonly Timer _timerEscalabilidade;

    // Configurações de monitoramento
    private readonly TimeSpan _intervaloVerificacao = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _intervaloEscalabilidade = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _tempoLimiteContainer = TimeSpan.FromMinutes(1);
    
    // Thresholds de CPU e memória para escalabilidade
    private readonly double _thresholdCpu = 70.0; // 70%
    private readonly double _thresholdMemoria = 70.0; // 70%

    public MonitoramentoContainersService(
        ILogger<MonitoramentoContainersService> logger,
        ConfiguracaoYarpProvider configuracaoProvider)
    {
        _logger = logger;
        _configuracaoProvider = configuracaoProvider;
        _dockerClient = new DockerClientConfiguration().CreateClient();
        _statusContainers = new ConcurrentDictionary<string, StatusContainer>();

        _timerVerificacaoSaude = new Timer(VerificarSaudeContainers, null, 
            TimeSpan.FromSeconds(10), _intervaloVerificacao);
        
        _timerEscalabilidade = new Timer(VerificarEscalabilidade, null, 
            TimeSpan.FromMinutes(1), _intervaloEscalabilidade);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de monitoramento de containers iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DescobrirContainers();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Serviço sendo parado
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no loop principal do monitoramento");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task DescobrirContainers()
    {
        try
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = false // Só containers rodando
            });

            var containersFluxoCaixa = containers.Where(c => 
                c.Names.Any(n => n.Contains("fluxo-lancamentos") || n.Contains("fluxo-consolidado")))
                .ToList();

            foreach (var container in containersFluxoCaixa)
            {
                var containerInfo = new StatusContainer
                {
                    Id = container.ID,
                    Nome = container.Names.FirstOrDefault()?.TrimStart('/') ?? container.ID[..12],
                    Tipo = DeterminarTipoContainer(container),
                    Status = container.State,
                    UltimaVerificacao = DateTime.UtcNow,
                    Saudavel = container.State == "running"
                };

                _statusContainers.AddOrUpdate(container.ID, containerInfo, (_, _) => containerInfo);
            }

            _logger.LogDebug("Descobertos {Quantidade} containers do FluxoCaixa", containersFluxoCaixa.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao descobrir containers");
        }
    }

    private void VerificarSaudeContainers(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var containersParaVerificar = _statusContainers.Values.ToList();
                var tarefasVerificacao = containersParaVerificar.Select(VerificarSaudeContainer);
                
                await Task.WhenAll(tarefasVerificacao);
                
                // Atualiza configuração YARP se houve mudanças
                await AtualizarConfiguracaoSeNecessario();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na verificação de saúde dos containers");
            }
        });
    }

    private async Task VerificarSaudeContainer(StatusContainer status)
    {
        try
        {
            // Verifica se o container ainda existe
            var containerInfo = await _dockerClient.Containers.InspectContainerAsync(status.Id);
            
            var estadoAnterior = status.Saudavel;
            status.Status = containerInfo.State.Status;
            status.Saudavel = containerInfo.State.Running && 
                             !containerInfo.State.Restarting && 
                             !containerInfo.State.Dead;
            
            // Verifica endpoint de health se disponível
            if (status.Saudavel)
            {
                status.Saudavel = await VerificarEndpointSaude(status);
            }

            status.UltimaVerificacao = DateTime.UtcNow;

            // Se mudou o estado de saúde, loga
            if (estadoAnterior != status.Saudavel)
            {
                _logger.LogWarning("Container {Nome} mudou estado de saúde: {EstadoAnterior} -> {EstadoAtual}", 
                    status.Nome, estadoAnterior, status.Saudavel);
            }

            // Se não está saudável há mais de 1 minuto, marca para recriação
            if (!status.Saudavel && DateTime.UtcNow - status.UltimaVezSaudavel > _tempoLimiteContainer)
            {
                _logger.LogWarning("Container {Nome} não saudável há mais de {Tempo} minutos. Marcando para recriação.", 
                    status.Nome, _tempoLimiteContainer.TotalMinutes);
                
                status.DeveSerRecriado = true;
                _ = Task.Run(() => RecriarContainer(status));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar saúde do container {ContainerId}", status.Id);
            status.Saudavel = false;
        }
    }

    private async Task<bool> VerificarEndpointSaude(StatusContainer status)
    {
        try
        {
            var porta = await ObterPortaContainer(status.Id);
            if (porta == null) return false;

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetAsync($"http://localhost:{porta}/health");
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<int?> ObterPortaContainer(string containerId)
    {
        try
        {
            var containerInfo = await _dockerClient.Containers.InspectContainerAsync(containerId);
            var portas = containerInfo.NetworkSettings.Ports;
            
            // Procura pela porta 8080 mapeada
            if (portas.TryGetValue("8080/tcp", out var portBindings) && portBindings?.Any() == true)
            {
                return int.Parse(portBindings.First().HostPort);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Erro ao obter porta do container {ContainerId}", containerId);
        }
        
        return null;
    }

    private void VerificarEscalabilidade(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var containersPorTipo = _statusContainers.Values
                    .Where(c => c.Saudavel)
                    .GroupBy(c => c.Tipo)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var grupo in containersPorTipo)
                {
                    await VerificarEscalabilidadeTipo(grupo.Key, grupo.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na verificação de escalabilidade");
            }
        });
    }

    private async Task VerificarEscalabilidadeTipo(TipoContainer tipo, List<StatusContainer> containers)
    {
        var metricas = await ObterMetricasContainers(containers);
          var cpuMedia = metricas.Average(m => m.CpuUsagePercent);
        var memoriaMedia = metricas.Average(m => m.MemoryUsagePercent);

        _logger.LogDebug("Métricas {Tipo}: CPU {Cpu:F1}%, Memória {Memoria:F1}%", 
            tipo, cpuMedia, memoriaMedia);

        // Verifica se precisa escalar para cima
        if (cpuMedia > _thresholdCpu || memoriaMedia > _thresholdMemoria)
        {
            _logger.LogInformation("Threshold ultrapassado para {Tipo}. Escalando para cima...", tipo);
            await EscalarParaCima(tipo);
        }
        // Verifica se pode escalar para baixo (só se tiver mais de 1 container)
        else if (containers.Count > 1 && cpuMedia < _thresholdCpu * 0.3 && memoriaMedia < _thresholdMemoria * 0.3)
        {
            _logger.LogInformation("Baixo uso de recursos para {Tipo}. Escalando para baixo...", tipo);
            await EscalarParaBaixo(tipo, containers);
        }
    }    private async Task<List<MetricasContainer>> ObterMetricasContainers(List<StatusContainer> containers)
    {
        var metricas = new List<MetricasContainer>();

        foreach (var container in containers)
        {
            try
            {
                // Use the newer API that returns individual stats responses
                var progress = new Progress<ContainerStatsResponse>(statsResponse =>
                {
                    try
                    {
                        if (statsResponse != null)
                        {
                            var metrica = new MetricasContainer
                            {
                                ContainerId = container.Id,
                                CpuUsagePercent = CalcularCpuPercent(statsResponse),
                                MemoryUsagePercent = CalcularMemoryPercent(statsResponse),
                                MemoryUsageBytes = (long)(statsResponse.MemoryStats?.Usage ?? 0),
                                MemoryLimitBytes = (long)(statsResponse.MemoryStats?.Limit ?? 0),
                                Timestamp = DateTime.UtcNow
                            };

                            metricas.Add(metrica);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Erro ao processar métricas do container {ContainerId}", container.Id);
                    }
                });

                await _dockerClient.Containers.GetContainerStatsAsync(
                    container.Id,
                    new ContainerStatsParameters { Stream = false },
                    progress,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Erro ao obter métricas do container {ContainerId}", container.Id);
            }
        }

        return metricas;
    }

    private double CalcularCpuPercent(ContainerStatsResponse stats)
    {
        if (stats.CPUStats?.CPUUsage?.TotalUsage == null || 
            stats.PreCPUStats?.CPUUsage?.TotalUsage == null)
            return 0;

        var cpuDelta = stats.CPUStats.CPUUsage.TotalUsage - stats.PreCPUStats.CPUUsage.TotalUsage;
        var systemDelta = stats.CPUStats.SystemUsage - stats.PreCPUStats.SystemUsage;        if (systemDelta > 0 && cpuDelta > 0)
        {
            var numCpus = stats.CPUStats.OnlineCPUs > 0 ? (int)stats.CPUStats.OnlineCPUs : Environment.ProcessorCount;
            return (double)cpuDelta / systemDelta * numCpus * 100.0;
        }

        return 0;
    }

    private double CalcularMemoryPercent(ContainerStatsResponse stats)
    {
        if (stats.MemoryStats?.Usage == null || stats.MemoryStats?.Limit == null)
            return 0;

        return (double)stats.MemoryStats.Usage / stats.MemoryStats.Limit * 100.0;
    }

    private async Task EscalarParaCima(TipoContainer tipo)
    {
        try
        {
            var nomeImagem = tipo == TipoContainer.Lancamentos ? "fluxo-lancamentos" : "fluxo-consolidado";
            var novoContainer = await CriarNovoContainer(tipo, nomeImagem);
            
            _logger.LogInformation("Novo container {Tipo} criado: {ContainerId}", tipo, novoContainer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao escalar para cima {Tipo}", tipo);
        }
    }

    private async Task EscalarParaBaixo(TipoContainer tipo, List<StatusContainer> containers)
    {
        try
        {
            // Remove o container com menor uso (simplificado - remove o último)
            var containerParaRemover = containers.Last();
            
            await _dockerClient.Containers.StopContainerAsync(containerParaRemover.Id, 
                new ContainerStopParameters { WaitBeforeKillSeconds = 30 });
            
            await _dockerClient.Containers.RemoveContainerAsync(containerParaRemover.Id,
                new ContainerRemoveParameters { Force = true });

            _statusContainers.TryRemove(containerParaRemover.Id, out _);
            
            _logger.LogInformation("Container {Tipo} removido: {ContainerId}", tipo, containerParaRemover.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao escalar para baixo {Tipo}", tipo);
        }
    }

    private async Task<string> CriarNovoContainer(TipoContainer tipo, string nomeImagem)
    {
        var parametros = new CreateContainerParameters
        {
            Image = nomeImagem,
            Name = $"{nomeImagem}-{Guid.NewGuid():N}",
            ExposedPorts = new Dictionary<string, EmptyStruct> { { "8080/tcp", new EmptyStruct() } },
            HostConfig = new HostConfig
            {
                PublishAllPorts = true,
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped },
                NetworkMode = tipo == TipoContainer.Lancamentos ? "lancamentos-net" : "consolidado-net"
            },
            Env = ObterVariaveisAmbiente(tipo)
        };

        var response = await _dockerClient.Containers.CreateContainerAsync(parametros);
        await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

        // Aguarda container ficar saudável antes de adicionar ao balanceador
        await AguardarContainerSaudavel(response.ID);

        return response.ID;
    }

    private List<string> ObterVariaveisAmbiente(TipoContainer tipo)
    {
        return tipo switch
        {
            TipoContainer.Lancamentos => new List<string>
            {
                "ASPNETCORE_ENVIRONMENT=Production",
                "ConnectionStrings__DefaultConnection=Server=sqlserver;Database=FluxoCaixaLancamentos;User=sa;Password=Your_password123;TrustServerCertificate=True",
                "RabbitMQ__HostName=rabbitmq",
                "RabbitMQ__QueueName=lancamentos"
            },
            TipoContainer.Consolidado => new List<string>
            {
                "ASPNETCORE_ENVIRONMENT=Production",
                "ConnectionStrings__DefaultConnection=Server=sqlserver;Database=FluxoCaixaConsolidado;User=sa;Password=Your_password123;TrustServerCertificate=True"
            },
            _ => new List<string>()
        };
    }

    private async Task AguardarContainerSaudavel(string containerId)
    {
        var tentativas = 0;
        const int maxTentativas = 12; // 2 minutos (10s * 12)

        while (tentativas < maxTentativas)
        {
            try
            {
                var containerInfo = await _dockerClient.Containers.InspectContainerAsync(containerId);
                if (containerInfo.State.Running)
                {
                    // Tenta verificar o health endpoint
                    var porta = await ObterPortaContainer(containerId);
                    if (porta.HasValue)
                    {
                        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                        var response = await httpClient.GetAsync($"http://localhost:{porta}/health");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Container {ContainerId} está saudável e pronto", containerId);
                            return;
                        }
                    }
                }
            }
            catch
            {
                // Ignora erros e continua tentando
            }

            tentativas++;
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        _logger.LogWarning("Container {ContainerId} não ficou saudável no tempo esperado", containerId);
    }

    private async Task RecriarContainer(StatusContainer status)
    {
        try
        {
            _logger.LogInformation("Recriando container não saudável: {Nome}", status.Nome);

            // Cria novo container primeiro
            var novoContainerId = await CriarNovoContainer(status.Tipo, 
                status.Tipo == TipoContainer.Lancamentos ? "fluxo-lancamentos" : "fluxo-consolidado");

            // Aguarda novo container ficar saudável
            await AguardarContainerSaudavel(novoContainerId);

            // Remove container antigo
            try
            {
                await _dockerClient.Containers.StopContainerAsync(status.Id, 
                    new ContainerStopParameters { WaitBeforeKillSeconds = 30 });
                await _dockerClient.Containers.RemoveContainerAsync(status.Id,
                    new ContainerRemoveParameters { Force = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao remover container antigo {ContainerId}", status.Id);
            }

            // Remove do tracking
            _statusContainers.TryRemove(status.Id, out _);

            _logger.LogInformation("Container {Nome} recriado com sucesso. Novo ID: {NovoId}", 
                status.Nome, novoContainerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recriar container {Nome}", status.Nome);
        }
    }

    private async Task AtualizarConfiguracaoSeNecessario()
    {
        var containersLancamentos = _statusContainers.Values
            .Where(c => c.Tipo == TipoContainer.Lancamentos && c.Saudavel)
            .ToList();

        var containersConsolidado = _statusContainers.Values
            .Where(c => c.Tipo == TipoContainer.Consolidado && c.Saudavel)
            .ToList();

        var destinosLancamentos = new List<string>();
        var destinosConsolidado = new List<string>();

        foreach (var container in containersLancamentos)
        {
            var porta = await ObterPortaContainer(container.Id);
            if (porta.HasValue)
            {
                destinosLancamentos.Add($"http://localhost:{porta}");
            }
        }

        foreach (var container in containersConsolidado)
        {
            var porta = await ObterPortaContainer(container.Id);
            if (porta.HasValue)
            {
                destinosConsolidado.Add($"http://localhost:{porta}");
            }
        }

        // Fallback para containers padrão se não houver containers saudáveis
        if (!destinosLancamentos.Any())
        {
            destinosLancamentos.Add("http://lancamentos-api:8080");
        }

        if (!destinosConsolidado.Any())
        {
            destinosConsolidado.Add("http://consolidado-api:8080");
        }

        _configuracaoProvider.AtualizarConfiguracao(destinosLancamentos, destinosConsolidado);
    }

    private TipoContainer DeterminarTipoContainer(ContainerListResponse container)
    {
        var nome = container.Names.FirstOrDefault()?.ToLower() ?? "";
        
        if (nome.Contains("lancamentos"))
            return TipoContainer.Lancamentos;
        if (nome.Contains("consolidado"))
            return TipoContainer.Consolidado;
        
        return TipoContainer.Desconhecido;
    }

    public override void Dispose()
    {
        _timerVerificacaoSaude?.Dispose();
        _timerEscalabilidade?.Dispose();
        _dockerClient?.Dispose();
        base.Dispose();
    }
}
