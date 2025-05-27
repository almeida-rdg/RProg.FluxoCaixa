using System.ComponentModel.DataAnnotations;

namespace RProg.FluxoCaixa.Worker.Domain.Entities
{
    /// <summary>
    /// Entidade para controle de idempotência de lançamentos processados.
    /// Evita o processamento duplicado de mensagens da fila.
    /// </summary>
    public class LancamentoProcessado
    {
        /// <summary>
        /// Identificador único do registro.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID do lançamento processado (deve corresponder ao ID da mensagem RabbitMQ).
        /// </summary>
        [Required]
        public int LancamentoId { get; set; }

        /// <summary>
        /// Data e hora em que o lançamento foi processado.
        /// </summary>
        [Required]
        public DateTime DataProcessamento { get; set; }

        /// <summary>
        /// Hash do conteúdo da mensagem para validação adicional (opcional).
        /// </summary>
        [StringLength(64)]
        public string? HashConteudo { get; set; }

        /// <summary>
        /// Nome da fila de origem da mensagem.
        /// </summary>
        [StringLength(100)]
        public string? NomeFila { get; set; }

        /// <summary>
        /// Informações adicionais sobre o processamento.
        /// </summary>
        [StringLength(500)]
        public string? Observacoes { get; set; }
    }
}
