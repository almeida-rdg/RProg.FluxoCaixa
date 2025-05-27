using System.ComponentModel.DataAnnotations;

namespace RProg.FluxoCaixa.Worker.Domain.Entities
{    /// <summary>
    /// Entidade para consolidação diária de lançamentos otimizada para performance.
    /// </summary>
    public class ConsolidadoDiario
    {
        public long Id { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [StringLength(100)]
        public string? Categoria { get; set; } // Null = consolidação geral

        /// <summary>
        /// Tipo da consolidação calculado automaticamente pelo banco.
        /// GERAL quando Categoria é NULL, CATEGORIA caso contrário.
        /// </summary>
        public string TipoConsolidacao => string.IsNullOrEmpty(Categoria) ? "GERAL" : "CATEGORIA";

        public decimal TotalCreditos { get; set; }

        public decimal TotalDebitos { get; set; }

        /// <summary>
        /// Saldo líquido calculado automaticamente pelo banco.
        /// Fórmula: TotalCreditos - ABS(TotalDebitos)
        /// </summary>
        public decimal SaldoLiquido { get; set; }

        public int QuantidadeLancamentos { get; set; }

        public DateTime DataCriacao { get; set; }

        public DateTime DataAtualizacao { get; set; }

        /// <summary>
        /// Indica se é uma consolidação geral (true) ou por categoria (false).
        /// </summary>
        public bool IsConsolidacaoGeral => string.IsNullOrEmpty(Categoria);

        /// <summary>
        /// Valida se os valores de crédito e débito estão corretos.
        /// Créditos devem ser >= 0 e débitos devem ser <= 0.
        /// </summary>
        public bool ValidarValores()
        {
            return TotalCreditos >= 0 && TotalDebitos <= 0 && QuantidadeLancamentos >= 0;
        }

        /// <summary>
        /// Atualiza a data de modificação para o momento atual.
        /// </summary>
        public void AtualizarDataModificacao()
        {
            DataAtualizacao = DateTime.UtcNow;
        }
    }
}
