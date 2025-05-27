using RProg.FluxoCaixa.Worker.Domain.DTOs;

namespace RProg.FluxoCaixa.Worker.Domain.Services
{
    /// <summary>
    /// Interface para serviços de consolidação de lançamentos.
    /// </summary>
    public interface IConsolidacaoService
    {
        /// <summary>
        /// Processa um lançamento para consolidação diária.
        /// É idempotente - não processa a mesma mensagem mais de uma vez.
        /// </summary>
        /// <param name="lancamento">Lançamento a ser processado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se processou com sucesso, False se já foi processado anteriormente</returns>
        Task<bool> ProcessarLancamentoAsync(LancamentoDto lancamento, CancellationToken cancellationToken);

        /// <summary>
        /// Força a atualização das consolidações de uma data específica.
        /// </summary>
        /// <param name="data">Data para recalcular</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task AtualizarConsolidacaoDataAsync(DateTime data, CancellationToken cancellationToken);
    }
}
