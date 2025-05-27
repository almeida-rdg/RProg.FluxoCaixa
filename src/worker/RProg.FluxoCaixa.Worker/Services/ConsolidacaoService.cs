using RProg.FluxoCaixa.Worker.Domain.DTOs;
using RProg.FluxoCaixa.Worker.Domain.Entities;
using RProg.FluxoCaixa.Worker.Domain.Services;
using RProg.FluxoCaixa.Worker.Infrastructure.Data;

namespace RProg.FluxoCaixa.Worker.Services
{
    /// <summary>
    /// Serviço responsável pela consolidação diária de lançamentos.
    /// Implementa lógica idempotente para evitar processamento duplicado.
    /// </summary>
    public class ConsolidacaoService : IConsolidacaoService
    {
        private readonly IConsolidadoRepository _consolidadoRepository;
        private readonly ILancamentoProcessadoRepository _lancamentoProcessadoRepository;
        private readonly ILogger<ConsolidacaoService> _logger;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ConsolidacaoService"/>.
        /// </summary>
        /// <param name="consolidadoRepository">O repositório de consolidados.</param>
        /// <param name="lancamentoProcessadoRepository">O repositório de lançamentos processados.</param>
        /// <param name="logger">O logger.</param>
        public ConsolidacaoService(
            IConsolidadoRepository consolidadoRepository,
            ILancamentoProcessadoRepository lancamentoProcessadoRepository,
            ILogger<ConsolidacaoService> logger)
        {
            _consolidadoRepository = consolidadoRepository;
            _lancamentoProcessadoRepository = lancamentoProcessadoRepository;
            _logger = logger;
        }

        /// <summary>
        /// Processa um lançamento financeiro, atualizando as consolidações diárias.
        /// </summary>
        /// <param name="lancamento">O DTO do lançamento a ser processado.</param>
        /// <param name="cancellationToken">Um token para observar enquanto espera a tarefa ser completada.</param>
        /// <returns>Um <see cref="Task"/> que representa a operação assíncrona. O resultado da tarefa contém <c>true</c> se o lançamento foi processado com sucesso; <c>false</c> se já havia sido processado.</returns>
        public async Task<bool> ProcessarLancamentoAsync(LancamentoDto lancamento, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando processamento do lançamento ID: {LancamentoId}, Valor: {Valor}, Data: {Data}",
                lancamento.Id, lancamento.Valor, lancamento.Data);
            // Verificar se o lançamento já foi processado (idempotência)
            var jaProcessado = await _lancamentoProcessadoRepository
                .JaFoiProcessadoAsync(lancamento.Id, cancellationToken);

            if (jaProcessado)
            {
                _logger.LogInformation("Lançamento ID: {LancamentoId} já foi processado anteriormente. Ignorando.",
                    lancamento.Id);
                return false;
            }

            try
            {
                var dataConsolidacao = lancamento.Data.Date;

                // Processar consolidação geral
                await ProcessarConsolidacaoGeralAsync(lancamento, dataConsolidacao, cancellationToken);

                // Processar consolidação por categoria
                if (!string.IsNullOrWhiteSpace(lancamento.Categoria))
                {
                    await ProcessarConsolidacaoPorCategoriaAsync(lancamento, dataConsolidacao, cancellationToken);
                }

                // Marcar como processado
                await _lancamentoProcessadoRepository
                    .MarcarComoProcessadoAsync(lancamento.Id, DateTime.UtcNow, cancellationToken);

                _logger.LogInformation("Lançamento ID: {LancamentoId} processado com sucesso", lancamento.Id);
                return true;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar lançamento ID: {LancamentoId}", lancamento.Id);
                throw;
            }
        }

        /// <summary>
        /// Atualiza forçadamente as consolidações para uma data específica.
        /// </summary>
        /// <param name="data">A data para a qual as consolidações serão atualizadas.</param>
        /// <param name="cancellationToken">Um token para observar enquanto espera a tarefa ser completada.</param>
        /// <returns>Um <see cref="Task"/> que representa a operação assíncrona.</returns>
        public async Task AtualizarConsolidacaoDataAsync(DateTime data, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando atualização forçada das consolidações para data: {Data}", data.Date);

            try
            {
                await _consolidadoRepository.RecalcularConsolidacoesDataAsync(data.Date, cancellationToken);
                _logger.LogInformation("Consolidações da data {Data} atualizadas com sucesso", data.Date);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar consolidações da data: {Data}", data.Date);
                throw;
            }
        }

        /// <summary>
        /// Executa limpeza de lançamentos processados antigos.
        /// </summary>
        /// <param name="diasParaManter">Número de dias para manter os registros (padrão: 30)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Número de registros removidos</returns>
        public async Task<int> LimparLancamentosProcessadosAntigosAsync(int diasParaManter = 30, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Iniciando limpeza de lançamentos processados antigos. Dias para manter: {DiasParaManter}", diasParaManter);

            try
            {
                var registrosRemovidos = await _consolidadoRepository.LimparLancamentosProcessadosAntigosAsync(diasParaManter, cancellationToken);
                _logger.LogInformation("Limpeza de lançamentos processados concluída: {RegistrosRemovidos} registros removidos", registrosRemovidos);
                return registrosRemovidos;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar lançamentos processados antigos");
                throw;
            }
        }

        private async Task ProcessarConsolidacaoGeralAsync(LancamentoDto lancamento, DateTime dataConsolidacao, CancellationToken cancellationToken)
        {
            var consolidadoGeral = await _consolidadoRepository
                .ObterOuCriarConsolidadoAsync(dataConsolidacao, null, cancellationToken);

            AtualizarConsolidado(consolidadoGeral, lancamento);

            await _consolidadoRepository.SalvarConsolidadoAsync(consolidadoGeral, cancellationToken);

            _logger.LogDebug("Consolidação geral atualizada para data: {Data}", dataConsolidacao);
        }

        private async Task ProcessarConsolidacaoPorCategoriaAsync(LancamentoDto lancamento, DateTime dataConsolidacao, CancellationToken cancellationToken)
        {
            var consolidadoCategoria = await _consolidadoRepository
                .ObterOuCriarConsolidadoAsync(dataConsolidacao, lancamento.Categoria, cancellationToken);

            AtualizarConsolidado(consolidadoCategoria, lancamento);

            await _consolidadoRepository.SalvarConsolidadoAsync(consolidadoCategoria, cancellationToken);

            _logger.LogDebug("Consolidação por categoria '{Categoria}' atualizada para data: {Data}",
                lancamento.Categoria, dataConsolidacao);
        }

        private static void AtualizarConsolidado(ConsolidadoDiario consolidado, LancamentoDto lancamento)
        {
            if (lancamento.IsCredito)
            {
                consolidado.TotalCreditos += Math.Abs(lancamento.Valor);
            }

            else if (lancamento.IsDebito)
            {
                // Débitos são armazenados como valores negativos (<=0)
                consolidado.TotalDebitos -= Math.Abs(lancamento.Valor);
            }

            consolidado.QuantidadeLancamentos++;
            consolidado.AtualizarDataModificacao();

            // Validar se os valores estão corretos após a atualização
            if (!consolidado.ValidarValores())
            {
                throw new InvalidOperationException(
                    $"Valores inválidos após consolidação: Créditos={consolidado.TotalCreditos}, Débitos={consolidado.TotalDebitos}");
            }
        }
    }
}
