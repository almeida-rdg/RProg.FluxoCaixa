using RProg.FluxoCaixa.Worker.Domain.Entities;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Interface para repositório de controle de mensagens processadas.
    /// </summary>
    public interface IMensagemProcessadaRepository
    {
        Task<bool> JaFoiProcessadaAsync(string idMensagem);
        Task RegistrarMensagemProcessadaAsync(MensagemProcessada mensagem);
        Task InicializarEstruturaBancoAsync();
    }
}
