using MediatR;
using Microsoft.AspNetCore.Mvc;
using RProg.FluxoCaixa.Consolidado.Application.DTOs;
using RProg.FluxoCaixa.Consolidado.Application.Queries;

namespace RProg.FluxoCaixa.Consolidado.Controllers
{
    /// <summary>
    /// Controller para consulta de dados consolidados por período e categoria.
    /// Implementa padrão CQRS para queries otimizadas de consolidação.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ConsolidadoController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ConsolidadoController> _logger;

        /// <summary>
        /// Inicializa o controller de consolidados.
        /// </summary>
        /// <param name="mediator">Mediador para padrão CQRS</param>
        /// <param name="logger">Logger para auditoria</param>
        public ConsolidadoController(IMediator mediator, ILogger<ConsolidadoController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Consulta dados consolidados por período.
        /// </summary>
        /// <param name="dataInicial">Data inicial do período (obrigatório)</param>
        /// <param name="dataFinal">Data final do período (obrigatório)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Dados consolidados do período com última consolidação</returns>
        /// <response code="200">Consulta realizada com sucesso</response>
        /// <response code="400">Parâmetros inválidos ou data final inferior à inicial</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet]
        [ProducesResponseType(typeof(ConsolidadoPeriodoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsolidadoPeriodoResponseDto>> ObterConsolidadosPorPeriodo(
            [FromQuery] DateTime dataInicial,
            [FromQuery] DateTime dataFinal,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Recebida solicitação para obter consolidados por período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}",
                dataInicial, dataFinal); try
            {
                var query = new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal);

                var resultado = await _mediator.Send(query, cancellationToken);

                _logger.LogInformation("Consulta de consolidados por período concluída com sucesso. Registros: {TotalRegistros}",
                    resultado.TotalRegistros);

                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parâmetros inválidos na consulta por período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}",
                    dataInicial, dataFinal);
                    
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao consultar consolidados por período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}",
                    dataInicial, dataFinal);

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Consulta dados consolidados por período e categoria específica.
        /// </summary>
        /// <param name="categoria">Categoria específica para filtro (obrigatório)</param>
        /// <param name="dataInicial">Data inicial do período (obrigatório)</param>
        /// <param name="dataFinal">Data final do período (obrigatório)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Dados consolidados da categoria no período com última consolidação</returns>
        /// <response code="200">Consulta realizada com sucesso</response>
        /// <response code="400">Parâmetros inválidos, categoria não informada ou data final inferior à inicial</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet("categoria/{categoria}")]
        [ProducesResponseType(typeof(ConsolidadoPeriodoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsolidadoPeriodoResponseDto>> ObterConsolidadosPorPeriodoECategoria(
            [FromRoute] string categoria,
            [FromQuery] DateTime dataInicial,
            [FromQuery] DateTime dataFinal,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Recebida solicitação para obter consolidados por período e categoria: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Categoria: {Categoria}",
                dataInicial, dataFinal, categoria);

            try
            {
                if (string.IsNullOrWhiteSpace(categoria))
                {
                    return BadRequest("Categoria deve ser informada");
                }

                var query = new ObterConsolidadosPorPeriodoECategoriaQuery(dataInicial, dataFinal, categoria);

                var resultado = await _mediator.Send(query, cancellationToken);

                _logger.LogInformation("Consulta de consolidados por período e categoria concluída com sucesso. Categoria: {Categoria}, Registros: {TotalRegistros}",
                    categoria, resultado.TotalRegistros);

                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parâmetros inválidos na consulta por período e categoria: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Categoria: {Categoria}",
                    dataInicial, dataFinal, categoria);

                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao consultar consolidados por período e categoria: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Categoria: {Categoria}",
                    dataInicial, dataFinal, categoria);

                return StatusCode(StatusCodes.Status500InternalServerError, "Erro interno do servidor");
            }
        }
    }
}
