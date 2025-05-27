using MediatR;
using RProg.FluxoCaixa.Consolidado.Application.DTOs;

namespace RProg.FluxoCaixa.Consolidado.Application.Queries
{
    /// <summary>
    /// Query para obter dados consolidados por período de datas.
    /// </summary>
    public record ObterConsolidadosPorPeriodoQuery : IRequest<ConsolidadoPeriodoResponseDto>
    {
        /// <summary>
        /// Data inicial do período (inclusive).
        /// </summary>
        public DateTime DataInicial { get; }

        /// <summary>
        /// Data final do período (inclusive).
        /// </summary>
        public DateTime DataFinal { get; }

        /// <summary>
        /// Construtor da query com validação de negócio.
        /// </summary>
        /// <param name="dataInicial">Data inicial do período</param>
        /// <param name="dataFinal">Data final do período</param>
        /// <exception cref="ArgumentException">Lançada quando a data final é anterior à data inicial</exception>
        public ObterConsolidadosPorPeriodoQuery(DateTime dataInicial, DateTime dataFinal)
        {
            ValidarPeriodo(dataInicial, dataFinal);
            
            DataInicial = dataInicial.Date;
            DataFinal = dataFinal.Date;
        }

        private static void ValidarPeriodo(DateTime dataInicial, DateTime dataFinal)
        {
            if (dataFinal < dataInicial)
            {
                throw new ArgumentException("A data final não pode ser anterior à data inicial.");
            }
        }
    }
}
