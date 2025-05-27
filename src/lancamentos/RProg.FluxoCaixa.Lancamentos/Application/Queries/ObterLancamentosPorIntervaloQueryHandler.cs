using MediatR;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;

namespace RProg.FluxoCaixa.Lancamentos.Application.Queries
{
    /// <summary>
    /// Handler para a query de obtenção de lançamentos por intervalo de datas.
    /// </summary>
    public class ObterLancamentosPorIntervaloQueryHandler : IRequestHandler<ObterLancamentosPorIntervaloQuery, IEnumerable<Lancamento>>
    {
        private readonly ILancamentoRepository _repositorio;
        private readonly ILogger<ObterLancamentosPorIntervaloQueryHandler> _logger;

        public ObterLancamentosPorIntervaloQueryHandler(ILancamentoRepository repositorio, ILogger<ObterLancamentosPorIntervaloQueryHandler> logger)
        {
            _repositorio = repositorio;
            _logger = logger;
        }

        public async Task<IEnumerable<Lancamento>> Handle(ObterLancamentosPorIntervaloQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando busca de lançamentos por intervalo de {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}", 
                request.DataInicial, request.DataFinal);

            var lancamentos = await _repositorio.ObterPorIntervaloAsync(request.DataInicial, request.DataFinal, cancellationToken);

            var quantidadeEncontrada = lancamentos.Count();
            _logger.LogInformation("Busca por intervalo concluída. {Quantidade} lançamentos encontrados", quantidadeEncontrada);

            return lancamentos;
        }
    }
}
