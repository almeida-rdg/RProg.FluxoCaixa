using MediatR;
using RProg.FluxoCaixa.Consolidado.Application.DTOs;
using RProg.FluxoCaixa.Consolidado.Infrastructure.Data;

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
        }        /// <summary>
        /// Processa a query para obter dados consolidados por período e tipo de consolidação.
        /// </summary>
        /// <param name="request">Query com período e tipo desejados</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Dados consolidados do período e tipo com informações de última atualização</returns>
        public async Task<ConsolidadoPeriodoResponseDto> Handle(ObterConsolidadosPorPeriodoQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando consulta de consolidados por período e tipo: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Tipo: {TipoConsolidacao}",
                request.DataInicial, request.DataFinal, request.TipoConsolidacao);

            try
            {
                // Busca dados consolidados do período e tipo
                var consolidados = await _repository.ObterPorPeriodoETipoAsync(
                    request.DataInicial, request.DataFinal, request.TipoConsolidacao, cancellationToken);

                // Busca a última data de atualização do período e tipo
                var ultimaConsolidacao = await _repository.ObterUltimaDataAtualizacaoPorTipoAsync(
                    request.DataInicial, request.DataFinal, request.TipoConsolidacao, cancellationToken);

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

                _logger.LogInformation("Consulta concluída. {TotalRegistros} registros encontrados para tipo {TipoConsolidacao}. Última consolidação: {UltimaConsolidacao}",
                    response.TotalRegistros, request.TipoConsolidacao, response.UltimaConsolidacao?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar consolidados por período e tipo: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Tipo: {TipoConsolidacao}",
                    request.DataInicial, request.DataFinal, request.TipoConsolidacao);

                throw;
            }
        }
    }
}
