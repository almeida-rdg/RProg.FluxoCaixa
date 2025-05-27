using RProg.FluxoCaixa.Worker.Domain.DTOs;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Services
{
    /// <summary>
    /// Interface para serviço de consumo de mensagens RabbitMQ.
    /// Suporta múltiplas filas com prefixo 'lancamento'.
    /// </summary>
    public interface IRabbitMqService
    {
        /// <summary>
        /// Inicia a escuta das filas RabbitMQ.
        /// </summary>
        /// <param name="prefixoFila">Prefixo das filas a serem monitoradas</param>
        /// <param name="onMessageReceived">Callback para processamento das mensagens</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task IniciarEscutaAsync(string prefixoFila, Func<LancamentoDto, string, Task<bool>> onMessageReceived, CancellationToken cancellationToken);

        /// <summary>
        /// Para a escuta das filas RabbitMQ.
        /// </summary>
        Task PararEscutaAsync();

        /// <summary>
        /// Verifica se o serviço está conectado.
        /// </summary>
        bool EstaConectado { get; }

        /// <summary>
        /// Reconecta ao RabbitMQ em caso de falha.
        /// </summary>
        Task ReconectarAsync();
    }
}
