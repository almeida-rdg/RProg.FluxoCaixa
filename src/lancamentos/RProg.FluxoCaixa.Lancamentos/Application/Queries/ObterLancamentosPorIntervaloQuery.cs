using MediatR;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Domain.Exceptions;

namespace RProg.FluxoCaixa.Lancamentos.Application.Queries
{
    /// <summary>
    /// Query para obter lançamentos por intervalo de datas.
    /// </summary>
    public record ObterLancamentosPorIntervaloQuery : IRequest<IEnumerable<Lancamento>>
    {
        public DateTime DataInicial { get; }
        public DateTime DataFinal { get; }

        public ObterLancamentosPorIntervaloQuery(DateTime dataInicial, DateTime dataFinal)
        {
            ValidarIntervalo(dataInicial, dataFinal);
            
            DataInicial = dataInicial;
            DataFinal = dataFinal;
        }

        private static void ValidarIntervalo(DateTime dataInicial, DateTime dataFinal)
        {
            if (dataInicial >= dataFinal)
            {
                throw new ExcecaoDadosInvalidos("A data inicial deve ser inferior à data final.");
            }
        }
    }
}
