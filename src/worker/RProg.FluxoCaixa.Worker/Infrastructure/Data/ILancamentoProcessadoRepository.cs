using RProg.FluxoCaixa.Worker.Domain.Entities;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Interface para repositório de controle de lançamentos processados.
    /// Implementa funcionalidades para garantir idempotência no processamento.
    /// </summary>
    public interface ILancamentoProcessadoRepository
    {
        /// <summary>
        /// Verifica se um lançamento já foi processado.
        /// </summary>
        /// <param name="lancamentoId">ID do lançamento</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se já foi processado, false caso contrário</returns>
        Task<bool> JaFoiProcessadoAsync(int lancamentoId, CancellationToken cancellationToken);

        /// <summary>
        /// Marca um lançamento como processado.
        /// </summary>
        /// <param name="lancamentoId">ID do lançamento</param>
        /// <param name="dataProcessamento">Data e hora do processamento</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task MarcarComoProcessadoAsync(int lancamentoId, DateTime dataProcessamento, CancellationToken cancellationToken);

        /// <summary>
        /// Marca um lançamento como processado com informações adicionais.
        /// </summary>
        /// <param name="lancamentoId">ID do lançamento</param>
        /// <param name="dataProcessamento">Data e hora do processamento</param>
        /// <param name="hashConteudo">Hash do conteúdo da mensagem</param>
        /// <param name="nomeFila">Nome da fila de origem</param>
        /// <param name="observacoes">Observações adicionais</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task MarcarComoProcessadoAsync(int lancamentoId, DateTime dataProcessamento, string? hashConteudo, string? nomeFila, string? observacoes, CancellationToken cancellationToken);

        /// <summary>
        /// Remove registros de lançamentos processados mais antigos que a data especificada.
        /// </summary>
        /// <param name="dataLimite">Data limite para limpeza</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Número de registros removidos</returns>
        Task<int> LimparRegistrosAntigosAsync(DateTime dataLimite, CancellationToken cancellationToken);

        /// <summary>
        /// Obtém um registro de lançamento processado pelo ID.
        /// </summary>
        /// <param name="lancamentoId">ID do lançamento</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Registro encontrado ou null</returns>
        Task<LancamentoProcessado?> ObterPorIdAsync(int lancamentoId, CancellationToken cancellationToken);
    }
}
