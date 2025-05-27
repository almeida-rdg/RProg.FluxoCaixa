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
        private Timer? _timerLimpeza;

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

        /// <summary>
        /// Executa o ciclo principal do worker, incluindo reconexão automática e escuta da fila.
        /// </summary>
        /// <param name="stoppingToken">Token de cancelamento.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando Worker de Consolidação de Fluxo de Caixa");

            try
            {
                IniciarTimerLimpezaPeriodica();

                await IniciarEscutaComReconexaoAsync(stoppingToken);
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
                _timerLimpeza?.Dispose();
                await _rabbitMqService.PararEscutaAsync();
                _logger.LogInformation("Worker finalizado");
            }
        }

        /// <summary>
        /// Inicia a escuta da fila e gerencia reconexões automáticas.
        /// </summary>
        /// <param name="stoppingToken">Token de cancelamento.</param>
        private async Task IniciarEscutaComReconexaoAsync(CancellationToken stoppingToken)
        {
            await _rabbitMqService.IniciarEscutaAsync(ProcessarMensagemAsync, stoppingToken);

            _logger.LogInformation("Worker iniciado com sucesso. Aguardando mensagens...");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_rabbitMqService.EstaConectado)
                {
                    _logger.LogWarning("Conexão RabbitMQ perdida. Tentando reconectar...");
                    await TentarReconectarAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        /// <summary>
        /// Tenta reconectar ao RabbitMQ e reiniciar a escuta.
        /// </summary>
        /// <param name="stoppingToken">Token de cancelamento.</param>
        private async Task TentarReconectarAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _rabbitMqService.ReconectarAsync();
                await _rabbitMqService.IniciarEscutaAsync(ProcessarMensagemAsync, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao tentar reconectar ao RabbitMQ");
            }
        }

        /// <summary>
        /// Processa uma mensagem recebida da fila.
        /// </summary>
        /// <param name="lancamento">Lançamento recebido.</param>
        /// <param name="nomeFila">Nome da fila.</param>
        /// <returns>True se processado com sucesso, false caso contrário.</returns>
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

        /// <summary>
        /// Inicia timer para limpeza periódica de lançamentos processados antigos.
        /// </summary>
        private void IniciarTimerLimpezaPeriodica()
        {
            var intervalHoras = _configuration.GetValue<int>("Worker:IntervalLimpezaHoras", 24);
            var diasParaManter = _configuration.GetValue<int>("Worker:DiasManterLancamentos", 30);

            var intervalo = TimeSpan.FromHours(intervalHoras);

            _timerLimpeza = new Timer(
                async _ => await ExecutarLimpezaPeriodicaAsync(diasParaManter),
                null,
                intervalo,
                intervalo
            );

            _logger.LogInformation("Timer de limpeza periódica configurado: Intervalo={IntervalHoras}h, DiasParaManter={DiasParaManter}",
                intervalHoras, diasParaManter);
        }

        /// <summary>
        /// Executa a limpeza periódica de lançamentos processados antigos.
        /// </summary>
        /// <param name="diasParaManter">Quantidade de dias para manter os lançamentos.</param>
        private async Task ExecutarLimpezaPeriodicaAsync(int diasParaManter)
        {
            try
            {
                _logger.LogInformation("Executando limpeza periódica de lançamentos processados antigos");

                var registrosRemovidos = await _consolidacaoService.LimparLancamentosProcessadosAntigosAsync(diasParaManter);

                _logger.LogInformation("Limpeza periódica concluída: {RegistrosRemovidos} registros removidos", registrosRemovidos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante limpeza periódica de lançamentos processados");
            }
        }

        /// <summary>
        /// Libera recursos utilizados pelo worker.
        /// </summary>
        public override void Dispose()
        {
            _timerLimpeza?.Dispose();
            base.Dispose();
        }
    }
}
