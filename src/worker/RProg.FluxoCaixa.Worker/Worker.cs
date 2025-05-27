using RProg.FluxoCaixa.Worker.Domain.Services;
using RProg.FluxoCaixa.Worker.Infrastructure.Services;

namespace RProg.FluxoCaixa.Worker
{
    /// <summary>
    /// Serviço worker principal responsável pela consolidação de lançamentos.
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly IConsolidacaoService _consolidacaoService;
        private readonly IConfiguration _configuration;

        public Worker(
            ILogger<Worker> logger,
            IRabbitMqService rabbitMqService,
            IConsolidacaoService consolidacaoService,
            IConfiguration configuration)
        {
            _logger = logger;
            _rabbitMqService = rabbitMqService;
            _consolidacaoService = consolidacaoService;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando Worker de Consolidação de Fluxo de Caixa");

            try
            {
                var prefixoFila = _configuration.GetValue<string>("RabbitMQ:PrefixoFila") ?? "lancamento";
                
                await _rabbitMqService.IniciarEscutaAsync(
                    prefixoFila,
                    ProcessarMensagemAsync,
                    stoppingToken);

                _logger.LogInformation("Worker iniciado com sucesso. Aguardando mensagens...");

                // Manter o worker vivo
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Verificar se a conexão está ativa
                    if (!_rabbitMqService.EstaConectado)
                    {
                        _logger.LogWarning("Conexão RabbitMQ perdida. Tentando reconectar...");
                        try
                        {
                            await _rabbitMqService.ReconectarAsync();
                            await _rabbitMqService.IniciarEscutaAsync(
                                prefixoFila,
                                ProcessarMensagemAsync,
                                stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Falha na reconexão. Tentando novamente em 30 segundos...");
                            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker cancelado pelo token de cancelamento");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal no Worker");
                throw;
            }
            finally
            {
                await _rabbitMqService.PararEscutaAsync();
                _logger.LogInformation("Worker finalizado");
            }
        }

        private async Task<bool> ProcessarMensagemAsync(Domain.DTOs.LancamentoDto lancamento, string nomeFila)
        {
            try
            {
                _logger.LogDebug("Processando lançamento da fila {Fila}: ID={Id}, Valor={Valor}", 
                    nomeFila, lancamento.Id, lancamento.Valor);

                var resultado = await _consolidacaoService.ProcessarLancamentoAsync(lancamento, CancellationToken.None);
                
                if (resultado)
                {
                    _logger.LogInformation("Lançamento {Id} processado com sucesso", lancamento.Id);
                }
                else
                {
                    _logger.LogInformation("Lançamento {Id} já havia sido processado anteriormente", lancamento.Id);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar lançamento {Id} da fila {Fila}", 
                    lancamento.Id, nomeFila);
                return false;
            }
        }
    }
}
