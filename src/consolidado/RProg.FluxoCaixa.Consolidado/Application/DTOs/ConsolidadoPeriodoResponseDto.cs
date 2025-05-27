namespace RProg.FluxoCaixa.Consolidado.Application.DTOs
{
    /// <summary>
    /// DTO de resposta contendo dados consolidados com informações de última atualização.
    /// </summary>
    public class ConsolidadoPeriodoResponseDto
    {
        /// <summary>
        /// Data inicial do período consultado.
        /// </summary>
        public DateTime DataInicial { get; set; }

        /// <summary>
        /// Data final do período consultado.
        /// </summary>
        public DateTime DataFinal { get; set; }

        /// <summary>
        /// Lista de dados consolidados do período.
        /// </summary>
        public List<ConsolidadoResponseDto> Consolidados { get; set; } = new();

        /// <summary>
        /// Data e horário da última consolidação realizada no período.
        /// </summary>
        public DateTime? UltimaConsolidacao { get; set; }

        /// <summary>
        /// Total de registros encontrados.
        /// </summary>
        public int TotalRegistros { get; set; }
    }
}
