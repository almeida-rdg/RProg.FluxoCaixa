using System.ComponentModel.DataAnnotations;

namespace RProg.FluxoCaixa.Worker.Domain.Entities
{
    /// <summary>
    /// Entidade para consolidação diária de lançamentos.
    /// </summary>
    public class ConsolidadoDiario
    {
        public int Id { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [StringLength(100)]
        public string? Categoria { get; set; } // Null = consolidação geral

        public decimal TotalCreditos { get; set; }

        public decimal TotalDebitos { get; set; }

        public decimal SaldoFinal { get; set; }

        public int QuantidadeLancamentos { get; set; }        public DateTime DataProcessamento { get; set; }

        public DateTime DataAtualizacao { get; set; }

        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Indica se é uma consolidação geral (true) ou por categoria (false).
        /// </summary>
        public bool IsConsolidacaoGeral => string.IsNullOrEmpty(Categoria);

        public void RecalcularSaldo()
        {
            SaldoFinal = TotalCreditos - TotalDebitos;
        }
    }
}
