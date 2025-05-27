namespace RProg.FluxoCaixa.Worker.Domain.DTOs
{
    /// <summary>
    /// DTO para representar um lançamento recebido da fila.
    /// </summary>
    public class LancamentoDto
    {
        public int Id { get; set; }

        public string Descricao { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public int Tipo { get; set; } // 1 = Débito, 2 = Crédito

        public DateTime Data { get; set; }

        public string? Categoria { get; set; }

        public DateTime DataCadastro { get; set; }

        /// <summary>
        /// Indica se é um lançamento de crédito.
        /// </summary>
        public bool IsCredito => Tipo == 2;

        /// <summary>
        /// Indica se é um lançamento de débito.
        /// </summary>
        public bool IsDebito => Tipo == 1;
    }
}
