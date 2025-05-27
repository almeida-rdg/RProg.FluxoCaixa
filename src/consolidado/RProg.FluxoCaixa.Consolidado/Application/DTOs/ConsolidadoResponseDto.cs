namespace RProg.FluxoCaixa.Consolidado.Application.DTOs
{
    /// <summary>
    /// DTO de resposta para dados consolidados.
    /// </summary>
    public class ConsolidadoResponseDto
    {
        /// <summary>
        /// Data da consolidação.
        /// </summary>
        public DateTime Data { get; set; }

        /// <summary>
        /// Categoria da consolidação. Null indica consolidação geral.
        /// </summary>
        public string? Categoria { get; set; }

        /// <summary>
        /// Tipo da consolidação (GERAL ou CATEGORIA).
        /// </summary>
        public string TipoConsolidacao { get; set; } = string.Empty;

        /// <summary>
        /// Total de créditos do período.
        /// </summary>
        public decimal TotalCreditos { get; set; }

        /// <summary>
        /// Total de débitos do período.
        /// </summary>
        public decimal TotalDebitos { get; set; }

        /// <summary>
        /// Saldo líquido calculado (TotalCreditos - ABS(TotalDebitos)).
        /// </summary>
        public decimal SaldoLiquido { get; set; }

        /// <summary>
        /// Quantidade de lançamentos consolidados.
        /// </summary>
        public int QuantidadeLancamentos { get; set; }

        /// <summary>
        /// Data da última atualização dos dados consolidados.
        /// </summary>
        public DateTime DataAtualizacao { get; set; }
    }
}
