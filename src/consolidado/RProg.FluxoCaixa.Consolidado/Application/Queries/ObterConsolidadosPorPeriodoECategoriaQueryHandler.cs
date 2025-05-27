using MediatR;
using RProg.FluxoCaixa.Consolidado.Application.DTOs;
using RProg.FluxoCaixa.Worker.Infrastructure.Data;

namespace RProg.FluxoCaixa.Consolidado.Application.Queries
{
    /// <summary>
    /// Handler para processar consultas de dados consolidados por período e categoria.
    /// </summary>
    public class ObterConsolidadosPorPeriodoECategoriaQueryHandler : IRequestHandler<ObterConsolidadosPorPeriodoECategoriaQuery, ConsolidadoPeriodoResponseDto>
    {
        private readonly IConsolidadoDiarioRepository _repository;
        private readonly ILogger<ObterConsolidadosPorPeriodoECategoriaQueryHandler> _logger;

        public ObterConsolidadosPorPeriodoECategoriaQueryHandler(
            IConsolidadoDiarioRepository repository,
            ILogger<ObterConsolidadosPorPeriodoECategoriaQueryHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Processa a query para obter dados consolidados por período e categoria.
        /// </summary>
        /// <param name="request">Query com período e categoria desejados</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Dados consolidados filtrados por período e categoria</returns>
        public async Task<ConsolidadoPeriodoResponseDto> Handle(ObterConsolidadosPorPeriodoECategoriaQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando consulta de consolidados por período e categoria: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Categoria: {Categoria}",
                request.DataInicial, request.DataFinal, request.Categoria);

            try
            {
                // Busca dados consolidados por período e categoria
                var consolidados = await _repository.ObterPorPeriodoECategoriaAsync(
                    request.DataInicial, request.DataFinal, request.Categoria, cancellationToken);

                // Busca a última data de atualização para a categoria específica no período
                var ultimaConsolidacao = await _repository.ObterUltimaDataAtualizacaoPorCategoriaAsync(
                    request.DataInicial, request.DataFinal, request.Categoria, cancellationToken);

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

                _logger.LogInformation("Consulta por categoria concluída. {TotalRegistros} registros encontrados para categoria {Categoria}. Última consolidação: {UltimaConsolidacao}",
                    response.TotalRegistros, request.Categoria, response.UltimaConsolidacao?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar consolidados por período e categoria: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Categoria: {Categoria}",
                    request.DataInicial, request.DataFinal, request.Categoria);

                throw;
            }
        }
    }
}
