using MediatR;
using Microsoft.AspNetCore.Mvc;
using RProg.FluxoCaixa.Lancamentos.Application.Commands;
using RProg.FluxoCaixa.Lancamentos.Application.Queries;

namespace RProg.FluxoCaixa.Lancamentos.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de lançamentos financeiros.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LancamentosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LancamentosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarLancamento([FromBody] RegistrarLancamentoCommand comando)
        {
            var id = await _mediator.Send(comando);
            return CreatedAtAction(nameof(ObterLancamentoPorId), new { id }, new { Id = id });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterLancamentoPorId(int id)
        {
            var query = new ObterLancamentoPorIdQuery(id);
            var resultado = await _mediator.Send(query);
            
            if (resultado == null)
            {
                return NotFound($"Lançamento com ID {id} não foi encontrado.");
            }
            
            return Ok(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> ObterLancamentosPorIntervalo(
            [FromQuery] DateTime dataInicial, 
            [FromQuery] DateTime dataFinal)
        {
            var query = new ObterLancamentosPorIntervaloQuery(dataInicial, dataFinal);
            var resultado = await _mediator.Send(query);
            return Ok(resultado);
        }

        [HttpGet("{data:datetime}")]
        public async Task<IActionResult> ObterLancamentosPorData(DateTime data)
        {
            var query = new ObterLancamentosPorDataQuery(data);
            var resultado = await _mediator.Send(query);
            return Ok(resultado);
        }
    }
}
