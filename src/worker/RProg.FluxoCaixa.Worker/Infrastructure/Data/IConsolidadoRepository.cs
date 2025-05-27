using RProg.FluxoCaixa.Worker.Domain.Entities;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Interface para repositório de consolidações diárias.
    /// </summary>
    public interface IConsolidadoRepository
    {
        /// <summary>
        /// Obtém ou cria um registro de consolidação para a data e categoria especificadas.
        /// </summary>
        /// <param name="data">Data da consolidação</param>
        /// <param name="categoria">Categoria (null para consolidação geral)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Registro de consolidação</returns>
        Task<ConsolidadoDiario> ObterOuCriarConsolidadoAsync(DateTime data, string? categoria, CancellationToken cancellationToken);

        /// <summary>
        /// Salva as alterações em um registro de consolidação.
        /// </summary>
        /// <param name="consolidado">Registro a ser salvo</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task SalvarConsolidadoAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken);

        /// <summary>
        /// Recalcula todas as consolidações para uma data específica.
        /// </summary>
        /// <param name="data">Data para recálculo</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task RecalcularConsolidacoesDataAsync(DateTime data, CancellationToken cancellationToken);

        /// <summary>
        /// Obtém consolidação por data e categoria.
        /// </summary>
        /// <param name="data">Data da consolidação</param>
        /// <param name="categoria">Categoria (null para consolidação geral)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Consolidação encontrada ou null</returns>
        Task<ConsolidadoDiario?> ObterPorDataECategoriaAsync(DateTime data, string? categoria, CancellationToken cancellationToken);

        /// <summary>
        /// Lista todas as consolidações de uma data.
        /// </summary>
        /// <param name="data">Data das consolidações</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de consolidações</returns>
        Task<IEnumerable<ConsolidadoDiario>> ListarPorDataAsync(DateTime data, CancellationToken cancellationToken);
    }
}
