using MediatR;
using RProg.FluxoCaixa.Consolidado.Application.DTOs;
using RProg.FluxoCaixa.Worker.Infrastructure.Data;

namespace RProg.FluxoCaixa.Consolidado.Application.Queries
{
    /// <summary>
    /// Handler para processar consultas de dados consolidados por período.
    /// </summary>
    public class ObterConsolidadosPorPeriodoQueryHandler : IRequestHandler<ObterConsolidadosPorPeriodoQuery, ConsolidadoPeriodoResponseDto>
    {
        private readonly IConsolidadoDiarioRepository _repository;
        private readonly ILogger<ObterConsolidadosPorPeriodoQueryHandler> _logger;

        public ObterConsolidadosPorPeriodoQueryHandler(
            IConsolidadoDiarioRepository repository,
            ILogger<ObterConsolidadosPorPeriodoQueryHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Processa a query para obter dados consolidados por período.
        /// </summary>
        /// <param name="request">Query com período desejado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Dados consolidados do período com informações de última atualização</returns>
        public async Task<ConsolidadoPeriodoResponseDto> Handle(ObterConsolidadosPorPeriodoQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando consulta de consolidados por período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}",
                request.DataInicial, request.DataFinal);

            try
            {
                // Busca dados consolidados do período
                var consolidados = await _repository.ObterPorPeriodoAsync(
                    request.DataInicial, request.DataFinal, cancellationToken);

                // Busca a última data de atualização do período
                var ultimaConsolidacao = await _repository.ObterUltimaDataAtualizacaoAsync(
                    request.DataInicial, request.DataFinal, cancellationToken);

                // Mapeia para DTO de resposta
                var consolidadosDto = consolidados.Select(c => new ConsolidadoResponseDto
                {
                    Data = c.Data,
                    Categoria = c.Categoria,
                    TipoConsolidacao = c.TipoConsolidacao,
                    TotalCreditos = c.TotalCreditos,
                    TotalDebitos = c.TotalDebitos,
                    SaldoLiquido = c.SaldoLiquido,
                    QuantidadeLancamentos = c.QuantidadeLancamentos,
                    DataAtualizacao = c.DataAtualizacao
                }).ToList();

                var response = new ConsolidadoPeriodoResponseDto
                {
                    DataInicial = request.DataInicial,
                    DataFinal = request.DataFinal,
                    Consolidados = consolidadosDto,
                    UltimaConsolidacao = ultimaConsolidacao,
                    TotalRegistros = consolidadosDto.Count
                };

                _logger.LogInformation("Consulta concluída. {TotalRegistros} registros encontrados. Última consolidação: {UltimaConsolidacao}",
                    response.TotalRegistros, response.UltimaConsolidacao?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar consolidados por período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}",
                    request.DataInicial, request.DataFinal);

                throw;
            }
        }
    }
}
