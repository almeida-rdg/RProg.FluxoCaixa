using MediatR;
using RProg.FluxoCaixa.Consolidado.Application.DTOs;

namespace RProg.FluxoCaixa.Consolidado.Application.Queries
{    /// <summary>
    /// Query para obter dados consolidados por período de datas e tipo de consolidação.
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
        /// Tipo de consolidação (GERAL ou CATEGORIA).
        /// </summary>
        public string TipoConsolidacao { get; }

        /// <summary>
        /// Construtor da query com validação de negócio.
        /// </summary>
        /// <param name="dataInicial">Data inicial do período</param>
        /// <param name="dataFinal">Data final do período</param>
        /// <param name="tipoConsolidacao">Tipo de consolidação (GERAL ou CATEGORIA)</param>
        /// <exception cref="ArgumentException">Lançada quando a data final é anterior à data inicial ou tipo de consolidação é inválido</exception>
        public ObterConsolidadosPorPeriodoQuery(DateTime dataInicial, DateTime dataFinal, string tipoConsolidacao)
        {
            ValidarPeriodo(dataInicial, dataFinal);
            ValidarTipoConsolidacao(tipoConsolidacao);
            
            DataInicial = dataInicial.Date;
            DataFinal = dataFinal.Date;
            TipoConsolidacao = tipoConsolidacao.Trim().ToUpper();
        }

        private static void ValidarPeriodo(DateTime dataInicial, DateTime dataFinal)
        {
            if (dataFinal < dataInicial)
            {
                throw new ArgumentException("A data final não pode ser anterior à data inicial.");
            }
        }

        private static void ValidarTipoConsolidacao(string tipoConsolidacao)
        {
            if (string.IsNullOrWhiteSpace(tipoConsolidacao))
            {
                throw new ArgumentException("Tipo de consolidação deve ser 'GERAL' ou 'CATEGORIA'.");
            }

            var tipoNormalizado = tipoConsolidacao.Trim().ToUpper();
            if (tipoNormalizado != "GERAL" && tipoNormalizado != "CATEGORIA")
            {
                throw new ArgumentException("Tipo de consolidação deve ser 'GERAL' ou 'CATEGORIA'.");
            }
        }
    }
}
