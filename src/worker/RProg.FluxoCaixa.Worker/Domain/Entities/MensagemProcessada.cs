namespace RProg.FluxoCaixa.Worker.Domain.Entities
{
    /// <summary>
    /// Entidade para controle de processamento de mensagens (idempotência).
    /// </summary>
    public class MensagemProcessada
    {
        public int Id { get; set; }

        public string IdMensagem { get; set; } = string.Empty;

        public DateTime DataProcessamento { get; set; }

        public string ConteudoMensagem { get; set; } = string.Empty;

        public string NomeFila { get; set; } = string.Empty;
    }
}
