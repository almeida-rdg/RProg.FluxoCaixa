using MediatR;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;

namespace RProg.FluxoCaixa.Lancamentos.Application.Queries
{
    // Query para obter lançamentos por data
    public class ObterLancamentosPorDataQuery : IRequest<IEnumerable<Lancamento>>
    {
        public DateTime Data { get; set; }
        public ObterLancamentosPorDataQuery(DateTime data)
        {
            Data = data;
        }
    }
}
