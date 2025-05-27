using MediatR;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;

namespace RProg.FluxoCaixa.Lancamentos.Application.Queries
{    // Handler para a query de obtenção de lançamentos por data
    public class ObterLancamentosPorDataQueryHandler : IRequestHandler<ObterLancamentosPorDataQuery, IEnumerable<Lancamento>>
    {
        private readonly ILancamentoRepository _repositorio;

        public ObterLancamentosPorDataQueryHandler(ILancamentoRepository repositorio)
        {
            _repositorio = repositorio;
        }        public async Task<IEnumerable<Lancamento>> Handle(ObterLancamentosPorDataQuery request, CancellationToken cancellationToken)
        {
            return await _repositorio.ObterPorDataAsync(request.Data, cancellationToken);
        }
    }
}
