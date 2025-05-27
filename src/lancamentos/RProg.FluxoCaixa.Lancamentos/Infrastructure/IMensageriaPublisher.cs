namespace RProg.FluxoCaixa.Lancamentos.Infrastructure
{
    public interface IMensageriaPublisher
    {
        Task PublicarMensagemAsync<TMensagem>(TMensagem mensagem);
    }
}
