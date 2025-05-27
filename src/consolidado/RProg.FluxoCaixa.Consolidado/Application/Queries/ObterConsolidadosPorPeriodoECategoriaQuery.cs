using MediatR;
using RProg.FluxoCaixa.Consolidado.Application.DTOs;

namespace RProg.FluxoCaixa.Consolidado.Application.Queries
{
    /// <summary>
    /// Query para obter dados consolidados por período e categoria específica.
    /// </summary>
    public record ObterConsolidadosPorPeriodoECategoriaQuery : IRequest<ConsolidadoPeriodoResponseDto>
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
        /// Categoria específica para filtro.
        /// </summary>
        public string Categoria { get; }

        /// <summary>
        /// Construtor da query com validação de negócio.
        /// </summary>
        /// <param name="dataInicial">Data inicial do período</param>
        /// <param name="dataFinal">Data final do período</param>
        /// <param name="categoria">Categoria para filtro</param>
        /// <exception cref="ArgumentException">Lançada quando a data final é anterior à data inicial ou categoria é inválida</exception>
        public ObterConsolidadosPorPeriodoECategoriaQuery(DateTime dataInicial, DateTime dataFinal, string categoria)
        {
            ValidarPeriodo(dataInicial, dataFinal);
            ValidarCategoria(categoria);
            
            DataInicial = dataInicial.Date;
            DataFinal = dataFinal.Date;
            Categoria = categoria.Trim().ToUpper();
        }

        private static void ValidarPeriodo(DateTime dataInicial, DateTime dataFinal)
        {
            if (dataFinal < dataInicial)
            {
                throw new ArgumentException("A data final não pode ser anterior à data inicial.");
            }
        }

        private static void ValidarCategoria(string categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria))
            {
                throw new ArgumentException("Categoria é obrigatória.");
            }
        }
    }
}
