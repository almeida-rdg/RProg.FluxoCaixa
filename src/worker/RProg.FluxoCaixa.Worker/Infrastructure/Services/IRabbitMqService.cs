using RProg.FluxoCaixa.Worker.Domain.DTOs;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Services
{
    /// <summary>
    /// Interface para serviço de consumo de mensagens RabbitMQ.
    /// Suporta fila única configurável via appsettings.
    /// </summary>
    public interface IRabbitMqService
    {
        /// <summary>
        /// Inicia a escuta da fila RabbitMQ.
        /// </summary>
        /// <param name="onMessageReceived">Callback para processamento das mensagens</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task IniciarEscutaAsync(Func<LancamentoDto, string, Task<bool>> onMessageReceived, CancellationToken cancellationToken);

        /// <summary>
        /// Para a escuta da fila RabbitMQ.
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
